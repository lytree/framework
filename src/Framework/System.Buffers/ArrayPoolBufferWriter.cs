using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers;

/// <summary>
/// 表示基于ArrayPool的BufferWriter
/// </summary>
[DebuggerDisplay("WrittenCount = {WrittenCount}")]
public sealed class ArrayPoolBufferWriter<T> : IWrittenBufferWriter<T>, IDisposable
{
    private int index = 0;
    private T[] buffer;
    private bool disposed = false;
    private const int defaultSizeHint = 256;

    /// <summary>
    /// 获取已数入的数据长度
    /// </summary>
    public int WrittenCount => index;

    /// <summary>
    /// 获取已数入的数据
    /// </summary>
    public ReadOnlySpan<T> WrittenSpan => buffer.AsSpan(0, index);

    /// <summary>
    /// 获取已数入的数据
    /// </summary>
    public ReadOnlyMemory<T> WrittenMemory => buffer.AsMemory(0, index);

    /// <summary>
    /// 获取已数入的数据
    /// </summary>
    /// <returns></returns>
    public ArraySegment<T> WrittenSegment => new(buffer, 0, index);

    /// <summary>
    /// 获取容量
    /// </summary>
    public int Capacity => buffer.Length;

    /// <summary>
    /// 获取剩余容量
    /// </summary>
    public int FreeCapacity => buffer.Length - index;


    /// <summary>
    /// 基于ArrayPool的BufferWriter
    /// </summary>
    /// <param name="initialCapacity">初始容量</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public ArrayPoolBufferWriter(int initialCapacity = 8)
    {
        if (initialCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        }
        buffer = ArrayPool<T>.Shared.Rent(initialCapacity);
    }

    /// <summary>
    /// 清除数据
    /// </summary>
    public void Clear()
    {
        buffer.AsSpan(0, index).Clear();
        index = 0;
    }

    /// <summary>
    /// 设置向前推进
    /// </summary>
    /// <param name="count"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Advance(int count)
    {
        if (count < 0 || index > buffer.Length - count)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        index += count;
    }

    /// <summary>
    /// 返回用于写入数据的Memory
    /// </summary>
    /// <param name="sizeHint">意图大小</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <returns></returns>
    public Memory<T> GetMemory(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return buffer.AsMemory(index);
    }

    /// <summary>
    /// 返回用于写入数据的Span
    /// </summary>
    /// <param name="sizeHint">意图大小</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <returns></returns>
    public Span<T> GetSpan(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return buffer.AsSpan(index);
    }

    /// <summary>
    /// 写入数据
    /// </summary>
    /// <param name="value"></param>
    public void Write(T value)
    {
        GetSpan(1)[0] = value;
        index += 1;
    }

    /// <summary>
    /// 写入数据
    /// </summary>
    /// <param name="value">值</param> 
    public void Write(ReadOnlySpan<T> value)
    {
        if (value.IsEmpty == false)
        {
            value.CopyTo(GetSpan(value.Length));
            index += value.Length;
        }
    }

    /// <summary>
    /// 检测和扩容
    /// </summary>
    /// <param name="sizeHint"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckAndResizeBuffer(int sizeHint)
    {
        if (sizeHint < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeHint));
        }

        if (sizeHint == 0)
        {
            sizeHint = defaultSizeHint;
        }

        if (sizeHint > FreeCapacity)
        {
            int currentLength = buffer.Length;
            var growBy = Math.Max(sizeHint, currentLength);
            var newSize = checked(currentLength + growBy);

            var newBuffer = ArrayPool<T>.Shared.Rent(newSize);
            Array.Copy(buffer, newBuffer, index);

            ArrayPool<T>.Shared.Return(buffer);
            buffer = newBuffer;
        }
    }

    /// <summary>
    /// 将对象进行回收
    /// </summary>
    public void Dispose()
    {
        if (disposed == false)
        {
            ArrayPool<T>.Shared.Return(buffer);
            GC.SuppressFinalize(this);
        }
        disposed = true;
    }

    /// <summary>
    /// 析构函数
    /// </summary>
    ~ArrayPoolBufferWriter()
    {
        Dispose();
    }
}