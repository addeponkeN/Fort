using LiteNetLib;
using System.Diagnostics;

namespace Fort.Network;

public delegate void MessageHandlerDelegate<in T>(T message, MessageDataType type, NetPeer sender) where T : IMessage;

public interface IMessageSender
{
	void SendMessage<T>(T message) where T : IMessage;
}

public interface IMessageListener
{
	void Sub<T>(MessageHandlerDelegate<T> messageHandler) where T : IMessage;
	void UnSub<T>(MessageHandlerDelegate<T> messageHandler) where T : IMessage;
}

internal interface IMessageSubscriber
{
	void Trigger(IMessage message, MessageDataType type, NetPeer peer);
	void Clear();
}

internal class MessageSubscription<T> : IMessageSubscriber where T : IMessage
{
	private event MessageHandlerDelegate<T> Event;

	public void Add(MessageHandlerDelegate<T> messageHandler)
	{
		Event += messageHandler;
	}

	/// <summary>
	/// Removes 'messageHandler' from the internal Event and returns the status of the Event.
	/// </summary>
	/// <returns>True if event is null</returns>
	public bool Remove(MessageHandlerDelegate<T> messageHandler)
	{
		Event -= messageHandler;
		return Event == null;
	}

	public void Trigger(IMessage message, MessageDataType type, NetPeer peer)
	{
		Event?.Invoke((T)message, type, peer);
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

public class MessageListener : IMessageListener, IDisposable
{
	struct ReadMessage
	{
		public IMessage Message;
		public NetPeer Peer;
		public MessageDataType MessageId;
	}

	private readonly Dictionary<Type, IMessageSubscriber> _subscribers = new();
	private readonly NetBase _base;
	private readonly MessageFactory _factory;
	private readonly List<ReadMessage> _messages = new();
	private readonly List<ReadMessage> _outgoing = new();
	private readonly object _messageLock = new();

	private Thread _thread;

	public event NetEvents.OnMessageReceived MessageReceivedEvent;

	public int PollFrequency { get; set; } = 60;

	public MessageListener(NetBase @base)
	{
		_base = @base;
		_factory = _base.MessageFactory;
	}

	public void Update()
	{
		var messages = GetMessages();
		foreach (var message in messages)
		{
			Trigger(message.MessageId, message.Message, message.Peer);
		}
	}

	private List<ReadMessage> GetMessages()
	{
		_outgoing.Clear();
		lock (_messageLock)
		{
			if (_messages.Count <= 0)
				return _outgoing;
			_outgoing.AddRange(_messages);
			_messages.Clear();
		}

		return _outgoing;
	}

	private void NetListener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
	{
		var messageId = reader.GetByte();
		var message = _factory.GetMessage(messageId);
		message.Deserialize(reader);
		reader.Recycle();

		lock (_messageLock)
		{
			_messages.Add(new ReadMessage { Message = message, MessageId = messageId, Peer = peer });
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

	public void Sub<T>(MessageHandlerDelegate<T> messageHandler) where T : IMessage
	{
		Type type = typeof(T);
		MessageSubscription<T> messageSub;
		if (!_subscribers.TryGetValue(type, out var sub))
		{
			messageSub = new MessageSubscription<T>();
			_subscribers.Add(type, messageSub);
		}
		else
		{
			messageSub = (MessageSubscription<T>)sub;
		}

		messageSub.Add(messageHandler);
	}

	public void UnSub<T>(MessageHandlerDelegate<T> messageHandler) where T : IMessage
	{
		Type type = typeof(T);
		if (_subscribers.TryGetValue(type, out var sub))
		{
			var messageSub = (MessageSubscription<T>)sub;
			if (messageSub.Remove(messageHandler))
			{
				_subscribers.Remove(type);
			}
		}
	}

	public void Trigger<T>(MessageDataType messageType, T message, NetPeer peer) where T : IMessage
	{
		var type = message.GetType();
		if (_subscribers.TryGetValue(type, out var sub))
		{
			sub.Trigger(message, messageType, peer);
		}
	}

	public void Dispose()
	{
		foreach (var sub in _subscribers)
			sub.Value.Clear();
	}
}
