
using System.Diagnostics;

namespace System.Buffers;

/// <summary>
/// 表示数组持有者
/// </summary>
/// <typeparam name="T"></typeparam>
[DebuggerDisplay("Length = {Length}")]
sealed class ArrayOwner<T> : IArrayOwner<T>
{
    private bool disposed = false;
    private readonly ArrayPool<T> arrayPool;

    /// <summary>
    /// 获取数据有效数据长度
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// 获取持有的数组
    /// </summary>
    public T[] Array { get; }

    public Span<T> AsSpan()
    {
        return Array.AsSpan(0, Length);
    }

    public Memory<T> AsMemory()
    {
        return Array.AsMemory(0, Length);
    }

    /// <summary>
    /// 数组持有者
    /// </summary>
    /// <param name="arrayPool"></param>
    /// <param name="length"></param> 
    public ArrayOwner(ArrayPool<T> arrayPool, int length)
    {
        this.arrayPool = arrayPool;
        Length = length;
        Array = arrayPool.Rent(length);
    }

    /// <summary>
    /// 将对象进行回收
    /// </summary>
    public void Dispose()
    {
        if (disposed == false)
        {
            arrayPool.Return(Array);
            GC.SuppressFinalize(this);
        }
        disposed = true;
    }

    ~ArrayOwner()
    {
        Dispose();
    }
}
