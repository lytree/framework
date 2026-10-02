
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
    private int _markedReaderIndex;
    private int _markedWriterIndex;
    private bool _isDisposed;

    /// <summary>最大容量上限（默认 int.MaxValue）</summary>
    public int MaxCapacity { get; init; } = int.MaxValue;

    public int ReaderIndex => _readerIndex;

    public int WriterIndex => _writerIndex;

    public int ReadableBytes => _writerIndex - _readerIndex;

    /// <summary>当前可写字节数（Capacity - WriterIndex）</summary>
    public int WritableBytes => _buffer.Length - _writerIndex;

    /// <summary>最大可写字节数（MaxCapacity - WriterIndex）</summary>
    public int MaxWritableBytes => MaxCapacity - _writerIndex;

    public int Capacity => _buffer.Length;

    /// <summary>是否还有可读字节</summary>
    public bool IsReadable => ReadableBytes > 0;

    /// <summary>是否还能写入</summary>
    public bool IsWritable => WritableBytes > 0;

    public PooledByteBuf(int initialCapacity = 256)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
    }

    /// <summary>判断是否有指定长度的可读数据</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsReadableBytes(int size) => ReadableBytes >= size;

    /// <summary>判断是否有指定长度的可写空间</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWritableBytes(int size) => WritableBytes >= size;

    // --- 索引设置 ---
    /// <summary>设置读取指针位置，必须在 [0, WriterIndex] 范围内</summary>
    public PooledByteBuf SetReaderIndex(int readerIndex)
    {
        ObjectDisposedThrowIfDisposed();
        if ((uint)readerIndex > (uint)_writerIndex)
            throw new IndexOutOfRangeException($"readerIndex({readerIndex}) 超出范围 [0, {_writerIndex}]");
        _readerIndex = readerIndex;
        return this;
    }

    /// <summary>设置写入指针位置，必须在 [ReaderIndex, Capacity] 范围内</summary>
    public PooledByteBuf SetWriterIndex(int writerIndex)
    {
        ObjectDisposedThrowIfDisposed();
        if ((uint)writerIndex > (uint)_buffer.Length || writerIndex < _readerIndex)
            throw new IndexOutOfRangeException($"writerIndex({writerIndex}) 超出范围 [{_readerIndex}, {_buffer.Length}]");
        _writerIndex = writerIndex;
        return this;
    }

    /// <summary>同时设置读写指针</summary>
    public PooledByteBuf SetIndex(int readerIndex, int writerIndex)
    {
        SetReaderIndex(readerIndex);
        SetWriterIndex(writerIndex);
        return this;
    }

    // --- Mark/Reset 模式（支持回退到标记位置，常用于协议解析）---
    /// <summary>标记当前读取位置</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf MarkReaderIndex() { _markedReaderIndex = _readerIndex; return this; }

    /// <summary>回退读取位置到标记处</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf ResetReaderIndex() { SetReaderIndex(_markedReaderIndex); return this; }

    /// <summary>标记当前写入位置</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf MarkWriterIndex() { _markedWriterIndex = _writerIndex; return this; }

    /// <summary>回退写入位置到标记处</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf ResetWriterIndex() { SetWriterIndex(_markedWriterIndex); return this; }

    // --- 内部检查与扩容 ---
    private void EnsureWritable(int count)
    {
        ObjectDisposedThrowIfDisposed();
        if (_writerIndex + count <= _buffer.Length) return;
        int newSize = Math.Max(_buffer.Length * 2, _writerIndex + count);
        if (newSize > MaxCapacity) newSize = MaxCapacity;
        if (_writerIndex + count > MaxCapacity)
            throw new InvalidOperationException($"超过最大容量限制: 需要 {_writerIndex + count}, 最大 {MaxCapacity}");
        byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        _buffer.AsSpan(0, _writerIndex).CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }

    /// <summary>
    /// 尝试确保可写空间，返回状态码（参考 DotNetty）
    /// 0=容量足够无需扩容; 1=空间不足且未扩容(force=false); 2=已扩容; 3=扩到 MaxCapacity 仍不足
    /// </summary>
    public int EnsureWritable(int minWritableBytes, bool force)
    {
        ObjectDisposedThrowIfDisposed();
        if (minWritableBytes <= WritableBytes) return 0;
        if (minWritableBytes > MaxWritableBytes)
        {
            if (force)
            {
                if (MaxCapacity - _writerIndex > WritableBytes)
                {
                    EnsureWritable(MaxCapacity - _writerIndex);
                    return 3;
                }
                return 3;
            }
            return 1;
        }
        int newCapacity = CalculateNewCapacity(_writerIndex + minWritableBytes, MaxCapacity);
        int oldSize = _buffer.Length;
        byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newCapacity);
        _buffer.AsSpan(0, _writerIndex).CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
        return oldSize == _buffer.Length ? 0 : 2;
    }

    /// <summary>按 2 的幂次计算新容量（参考 DotNetty CalculateNewCapacity）</summary>
    private static int CalculateNewCapacity(int minNewCapacity, int maxCapacity)
    {
        if (minNewCapacity < 0) throw new ArgumentOutOfRangeException(nameof(minNewCapacity));
        if (minNewCapacity > maxCapacity) throw new InvalidOperationException($"超过最大容量限制: {minNewCapacity} > {maxCapacity}");
        const int Threshold = 4 * 1024 * 1024; // 4MB 阈值
        if (minNewCapacity == Threshold) return Threshold;
        if (minNewCapacity > Threshold)
        {
            int newCapacity = minNewCapacity | (minNewCapacity >> 1);
            newCapacity |= newCapacity >> 2;
            newCapacity |= newCapacity >> 4;
            newCapacity |= newCapacity >> 8;
            newCapacity |= newCapacity >> 16;
            newCapacity = (newCapacity + 1) & ~newCapacity;
            return newCapacity > maxCapacity ? maxCapacity : newCapacity;
        }
        int capacity = 64;
        while (capacity < minNewCapacity) capacity <<= 1;
        return capacity;
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

    // Boolean (1 byte)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteBoolean(bool value) => WriteByte(value ? (byte)1 : (byte)0);

    // Char (2 bytes, UTF-16)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteChar(char value) => WriteShort((short)value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteCharLE(char value) => WriteShortLE((short)value);

    // Medium (3 bytes, 24-bit int, 常用于 DNS/MQTT/RTSP 协议)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteMedium(int value) { EnsureWritable(3); WriteMediumImpl(GetWriteSpan(3), value, false); _writerIndex += 3; return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteMediumLE(int value) { EnsureWritable(3); WriteMediumImpl(GetWriteSpan(3), value, true); _writerIndex += 3; return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteUnsignedMedium(int value) => WriteMedium(value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf WriteUnsignedMediumLE(int value) => WriteMediumLE(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteMediumImpl(Span<byte> span, int value, bool littleEndian)
    {
        if (littleEndian)
        {
            span[0] = (byte)value;
            span[1] = (byte)(value >> 8);
            span[2] = (byte)(value >> 16);
        }
        else
        {
            span[0] = (byte)(value >> 16);
            span[1] = (byte)(value >> 8);
            span[2] = (byte)value;
        }
    }

    /// <summary>写入 length 个 0 字节，推进 WriterIndex</summary>
    public PooledByteBuf WriteZero(int length)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        if (length == 0) return this;
        EnsureWritable(length);
        _buffer.AsSpan(_writerIndex, length).Clear();
        _writerIndex += length;
        return this;
    }

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

    // Boolean
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBoolean() => ReadByte() != 0;

    // Char (2 bytes, UTF-16)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public char ReadChar() => (char)ReadShort();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public char ReadCharLE() => (char)ReadShortLE();

    // Medium (3 bytes, 24-bit int)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadUnsignedMedium() { CheckReadable(3); var v = ReadUnsignedMediumImpl(GetReadSpan(3), false); _readerIndex += 3; return v; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadUnsignedMediumLE() { CheckReadable(3); var v = ReadUnsignedMediumImpl(GetReadSpan(3), true); _readerIndex += 3; return v; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadMedium() => SignExtendMedium(ReadUnsignedMedium());
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadMediumLE() => SignExtendMedium(ReadUnsignedMediumLE());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReadUnsignedMediumImpl(ReadOnlySpan<byte> span, bool littleEndian)
    {
        return littleEndian
            ? span[0] | (span[1] << 8) | (span[2] << 16)
            : (span[0] << 16) | (span[1] << 8) | span[2];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SignExtendMedium(int value) => (value & 0x800000) != 0 ? value | unchecked((int)0xff000000) : value;

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

    /// <summary>获取指定索引处的布尔值（1字节，不移动读取指针）</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetBoolean(int index) { CheckIndex(index, 1); return _buffer[index] != 0; }

    /// <summary>获取指定索引处的字符（2字节 UTF-16，不移动读取指针）</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public char GetChar(int index) => (char)GetShort(index);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public char GetCharLE(int index) => (char)GetShortLE(index);

    /// <summary>获取指定索引处的 3 字节无符号整型（不移动读取指针）</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetUnsignedMedium(int index) { CheckIndex(index, 3); return ReadUnsignedMediumImpl(_buffer.AsSpan(index, 3), false); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetUnsignedMediumLE(int index) { CheckIndex(index, 3); return ReadUnsignedMediumImpl(_buffer.AsSpan(index, 3), true); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetMedium(int index) => SignExtendMedium(GetUnsignedMedium(index));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetMediumLE(int index) => SignExtendMedium(GetUnsignedMediumLE(index));

    /// <summary>获取指定索引处指定长度的字节副本</summary>
    public byte[] GetBytes(int index, int length)
    {
        CheckIndex(index, length);
        byte[] result = new byte[length];
        _buffer.AsSpan(index, length).CopyTo(result);
        return result;
    }

    /// <summary>将指定索引处的字节拷贝到目标数组（不移动读写指针）</summary>
    public PooledByteBuf GetBytes(int index, byte[] dst, int dstIndex, int length)
    {
        CheckIndex(index, length);
        _buffer.AsSpan(index, length).CopyTo(dst.AsSpan(dstIndex, length));
        return this;
    }

    /// <summary>将指定索引处的字节拷贝到目标 Span（不移动读写指针）</summary>
    public PooledByteBuf GetBytes(int index, Span<byte> dst)
    {
        CheckIndex(index, dst.Length);
        _buffer.AsSpan(index, dst.Length).CopyTo(dst);
        return this;
    }

    // --- Set 随机写入（不移动 WriterIndex）---
    /// <summary>在指定索引处写入字节（不移动写入指针）</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetByte(int index, byte value) { CheckIndex(index, 1); _buffer[index] = value; return this; }

    // 有符号
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetShort(int index, short value) { CheckIndex(index, 2); BinaryPrimitives.WriteInt16BigEndian(_buffer.AsSpan(index, 2), value); return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetShortLE(int index, short value) { CheckIndex(index, 2); BinaryPrimitives.WriteInt16LittleEndian(_buffer.AsSpan(index, 2), value); return this; }
    // 无符号
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetUnsignedShort(int index, ushort value) { CheckIndex(index, 2); BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(index, 2), value); return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetUnsignedShortLE(int index, ushort value) { CheckIndex(index, 2); BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(index, 2), value); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetInt(int index, int value) { CheckIndex(index, 4); BinaryPrimitives.WriteInt32BigEndian(_buffer.AsSpan(index, 4), value); return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetIntLE(int index, int value) { CheckIndex(index, 4); BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(index, 4), value); return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetUnsignedInt(int index, uint value) { CheckIndex(index, 4); BinaryPrimitives.WriteUInt32BigEndian(_buffer.AsSpan(index, 4), value); return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetUnsignedIntLE(int index, uint value) { CheckIndex(index, 4); BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(index, 4), value); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetLong(int index, long value) { CheckIndex(index, 8); BinaryPrimitives.WriteInt64BigEndian(_buffer.AsSpan(index, 8), value); return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetLongLE(int index, long value) { CheckIndex(index, 8); BinaryPrimitives.WriteInt64LittleEndian(_buffer.AsSpan(index, 8), value); return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetUnsignedLong(int index, ulong value) { CheckIndex(index, 8); BinaryPrimitives.WriteUInt64BigEndian(_buffer.AsSpan(index, 8), value); return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetUnsignedLongLE(int index, ulong value) { CheckIndex(index, 8); BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan(index, 8), value); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetFloat(int index, float value) => SetInt(index, BitConverter.SingleToInt32Bits(value));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetFloatLE(int index, float value) => SetIntLE(index, BitConverter.SingleToInt32Bits(value));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetDouble(int index, double value) => SetLong(index, BitConverter.DoubleToInt64Bits(value));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetDoubleLE(int index, double value) => SetLongLE(index, BitConverter.DoubleToInt64Bits(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetBoolean(int index, bool value) { CheckIndex(index, 1); _buffer[index] = value ? (byte)1 : (byte)0; return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetChar(int index, char value) => SetShort(index, (short)value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetCharLE(int index, char value) => SetShortLE(index, (short)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetMedium(int index, int value) { CheckIndex(index, 3); WriteMediumImpl(_buffer.AsSpan(index, 3), value, false); return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetMediumLE(int index, int value) { CheckIndex(index, 3); WriteMediumImpl(_buffer.AsSpan(index, 3), value, true); return this; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetUnsignedMedium(int index, int value) => SetMedium(index, value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SetUnsignedMediumLE(int index, int value) => SetMediumLE(index, value);

    /// <summary>将源数组拷贝到指定索引处（不移动写入指针）</summary>
    public PooledByteBuf SetBytes(int index, byte[] src) => SetBytes(index, src.AsSpan());
    public PooledByteBuf SetBytes(int index, byte[] src, int srcIndex, int length) => SetBytes(index, src.AsSpan(srcIndex, length));
    public PooledByteBuf SetBytes(int index, ReadOnlySpan<byte> src)
    {
        CheckIndex(index, src.Length);
        src.CopyTo(_buffer.AsSpan(index, src.Length));
        return this;
    }

    /// <summary>在指定索引处填充 length 个 0（不移动写入指针）</summary>
    public PooledByteBuf SetZero(int index, int length)
    {
        CheckIndex(index, length);
        _buffer.AsSpan(index, length).Clear();
        return this;
    }

    /// <summary>
    /// 丢弃已读字节，将可读区域压缩到缓冲区头部（参考 DotNetty DiscardReadBytes）
    /// 注意：此操作涉及内存拷贝，频繁调用可能影响性能
    /// </summary>
    public PooledByteBuf DiscardReadBytes()
    {
        ObjectDisposedThrowIfDisposed();
        if (_readerIndex == 0) return this;
        int readable = ReadableBytes;
        if (readable > 0)
        {
            Array.Copy(_buffer, _readerIndex, _buffer, 0, readable);
        }
        // 调整标记位置（参考 DotNetty AdjustMarkers）
        int readerIndex = _readerIndex;
        _markedReaderIndex = Math.Max(_markedReaderIndex - readerIndex, 0);
        _markedWriterIndex = Math.Max(_markedWriterIndex - readerIndex, 0);
        _readerIndex = 0;
        _writerIndex = readable;
        return this;
    }

    /// <summary>
    /// 仅当已读字节超过容量一半时才执行压缩（参考 DotNetty DiscardSomeReadBytes，避免频繁拷贝）
    /// </summary>
    public PooledByteBuf DiscardSomeReadBytes()
    {
        if (_readerIndex >= _buffer.Length >> 1)
        {
            return DiscardReadBytes();
        }
        return this;
    }

    /// <summary>
    /// 跳过指定长度的可读字节（移动 ReaderIndex）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledByteBuf SkipBytes(int length) { CheckReadable(length); _readerIndex += length; return this; }

    /// <summary>
    /// 清空缓冲区，重置读写指针与标记（不释放底层数组，便于复用）
    /// </summary>
    public PooledByteBuf Clear()
    {
        _readerIndex = 0;
        _writerIndex = 0;
        _markedReaderIndex = 0;
        _markedWriterIndex = 0;
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
        _buffer.AsSpan(0, _writerIndex).CopyTo(result);
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
}