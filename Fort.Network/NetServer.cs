using LiteNetLib;
using LiteNetLib.Utils;

namespace Fort.Network;

public class NetEvents
{
	public delegate void OnConnectionRequest(ConnectionRequest request);
	public delegate void OnPeerConnected(NetPeer peer);
	public delegate void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo);
	public delegate void OnPacketReceived(NetPeer peer, IPacket packet, PacketDataType type);
}

public abstract class NetBase
{
	protected object SendPacketLock = new object();

	public NetManager Manager { get; private set; }
	internal EventBasedNetListener NetListener { get; set; }
	public PacketFactory PacketFactory { get; private set; } = new();
	public PacketListener PacketListener { get; private set; }
	public NetDataWriter PacketWriter { get; private set; } = new();
	public bool IsRunning { get; set; }

	public event NetEvents.OnPacketReceived PacketReceivedEvent;

	protected NetBase()
	{
		NetListener = new EventBasedNetListener();
		Manager = new(NetListener);

		PacketListener = new(this);
	}

	protected void OnStart()
	{
		PacketListener.PacketReceivedEvent += (peer, packet, type) => PacketReceivedEvent?.Invoke(peer, packet, type);
	}

	public void Update()
	{
		if (!IsRunning) return;

		Manager.PollEvents();
	}

	protected void WritePacket<T>(T packet) where T : IPacket
	{
		var packetType = PacketFactory.GetPacketId<T>();
		PacketWriter.Put(packetType);
		packet.Serialize(PacketWriter);
	}
}

public class NetServer : NetBase
{
	public event NetEvents.OnConnectionRequest ConnectionRequestEvent;
	public event NetEvents.OnPeerConnected PeerConnectedEvent;
	public event NetEvents.OnPeerDisconnected PeerDisconnectedEvent;

	public void Start(int listenPort)
	{
		IsRunning = true;
		Manager.Start(listenPort);

		OnStart();

		NetListener.ConnectionRequestEvent += NetListenerConnectionRequestEvent;
		NetListener.PeerConnectedEvent += NetListenerPeerConnectedEvent;
		NetListener.PeerDisconnectedEvent += NetListenerPeerDisconnectedEvent;
	}

	private void NetListenerConnectionRequestEvent(ConnectionRequest request) => ConnectionRequestEvent?.Invoke(request);
	private void NetListenerPeerConnectedEvent(NetPeer peer) => PeerConnectedEvent?.Invoke(peer);
	private void NetListenerPeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo) => PeerDisconnectedEvent?.Invoke(peer, disconnectInfo);

	public void SendPacket<T>(T packet, NetPeer peer) where T : IPacket
	{
		lock (SendPacketLock)
		{
			PacketWriter.Reset();
			WritePacket(packet);
			peer.Send(PacketWriter, DeliveryMethod.ReliableOrdered);
		}
	}

	public void SendPacket<T>(T packet, IEnumerable<NetPeer> peers) where T : IPacket
	{
		lock (SendPacketLock)
		{
			PacketWriter.Reset();
			WritePacket(packet);
			foreach (var peer in peers)
			{
				peer.Send(PacketWriter, DeliveryMethod.ReliableOrdered);
			}
		}
	}
}

public class NetClient : NetBase
{
	public void Connect(string address, int port, string key)
	{
		IsRunning = true;
		Manager.Start();
		Manager.Connect(address, port, key);

		OnStart();
	}

	public void Send(IPacket packet)
	{
		PacketWriter.Reset();
		WritePacket(packet);
		Manager.FirstPeer.Send(PacketWriter, DeliveryMethod.ReliableOrdered);
	}
}