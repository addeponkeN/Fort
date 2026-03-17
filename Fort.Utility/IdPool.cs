namespace Fort.Utility;

public class IdPool
{
    private int _idPool;
    private readonly Stack<int> _pool = new();

    public IdPool(int poolStart = 1)
    {
        _idPool = poolStart;
    }

    public int Get()
    {
        if (_pool.Count > 0)
            return _pool.Pop();
        return _idPool++;
    }

    public void Return(int v)
    {
        _pool.Push(v);
    }
}

public class IdPool16S
{
    private ushort _idPool;
    private readonly Stack<ushort> _pool = new();

    public IdPool16S(ushort poolStart = 1)
    {
        _idPool = poolStart;
    }

    public ushort Get()
    {
        if (_pool.Count > 0)
            return _pool.Pop();
        return _idPool++;
    }

    public void Return(ushort v)
    {
        _pool.Push(v);
    }
}

public class IdPool8
{
    private byte _idPool;
    private readonly Stack<byte> _pool = new();

    public IdPool8(byte poolStart = 1)
    {
        _idPool = poolStart;
    }

    public byte Get()
    {
        if (_pool.Count > 0)
            return _pool.Pop();
        return _idPool++;
    }

    public void Return(byte v)
    {
        _pool.Push(v);
    }
}
