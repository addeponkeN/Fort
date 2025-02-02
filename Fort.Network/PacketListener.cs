using LiteNetLib;
using System.Diagnostics;

namespace Fort.Network;

public delegate void PacketHandlerDelegate<in T>(T packet, PacketDataType type, NetPeer sender) where T : IPacket;

public interface IPacketSender
{
	void SendPacket<T>(T packet) where T : IPacket;
}

public interface IPacketListener
{
	void Sub<T>(PacketHandlerDelegate<T> packetHandler) where T : IPacket;
	void UnSub<T>(PacketHandlerDelegate<T> packetHandler) where T : IPacket;
}

internal interface IPacketSubscriber
{
	void Trigger(IPacket packet, PacketDataType type, NetPeer peer);
	void Clear();
}

internal class PacketSubscription<T> : IPacketSubscriber where T : IPacket
{
	private event PacketHandlerDelegate<T> Event;

	public void Add(PacketHandlerDelegate<T> packetHandler)
	{
		Event += packetHandler;
	}

	/// <summary>
	/// Removes 'packetHandler' from the internal Event and returns the status of the Event.
	/// </summary>
	/// <returns>True if event is null</returns>
	public bool Remove(PacketHandlerDelegate<T> packetHandler)
	{
		Event -= packetHandler;
		return Event == null;
	}

	public void Trigger(IPacket packet, PacketDataType type, NetPeer peer)
	{
		Event?.Invoke((T)packet, type, peer);
	}

	public void Clear()
	{
		Event = null;
	}

	public void Dispose()
	{
		Clear();
	}
}

public class PacketListener : IPacketListener, IDisposable
{
	struct ReadPacket
	{
		public IPacket Packet;
		public NetPeer Peer;
		public PacketDataType PacketId;
	}

	private readonly Dictionary<Type, IPacketSubscriber> _subscribers = new();
	private readonly NetBase _base;
	private readonly PacketFactory _factory;
	private readonly List<ReadPacket> _packets = new();
	private readonly List<ReadPacket> _outgoing = new();
	private readonly object _packetLock = new();

	private Thread _thread;

	public event NetEvents.OnPacketReceived PacketReceivedEvent;

	public int PollFrequency { get; set; } = 60;

	public PacketListener(NetBase @base)
	{
		_base = @base;
		_factory = _base.PacketFactory;
	}

	public void Update()
	{
		var packets = GetPackets();
		foreach (var packet in packets)
		{
			Trigger(packet.PacketId, packet.Packet, packet.Peer);
		}
	}

	private List<ReadPacket> GetPackets()
	{
		_outgoing.Clear();
		lock (_packetLock)
		{
			if (_packets.Count <= 0)
				return _outgoing;
			_outgoing.AddRange(_packets);
			_packets.Clear();
		}

		return _outgoing;
	}

	private void NetListener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
	{
		var packetId = reader.GetByte();
		var packet = _factory.GetPacket(packetId);
		packet.Deserialize(reader);
		reader.Recycle();

		lock (_packetLock)
		{
			_packets.Add(new ReadPacket { Packet = packet, PacketId = packetId, Peer = peer });
		}
	}

	public void Start()
	{
		_base.NetListener.NetworkReceiveEvent += NetListener_NetworkReceiveEvent;
		_thread = new Thread(() =>
		{
			Stopwatch stopwatch = new Stopwatch();

			while (_base.IsRunning)
			{
				int targetFrameTime = 1000 / PollFrequency;
				stopwatch.Restart();

				_base.Manager.PollEvents();

				int elapsedTime = (int)stopwatch.ElapsedMilliseconds;
				int sleepTime = targetFrameTime - elapsedTime;

				if (sleepTime > 0)
				{
					Thread.Sleep(sleepTime);
				}
			}
		});

		_thread.Start();
	}

	public void Sub<T>(PacketHandlerDelegate<T> packetHandler) where T : IPacket
	{
		Type type = typeof(T);
		PacketSubscription<T> packetSub;
		if (!_subscribers.TryGetValue(type, out var sub))
		{
			packetSub = new PacketSubscription<T>();
			_subscribers.Add(type, packetSub);
		}
		else
		{
			packetSub = (PacketSubscription<T>)sub;
		}

		packetSub.Add(packetHandler);
	}

	public void UnSub<T>(PacketHandlerDelegate<T> packetHandler) where T : IPacket
	{
		Type type = typeof(T);
		if (_subscribers.TryGetValue(type, out var sub))
		{
			var packetSub = (PacketSubscription<T>)sub;
			if (packetSub.Remove(packetHandler))
			{
				_subscribers.Remove(type);
			}
		}
	}

	public void Trigger<T>(PacketDataType packetType, T packet, NetPeer peer) where T : IPacket
	{
		var type = packet.GetType();
		if (_subscribers.TryGetValue(type, out var sub))
		{
			sub.Trigger(packet, packetType, peer);
		}
	}

	public void Dispose()
	{
		foreach (var sub in _subscribers)
			sub.Value.Clear();
	}
}
