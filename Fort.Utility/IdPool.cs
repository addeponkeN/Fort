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

public class IdPoolByte
{
    private byte _idPool;
    private readonly Stack<byte> _pool = new();

    public IdPoolByte(byte poolStart = 1)
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
