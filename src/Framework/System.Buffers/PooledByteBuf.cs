
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
        ObjectDisposedThrowIfDisposed();
        if (_writerIndex + count <= _buffer.Length) return;
        int newSize = Math.Max(_buffer.Length * 2, _writerIndex + count);
        byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        Array.Copy(_buffer, 0, newBuffer, 0, _writerIndex);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }

    private void CheckReadable(int count)
    {
        ObjectDisposedThrowIfDisposed();
        if (_writerIndex - _readerIndex < count)
            throw new IndexOutOfRangeException("可读字节不足");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ObjectDisposedThrowIfDisposed()
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(PooledByteBuf));
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteShort(short value) { EnsureWritable(2); BinaryPrimitives.WriteInt16BigEndian(GetWriteSpan(2), value); _writerIndex += 2; return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteShortLE(short value) { EnsureWritable(2); BinaryPrimitives.WriteInt16LittleEndian(GetWriteSpan(2), value); _writerIndex += 2; return this; }

    // Unsigned Short (2 bytes)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteUnsignedShort(ushort value) { EnsureWritable(2); BinaryPrimitives.WriteUInt16BigEndian(GetWriteSpan(2), value); _writerIndex += 2; return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteUnsignedShortLE(ushort value) { EnsureWritable(2); BinaryPrimitives.WriteUInt16LittleEndian(GetWriteSpan(2), value); _writerIndex += 2; return this; }

    // Int (4 bytes)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteInt(int value) { EnsureWritable(4); BinaryPrimitives.WriteInt32BigEndian(GetWriteSpan(4), value); _writerIndex += 4; return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteIntLE(int value) { EnsureWritable(4); BinaryPrimitives.WriteInt32LittleEndian(GetWriteSpan(4), value); _writerIndex += 4; return this; }

    // Unsigned Int (4 bytes)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteUnsignedInt(uint value) { EnsureWritable(4); BinaryPrimitives.WriteUInt32BigEndian(GetWriteSpan(4), value); _writerIndex += 4; return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteUnsignedIntLE(uint value) { EnsureWritable(4); BinaryPrimitives.WriteUInt32LittleEndian(GetWriteSpan(4), value); _writerIndex += 4; return this; }

    // Long (8 bytes)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteLong(long value) { EnsureWritable(8); BinaryPrimitives.WriteInt64BigEndian(GetWriteSpan(8), value); _writerIndex += 8; return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteLongLE(long value) { EnsureWritable(8); BinaryPrimitives.WriteInt64LittleEndian(GetWriteSpan(8), value); _writerIndex += 8; return this; }

    // Unsigned Long (8 bytes)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteUnsignedLong(ulong value) { EnsureWritable(8); BinaryPrimitives.WriteUInt64BigEndian(GetWriteSpan(8), value); _writerIndex += 8; return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteUnsignedLongLE(ulong value) { EnsureWritable(8); BinaryPrimitives.WriteUInt64LittleEndian(GetWriteSpan(8), value); _writerIndex += 8; return this; }

    // Float (4 bytes)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteFloat(float value) => WriteInt(BitConverter.SingleToInt32Bits(value));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteFloatLE(float value) => WriteIntLE(BitConverter.SingleToInt32Bits(value));

    // Double (8 bytes)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteDouble(double value) => WriteLong(BitConverter.DoubleToInt64Bits(value));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    // Byte
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte() { CheckReadable(1); return _buffer[_readerIndex++]; }

    // Short
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadShort() { CheckReadable(2); var v = BinaryPrimitives.ReadInt16BigEndian(GetReadSpan(2)); _readerIndex += 2; return v; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadShortLE() { CheckReadable(2); var v = BinaryPrimitives.ReadInt16LittleEndian(GetReadSpan(2)); _readerIndex += 2; return v; }

    // Unsigned Short
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUnsignedShort() { CheckReadable(2); var v = BinaryPrimitives.ReadUInt16BigEndian(GetReadSpan(2)); _readerIndex += 2; return v; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUnsignedShortLE() { CheckReadable(2); var v = BinaryPrimitives.ReadUInt16LittleEndian(GetReadSpan(2)); _readerIndex += 2; return v; }

    // Int
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt() { CheckReadable(4); var v = BinaryPrimitives.ReadInt32BigEndian(GetReadSpan(4)); _readerIndex += 4; return v; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadIntLE() { CheckReadable(4); var v = BinaryPrimitives.ReadInt32LittleEndian(GetReadSpan(4)); _readerIndex += 4; return v; }

    // Unsigned Int
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUnsignedInt() { CheckReadable(4); var v = BinaryPrimitives.ReadUInt32BigEndian(GetReadSpan(4)); _readerIndex += 4; return v; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUnsignedIntLE() { CheckReadable(4); var v = BinaryPrimitives.ReadUInt32LittleEndian(GetReadSpan(4)); _readerIndex += 4; return v; }

    // Long
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLong() { CheckReadable(8); var v = BinaryPrimitives.ReadInt64BigEndian(GetReadSpan(8)); _readerIndex += 8; return v; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLongLE() { CheckReadable(8); var v = BinaryPrimitives.ReadInt64LittleEndian(GetReadSpan(8)); _readerIndex += 8; return v; }

    // Unsigned Long
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadUnsignedLong() { CheckReadable(8); var v = BinaryPrimitives.ReadUInt64BigEndian(GetReadSpan(8)); _readerIndex += 8; return v; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadUnsignedLongLE() { CheckReadable(8); var v = BinaryPrimitives.ReadUInt64LittleEndian(GetReadSpan(8)); _readerIndex += 8; return v; }

    // Float
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat() => BitConverter.Int32BitsToSingle(ReadInt());
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloatLE() => BitConverter.Int32BitsToSingle(ReadIntLE());

    // Double
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadLong());
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDoubleLE() => BitConverter.Int64BitsToDouble(ReadLongLE());

    // --- 辅助 Span 获取 ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<byte> GetWriteSpan(int length) => _buffer.AsSpan(_writerIndex, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> GetReadSpan(int length) => _buffer.AsSpan(_readerIndex, length);

    // --- Get 随机访问（不移动 ReaderIndex）---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckIndex(int index, int length)
    {
        ObjectDisposedThrowIfDisposed();
        if ((uint)index > (uint)_writerIndex || length < 0 || (uint)(index + length) > (uint)_writerIndex)
            throw new IndexOutOfRangeException($"索引越界: index={index}, length={length}, writerIndex={_writerIndex}");
    }

    /// <summary>获取指定索引处的字节（不移动读取指针）</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetByte(int index) { CheckIndex(index, 1); return _buffer[index]; }

    /// <summary>获取指定索引处的有符号短整型（大端，不移动读取指针）</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short GetShort(int index) { CheckIndex(index, 2); return BinaryPrimitives.ReadInt16BigEndian(_buffer.AsSpan(index, 2)); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short GetShortLE(int index) { CheckIndex(index, 2); return BinaryPrimitives.ReadInt16LittleEndian(_buffer.AsSpan(index, 2)); }

    /// <summary>获取指定索引处的无符号短整型（大端，不移动读取指针）</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetUnsignedShort(int index) { CheckIndex(index, 2); return BinaryPrimitives.ReadUInt16BigEndian(_buffer.AsSpan(index, 2)); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetUnsignedShortLE(int index) { CheckIndex(index, 2); return BinaryPrimitives.ReadUInt16LittleEndian(_buffer.AsSpan(index, 2)); }

    /// <summary>获取指定索引处的有符号整型（大端，不移动读取指针）</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetInt(int index) { CheckIndex(index, 4); return BinaryPrimitives.ReadInt32BigEndian(_buffer.AsSpan(index, 4)); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetIntLE(int index) { CheckIndex(index, 4); return BinaryPrimitives.ReadInt32LittleEndian(_buffer.AsSpan(index, 4)); }

    /// <summary>获取指定索引处的无符号整型（大端，不移动读取指针）</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetUnsignedInt(int index) { CheckIndex(index, 4); return BinaryPrimitives.ReadUInt32BigEndian(_buffer.AsSpan(index, 4)); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetUnsignedIntLE(int index) { CheckIndex(index, 4); return BinaryPrimitives.ReadUInt32LittleEndian(_buffer.AsSpan(index, 4)); }

    /// <summary>获取指定索引处的有符号长整型（大端，不移动读取指针）</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long GetLong(int index) { CheckIndex(index, 8); return BinaryPrimitives.ReadInt64BigEndian(_buffer.AsSpan(index, 8)); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long GetLongLE(int index) { CheckIndex(index, 8); return BinaryPrimitives.ReadInt64LittleEndian(_buffer.AsSpan(index, 8)); }

    /// <summary>获取指定索引处的无符号长整型（大端，不移动读取指针）</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong GetUnsignedLong(int index) { CheckIndex(index, 8); return BinaryPrimitives.ReadUInt64BigEndian(_buffer.AsSpan(index, 8)); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong GetUnsignedLongLE(int index) { CheckIndex(index, 8); return BinaryPrimitives.ReadUInt64LittleEndian(_buffer.AsSpan(index, 8)); }

    /// <summary>获取指定索引处的单精度浮点数（大端，不移动读取指针）</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetFloat(int index) => BitConverter.Int32BitsToSingle(GetInt(index));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetFloatLE(int index) => BitConverter.Int32BitsToSingle(GetIntLE(index));

    /// <summary>获取指定索引处的双精度浮点数（大端，不移动读取指针）</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetDouble(int index) => BitConverter.Int64BitsToDouble(GetLong(index));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetDoubleLE(int index) => BitConverter.Int64BitsToDouble(GetLongLE(index));

    // --- Set 随机写入（不移动 WriterIndex）---
    /// <summary>在指定索引处写入字节（不移动写入指针）</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetByte(int index, byte value) { CheckIndex(index, 1); _buffer[index] = value; return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetUnsignedShort(int index, ushort value) { CheckIndex(index, 2); BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(index, 2), value); return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetUnsignedShortLE(int index, ushort value) { CheckIndex(index, 2); BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(index, 2), value); return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetUnsignedInt(int index, uint value) { CheckIndex(index, 4); BinaryPrimitives.WriteUInt32BigEndian(_buffer.AsSpan(index, 4), value); return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetUnsignedIntLE(int index, uint value) { CheckIndex(index, 4); BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(index, 4), value); return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetUnsignedLong(int index, ulong value) { CheckIndex(index, 8); BinaryPrimitives.WriteUInt64BigEndian(_buffer.AsSpan(index, 8), value); return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetUnsignedLongLE(int index, ulong value) { CheckIndex(index, 8); BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan(index, 8), value); return this; }

    /// <summary>
    /// 跳过指定长度的可读字节（移动 ReaderIndex）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SkipBytes(int length) { CheckReadable(length); _readerIndex += length; return this; }

    /// <summary>
    /// 清空缓冲区，重置读写指针（不释放底层数组）
    /// </summary>
    public PooledByteBuf Clear()
    {
        _readerIndex = 0;
        _writerIndex = 0;
        return this;
    }
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