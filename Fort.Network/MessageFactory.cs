using System.Reflection;
using System.Reflection.Emit;

namespace Fort.Network;

public class MessageFactory
{
	private readonly Dictionary<Type, MessageDataType> _messageTypes = new();
	private readonly Dictionary<MessageDataType, Func<IMessage>> _messageConstructors = new();

	private byte _idPool = 0;

	public MessageFactory()
	{
		RegisterAssembly<IMessage>();
	}

	private void AddAssemblyMessages(Assembly assembly)
	{
		var messageTypes = assembly.GetTypes()
			.Where(t => !t.IsAbstract && !t.IsInterface && !t.ContainsGenericParameters
				&& (typeof(IMessage).IsAssignableFrom(t) && t.IsValueType));

		foreach (var type in messageTypes)
		{
			if (_messageTypes.ContainsKey(type))
				continue;

			var id = _idPool++;
			_messageTypes.Add(type, id);

			// Create factory delegate and cache it
			_messageConstructors[id] = CreateFactory(type);
		}
	}

	public void RegisterAssembly<T>()
	{
		var assembly = typeof(T).Assembly;
		AddAssemblyMessages(assembly);
	}

	public MessageDataType GetMessageId<T>() where T : IMessage
	{
		return GetMessageId(typeof(T));
	}

	public MessageDataType GetMessageId(Type type)
	{
		if (!_messageTypes.TryGetValue(type, out var id))
			throw new ArgumentException($"Message type {type} not registered.");

		return id;
	}

	public IMessage GetMessage(MessageDataType id)
	{
		if (_messageConstructors.TryGetValue(id, out var ctor))
			return ctor();

		throw new ArgumentException($"Message ID {id} not registered.");
	}

	private static Func<IMessage> CreateFactory(Type type)
	{
		// Since all IMessage are structs, we can create instance via default ctor
		var method = new DynamicMethod($"Create_{type.Name}", typeof(IMessage), Type.EmptyTypes, true);
		var il = method.GetILGenerator();

		// Declare local of the struct type
		il.DeclareLocal(type);

		// Load local address
		il.Emit(OpCodes.Ldloca_S, (byte)0);

		// Initialize struct
		il.Emit(OpCodes.Initobj, type);

		// Load local onto stack
		il.Emit(OpCodes.Ldloc_0);

		// Box struct (because IMessage is interface)
		il.Emit(OpCodes.Box, type);

		il.Emit(OpCodes.Ret);

		return (Func<IMessage>)method.CreateDelegate(typeof(Func<IMessage>));
	}
}
