
namespace System.Buffers;

using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

public partial class PooledByteBuf : IDisposable
{
    private byte[] _buffer;
    private int _readerIndex;
    private int _writerIndex;
    private bool _isDisposed;

    public int ReaderIndex => _readerIndex;

    public int WriterIndex => _writerIndex;

    public int ReadableBytes => _writerIndex - _readerIndex;

    public int Capacity => _buffer.Length;
    public PooledByteBuf(int initialCapacity = 256)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
    }




    // --- 内部检查与扩容 ---
    private void EnsureWritable(int count)
    {
        if (_writerIndex + count <= _buffer.Length) return;
        int newSize = Math.Max(_buffer.Length * 2, _writerIndex + count);
        byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        Array.Copy(_buffer, 0, newBuffer, 0, _writerIndex);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }

    private void CheckReadable(int count)
    {
        if (_writerIndex - _readerIndex < count)
            throw new IndexOutOfRangeException("可读字节不足");
    }
    /// <summary>
    /// 写入固定长度的字符串。
    /// 如果字符串转换后的字节长度不足，将不会补齐；
    /// 如果超过长度，则只读取指定部分（注意：这可能导致字符截断，建议配合固定协议使用）。
    /// </summary>
    public PooledByteBuf WriteStringFixed(string value, int byteLength, Encoding encoding = null)
    {
        encoding ??= Encoding.UTF8;
        EnsureWritable(byteLength);

        // 获取字符串的字节
        byte[] bytes = encoding.GetBytes(value);
        int copyLength = Math.Min(bytes.Length, byteLength);

        // 写入实际字节
        bytes.AsSpan(0, copyLength).CopyTo(_buffer.AsSpan(_writerIndex));

        // 如果不足，可以用 0 填充（可选，视协议而定）
        if (copyLength < byteLength)
        {
            _buffer.AsSpan(_writerIndex + copyLength, byteLength - copyLength).Clear();
        }

        _writerIndex += byteLength;
        return this;
    }

    /// <summary>
    /// 读取指定字节长度的字符串
    /// </summary>
    public string ReadString(int byteLength, Encoding encoding = null)
    {
        encoding ??= Encoding.UTF8;
        CheckReadable(byteLength);

        string result = encoding.GetString(_buffer, _readerIndex, byteLength);
        _readerIndex += byteLength;

        // 通常需要去掉末尾的 \0 (如果是固定长度填充的)
        return result.TrimEnd('\0');
    }

    // --- 写入方法 (Write) ---
    public PooledByteBuf WriteByte(byte v)
    {
        EnsureWritable(1);
        _buffer[_writerIndex++] = v;
        return this;
    }
    // --- Byte Array (写入) ---
    public PooledByteBuf WriteBytes(byte[] src)
    {
        if (src == null) throw new ArgumentNullException(nameof(src));
        return WriteBytes(src.AsSpan());
    }
    public PooledByteBuf WriteBytes(ReadOnlySpan<byte> src)
    {
        EnsureWritable(src.Length);
        src.CopyTo(_buffer.AsSpan(_writerIndex));
        _writerIndex += src.Length;
        return this;
    }
    // Short (2 bytes)
    public PooledByteBuf WriteShort(short value) { EnsureWritable(2); BinaryPrimitives.WriteInt16BigEndian(GetWriteSpan(2), value); _writerIndex += 2; return this; }
    public PooledByteBuf WriteShortLE(short value) { EnsureWritable(2); BinaryPrimitives.WriteInt16LittleEndian(GetWriteSpan(2), value); _writerIndex += 2; return this; }

    // Int (4 bytes)
    public PooledByteBuf WriteInt(int value) { EnsureWritable(4); BinaryPrimitives.WriteInt32BigEndian(GetWriteSpan(4), value); _writerIndex += 4; return this; }
    public PooledByteBuf WriteIntLE(int value) { EnsureWritable(4); BinaryPrimitives.WriteInt32LittleEndian(GetWriteSpan(4), value); _writerIndex += 4; return this; }

    // Long (8 bytes)
    public PooledByteBuf WriteLong(long value) { EnsureWritable(8); BinaryPrimitives.WriteInt64BigEndian(GetWriteSpan(8), value); _writerIndex += 8; return this; }
    public PooledByteBuf WriteLongLE(long value) { EnsureWritable(8); BinaryPrimitives.WriteInt64LittleEndian(GetWriteSpan(8), value); _writerIndex += 8; return this; }

    // Float (4 bytes)
    public PooledByteBuf WriteFloat(float value) => WriteInt(BitConverter.SingleToInt32Bits(value));
    public PooledByteBuf WriteFloatLE(float value) => WriteIntLE(BitConverter.SingleToInt32Bits(value));

    // Double (8 bytes)
    public PooledByteBuf WriteDouble(double value) => WriteLong(BitConverter.DoubleToInt64Bits(value));
    public PooledByteBuf WriteDoubleLE(double value) => WriteLongLE(BitConverter.DoubleToInt64Bits(value));

    // --- 读取方法 (Read) ---
    /// <summary>
    /// 读取指定长度的字节并返回一个新的数组
    /// </summary>
    public byte[] ReadBytes(int length)
    {
        CheckReadable(length);
        byte[] result = new byte[length];
        _buffer.AsSpan(_readerIndex, length).CopyTo(result);
        _readerIndex += length;
        return result;
    }

    /// <summary>
    /// 将数据读取到用户提供的目标数组中
    /// </summary>
    public PooledByteBuf ReadBytes(byte[] dst, int dstIndex, int length)
    {
        CheckReadable(length);
        _buffer.AsSpan(_readerIndex, length).CopyTo(dst.AsSpan(dstIndex, length));
        _readerIndex += length;
        return this;
    }

    /// <summary>
    /// 将数据读取到目标 Span 中（最高效）
    /// </summary>
    public PooledByteBuf ReadBytes(Span<byte> dst)
    {
        CheckReadable(dst.Length);
        _buffer.AsSpan(_readerIndex, dst.Length).CopyTo(dst);
        _readerIndex += dst.Length;
        return this;
    }
    // Short
    public short ReadShort() { CheckReadable(2); var v = BinaryPrimitives.ReadInt16BigEndian(GetReadSpan(2)); _readerIndex += 2; return v; }
    public short ReadShortLE() { CheckReadable(2); var v = BinaryPrimitives.ReadInt16LittleEndian(GetReadSpan(2)); _readerIndex += 2; return v; }

    // Int
    public int ReadInt() { CheckReadable(4); var v = BinaryPrimitives.ReadInt32BigEndian(GetReadSpan(4)); _readerIndex += 4; return v; }
    public int ReadIntLE() { CheckReadable(4); var v = BinaryPrimitives.ReadInt32LittleEndian(GetReadSpan(4)); _readerIndex += 4; return v; }

    // Long
    public long ReadLong() { CheckReadable(8); var v = BinaryPrimitives.ReadInt64BigEndian(GetReadSpan(8)); _readerIndex += 8; return v; }
    public long ReadLongLE() { CheckReadable(8); var v = BinaryPrimitives.ReadInt64LittleEndian(GetReadSpan(8)); _readerIndex += 8; return v; }

    // Float
    public float ReadFloat() => BitConverter.Int32BitsToSingle(ReadInt());
    public float ReadFloatLE() => BitConverter.Int32BitsToSingle(ReadIntLE());

    // Double
    public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadLong());
    public double ReadDoubleLE() => BitConverter.Int64BitsToDouble(ReadLongLE());

    // --- 辅助 Span 获取 ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<byte> GetWriteSpan(int length) => _buffer.AsSpan(_writerIndex, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> GetReadSpan(int length) => _buffer.AsSpan(_readerIndex, length);
    /// <summary>
    /// 返回从 ReaderIndex 到 WriterIndex 之间所有已填充的可读字节数组（执行内存拷贝）
    /// </summary>
    public byte[] ToArray()
    {
        int length = ReadableBytes;
        if (length == 0) return [];

        byte[] result = new byte[length];
        _buffer.AsSpan(_readerIndex, length).CopyTo(result);
        return result;
    }

    /// <summary>
    /// 返回从 0 到 WriterIndex 之间所有填充过的字节数组（执行内存拷贝）
    /// </summary>
    public byte[] ToFullArray()
    {
        if (_writerIndex == 0) return [];

        byte[] result = new byte[_writerIndex];
        Array.Copy(_buffer, 0, result, 0, _writerIndex);
        return result;
    }

    /// <summary>
    /// 【推荐】返回可读部分的 Span 视图。
    /// 优点：零拷贝，性能极高。
    /// 缺点：不能在 Dispose 之后使用。
    /// </summary>
    public Span<byte> ReadSpan()
    {
        return _buffer.AsSpan(_readerIndex, ReadableBytes);
    }

    /// <summary>
    /// 【推荐】返回已写入部分的 Span 视图。
    /// </summary>
    public Span<byte> WrittenSpan()
    {
        return _buffer.AsSpan(0, _writerIndex);
    }
    public void Dispose()
    {
        if (!_isDisposed)
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            GC.SuppressFinalize(this);
            _isDisposed = true;
        }
    }

    /// <summary>
    /// 析构函数
    /// </summary>
    ~PooledByteBuf()
    {
        Dispose();
    }
}