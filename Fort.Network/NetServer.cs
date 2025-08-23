using LiteNetLib;
using LiteNetLib.Utils;

namespace Fort.Network;

public class NetEvents
{
	public delegate void OnConnectionRequest(ConnectionRequest request);
	public delegate void OnPeerConnected(NetPeer peer);
	public delegate void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo);
	public delegate void OnMessageReceived(NetPeer peer, IMessage message, Byte type);
}

public abstract class NetBase
{
	protected object SendMessageLock = new object();

	public NetManager Manager { get; private set; }
	internal EventBasedNetListener NetListener { get; set; }
	public MessageFactory MessageFactory { get; private set; } = new();
	public MessageListener MessageListener { get; private set; }
	public NetDataWriter MessageWriter { get; private set; } = new();
	public bool IsRunning { get; set; }

	public event NetEvents.OnMessageReceived MessageReceivedEvent;

	protected NetBase()
	{
		NetListener = new EventBasedNetListener();
		Manager = new(NetListener);

		MessageListener = new(this);
	}

	protected void OnStart()
	{
		MessageListener.MessageReceivedEvent += MessageListenerMessageReceivedEvent;
	}

	protected void OnStop()
	{
		MessageListener.MessageReceivedEvent -= MessageListenerMessageReceivedEvent;
	}

	private void MessageListenerMessageReceivedEvent(NetPeer peer, IMessage message, byte type)
		=> MessageReceivedEvent?.Invoke(peer, message, type);

	public void Update()
	{
		if (!IsRunning) return;

		Manager.PollEvents();
	}

	protected void WriteMessage<T>(T message) where T : IMessage
	{
		var messageType = MessageFactory.GetMessageId<T>();
		MessageWriter.Put(messageType);
		message.Serialize(MessageWriter);
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

	public void Stop()
	{
		OnStop();

		Manager.DisconnectAll();
		Manager.Stop();

		NetListener.ConnectionRequestEvent -= NetListenerConnectionRequestEvent;
		NetListener.PeerConnectedEvent -= NetListenerPeerConnectedEvent;
		NetListener.PeerDisconnectedEvent -= NetListenerPeerDisconnectedEvent;

		IsRunning = false;
	}

	private void NetListenerConnectionRequestEvent(ConnectionRequest request) => ConnectionRequestEvent?.Invoke(request);
	private void NetListenerPeerConnectedEvent(NetPeer peer) => PeerConnectedEvent?.Invoke(peer);
	private void NetListenerPeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo) => PeerDisconnectedEvent?.Invoke(peer, disconnectInfo);

	public void SendMessage<T>(T message, NetPeer peer) where T : IMessage
	{
		lock (SendMessageLock)
		{
			MessageWriter.Reset();
			WriteMessage(message);
			peer.Send(MessageWriter, DeliveryMethod.ReliableOrdered);
		}
	}

	public void SendMessage<T>(T message, IEnumerable<NetPeer> peers) where T : IMessage
	{
		lock (SendMessageLock)
		{
			MessageWriter.Reset();
			WriteMessage(message);
			foreach (var peer in peers)
			{
				peer.Send(MessageWriter, DeliveryMethod.ReliableOrdered);
			}
		}
	}
}

public class NetClient : NetBase
{
	public event NetEvents.OnPeerConnected ConnectedEvent;
	public event NetEvents.OnPeerDisconnected DisconnectedEvent;

	public void Connect(string address, int port, string key)
	{
		IsRunning = true;
		Manager.Start();
		Manager.Connect(address, port, key);

		OnStart();

		NetListener.PeerConnectedEvent += NetListener_PeerConnectedEvent;
		NetListener.PeerDisconnectedEvent += NetListener_OnPeerDisconnectedEvent;
	}

	public void Disconnect()
	{
		OnStop();

		NetListener.PeerConnectedEvent -= NetListener_PeerConnectedEvent;
		NetListener.PeerDisconnectedEvent -= NetListener_OnPeerDisconnectedEvent;
	}

	private void NetListener_OnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectinfo) =>
		DisconnectedEvent?.Invoke(peer, disconnectinfo);

	private void NetListener_PeerConnectedEvent(NetPeer peer) => ConnectedEvent?.Invoke(peer);

	public void Send(IMessage message)
	{
		MessageWriter.Reset();
		WriteMessage(message);
		Manager.FirstPeer.Send(MessageWriter, DeliveryMethod.ReliableOrdered);
	}
}