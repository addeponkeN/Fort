using System.Reflection;

namespace Fort.Network;

public class PacketFactory
{
	private readonly Dictionary<Type, PacketDataType> _packetTypes;

	private PacketDataType _idPool = 0;

	public PacketFactory()
	{
		_packetTypes = new Dictionary<Type, PacketDataType>();

		RegisterAssembly<IPacket>();
	}

	private void AddAssemblyPackets(Assembly assembly)
	{
		var types = assembly.GetTypes();

		var packetTypes = types.Where(x => !x.IsAbstract && !x.IsInterface && !x.ContainsGenericParameters &&
		                                   (x.IsSubclassOf(typeof(IPacket)) ||
		                                    (x.IsValueType && typeof(IPacket).IsAssignableFrom(x))));

		foreach (var classType in packetTypes)
		{
			_packetTypes.Add(classType, _idPool++);
		}
	}

	public void RegisterAssembly<T>()
	{
		var assembly = Assembly.GetAssembly(typeof(T));
		AddAssemblyPackets(assembly);
	}

	public PacketDataType GetPacketId<T>() where T : IPacket
	{
		return GetPacketId(typeof(T));
	}

	public PacketDataType GetPacketId(Type type)
	{
		return _packetTypes[type];
	}

	public IPacket GetPacket(PacketDataType type)
	{
		string name = $"{type}Packet";
		return GetPacket(name);
	}

	public IPacket GetPacket(string name)
	{
		string packetNamespace = "turfers2.Library.Net";
		name = $"{packetNamespace}.{name}";
		Type structType = Type.GetType(name);
		if (structType != null && structType.IsValueType && !structType.IsEnum)
		{
			object instance = Activator.CreateInstance(structType);
			if (instance is IPacket packet)
			{
				return packet;
			}
			else
			{
				throw new InvalidOperationException($"The specified struct does not implement IPacket interface '{name}'");
			}
		}
		else
		{
			throw new ArgumentException($"Invalid struct name '{name}'");
		}
	}

}