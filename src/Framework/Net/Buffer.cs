
using System;
using System.Buffers;
using System.Diagnostics;
using System.Text;
namespace Framework.Net;
/// <summary>
/// Dynamic byte buffer
/// 内部使用的可扩展字节缓冲区，维护数据长度、读取偏移和底层数组容量。
/// </summary>
class Buffer : IBufferWriter<byte>
{
    private byte[] _data;
    // 逻辑末尾（下一个写入位置）与读取指针相互独立，读写移动时需同步维护二者的相对位置。
    private long _size;
    private long _offset;

    /// <summary>
    /// Is the buffer empty?
    /// 判断缓冲区是否尚未包含任何数据。
    /// </summary>
    /// <returns>底层数组尚未初始化或逻辑长度为零时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    public bool IsEmpty => (_data == null) || (_size == 0);
    /// <summary>
    /// Bytes memory buffer
    /// 获取当前缓冲区使用的底层字节数组。
    /// </summary>
    /// <returns>当前底层字节数组；该属性直接返回内部数组。</returns>
    public byte[] Data => _data;
    /// <summary>
    /// Bytes memory buffer capacity
    /// 获取底层字节数组的容量。
    /// </summary>
    /// <returns>底层字节数组可容纳的字节数。</returns>
    public long Capacity => _data.Length;
    /// <summary>
    /// Bytes memory buffer size
    /// 获取当前记录的逻辑长度。
    /// </summary>
    /// <returns>当前记录的缓冲区逻辑长度。</returns>
    public long Size => _size;
    /// <summary>
    /// Bytes memory buffer offset
    /// 获取当前缓冲区读取位置。
    /// </summary>
    /// <returns>当前缓冲区读取位置相对于底层数组起点的偏移量。</returns>
    public long Offset => _offset;

    /// <summary>
    /// Buffer indexer operator
    /// 按逻辑索引读取缓冲区中的字节；实现会先检查索引范围，再叠加当前读取偏移量访问底层数组。
    /// </summary>
    /// <param name="index">逻辑索引，取值范围为 0 至 <see cref="Size"/> - 1；实际访问底层数组时会叠加 <see cref="Offset"/>。</param>
    /// <returns>逻辑索引对应的字节。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="index"/> 小于 0 或大于等于 <see cref="Size"/> 时抛出。</exception>
    public byte this[long index]
    {
        get
        {
            if (index < 0 || index >= _size)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _data[_offset + index];  // 如果希望支持逻辑索引
                                            // 或保持原样，但文档需明确说明是物理索引
        }
    }

    /// <summary>
    /// Initialize a new expandable buffer with zero capacity
    /// 创建容量为零的空缓冲区。
    /// </summary>
    public Buffer() { _data = []; _size = 0; _offset = 0; }
    /// <summary>
    /// Initialize a new expandable buffer with the given capacity
    /// 创建具有指定初始容量的可扩展空缓冲区。
    /// </summary>
    /// <param name="capacity">初始底层字节数组容量；必须为非负数。</param>
    public Buffer(int capacity) { _data = new byte[capacity]; _size = 0; _offset = 0; }
    /// <summary>
    /// Initialize a new expandable buffer with the given data
    /// 使用已有字节数据创建缓冲区。
    /// </summary>
    /// <param name="data">作为初始内容的字节数组；该数组引用由缓冲区直接保存。</param>
    public Buffer(byte[] data) { _data = data; _size = data.Length; _offset = 0; }

    #region Memory buffer methods

    /// <summary>
    /// Get a span of bytes from the current buffer
    /// 获取从当前读取位置开始、长度等于"已写入但尚未消费"字节数的 <see cref="Span{T}"/>。
    /// </summary>
    /// <returns>从 <see cref="Offset"/> 开始、长度为 <c>Size - Offset</c> 的字节跨度。</returns>
    /// <remarks>
    /// 原实现以 <see cref="Size"/> 作为长度，在 <see cref="Offset"/> 非零时会导致 <c>AsSpan</c> 越过数组边界。
    /// 这里改为与 <see cref="AsReadableSpan"/> 一致：只覆盖尚未被读取指针消费的有效数据。
    /// </remarks>
    public Span<byte> AsSpan()
    {
        int readable = (int)(_size - _offset);
        if (readable < 0)
            readable = 0;
        return _data.AsSpan((int)_offset, readable);
    }
    // 获取当前可读区域的 Span（考虑 _offset）
    /// <summary>
    /// 获取当前可读区域的 <see cref="Span{T}"/>，已考虑 <c>_offset</c> 偏移后剩余的有效字节。
    /// </summary>
    /// <returns>从 <c>_offset</c> 开始、长度为 <c>_size - _offset</c> 的字节跨度。</returns>
    public Span<byte> AsReadableSpan()
        => _data.AsSpan((int)_offset, (int)(_size - _offset));

        
    /// <summary>
    /// Get a string from the current buffer
    /// 将缓冲区前 <see cref="Size"/> 个字节按 UTF-8 解码为字符串。
    /// </summary>
    /// <returns>将底层数组从索引 0 开始的前 <see cref="Size"/> 个字节按 UTF-8 解码得到的字符串。</returns>
    public override string ToString()
    {
        return ExtractString(0, _size);
    }

    /// <summary>
    /// Clear the current buffer and its offset
    /// 清空当前缓冲区，并将读取偏移重置为起点。
    /// </summary>
    public void Clear()
    {
        _size = 0;
        _offset = 0;
    }

    /// <summary>
    /// Extract the string from buffer of the given offset and size
    /// 提取指定字节范围并按 UTF-8 解码为字符串。
    /// </summary>
    /// <param name="offset">相对于底层字节数组的起始索引。</param>
    /// <param name="size">要解码的字节数。</param>
    /// <returns>指定字节范围按 UTF-8 解码得到的字符串。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="offset"/> 与 <paramref name="size"/> 的和大于 <see cref="Size"/> 时抛出。</exception>
    public string ExtractString(long offset, long size)
    {
        Debug.Assert(((offset + size) <= Size), "Invalid offset & size!");
        if ((offset + size) > Size)
            throw new ArgumentException("Invalid offset & size!", nameof(offset));

        return Encoding.UTF8.GetString(_data, (int)offset, (int)size);
    }

    /// <summary>
    /// Remove the buffer of the given offset and size
    /// 从缓冲区中移除指定字节范围，并前移后续有效数据。
    /// </summary>
    /// <param name="offset">相对于底层字节数组的待移除起始索引。</param>
    /// <param name="size">要移除的字节数。</param>
    /// <exception cref="ArgumentException">当 <paramref name="offset"/> 与 <paramref name="size"/> 的和大于 <see cref="Size"/> 时抛出。</exception>
    public void Remove(long offset, long size)
    {
        Debug.Assert(((offset + size) <= Size), "Invalid offset & size!");
        if ((offset + size) > Size)
            throw new ArgumentException("Invalid offset & size!", nameof(offset));

        // 将被移除区间之后的有效字节前移，保持底层数组从索引 0 开始连续存放有效数据。
        Array.Copy(_data, offset + size, _data, offset, _size - size - offset);
        _size -= size;
        if (_offset >= (offset + size))
            _offset -= size;
        else if (_offset >= offset)
        {
            _offset -= _offset - offset;
            if (_offset > Size)
                _offset = Size;
        }
    }

    /// <summary>
    /// Reserve the buffer of the given capacity
    /// 确保底层字节数组至少具有指定容量。
    /// </summary>
    /// <param name="capacity">期望的最小底层数组容量，必须为非负数。</param>
    /// <exception cref="ArgumentException">当 <paramref name="capacity"/> 小于 0 时抛出。</exception>
    public void Reserve(long capacity)
    {
        Debug.Assert((capacity >= 0), "Invalid reserve capacity!");
        if (capacity < 0)
            throw new ArgumentException("Invalid reserve capacity!", nameof(capacity));

        if (capacity > Capacity)
        {
            byte[] data = new byte[Math.Max(capacity, 2 * Capacity)];
            Array.Copy(_data, 0, data, 0, _size);
            _data = data;
        }
    }

    /// <summary>
    /// Resize the current buffer
    /// 调整缓冲区逻辑长度，并在必要时扩展底层数组。
    /// </summary>
    /// <param name="size">新的逻辑长度，必须为非负数。</param>
    public void Resize(long size)
    {
        Reserve(size);
        _size = size;
        if (_offset > _size)
            _offset = _size;
    }

    /// <summary>
    /// Shift the current buffer offset
    /// 按指定增量移动当前读取偏移量。
    /// </summary>
    /// <param name="offset">要叠加到当前偏移量上的字节数，可为负数；调整后的偏移量必须位于 0 至 <see cref="Size"/> 之间。</param>
    /// <exception cref="ArgumentOutOfRangeException">当调整后的偏移量超出 0 至 <see cref="Size"/> 的范围时抛出。</exception>
    public void Shift(long offset)
    {
        long newOffset = _offset + offset;
        if (newOffset < 0 || newOffset > _size)
            throw new ArgumentOutOfRangeException(nameof(offset));
        _offset = newOffset;
    }
    /// <summary>
    /// Unshift the current buffer offset
    /// 按指定增量回退当前读取偏移量。
    /// </summary>
    /// <param name="offset">要从当前偏移量中扣除的字节数；方法不会检查结果是否小于 0。</param>
    public void Unshift(long offset) { _offset -= offset; }

    #endregion

    #region Buffer I/O methods

    /// <summary>
    /// Append the single byte
    /// 将一个字节追加到缓冲区末尾。
    /// </summary>
    /// <param name="value">Byte value to append</param>
    /// <returns>Count of append bytes</returns>
    public long Append(byte value)
    {
        Reserve(_size + 1);
        _data[_size] = value;
        _size += 1;
        return 1;
    }

    /// <summary>
    /// Append the given buffer
    /// 将字节数组的全部内容追加到缓冲区末尾。
    /// </summary>
    /// <param name="buffer">Buffer to append</param>
    /// <returns>Count of append bytes</returns>
    public long Append(byte[] buffer)
    {
        Reserve(_size + buffer.Length);
        Array.Copy(buffer, 0, _data, _size, buffer.Length);
        _size += buffer.Length;
        return buffer.Length;
    }

    /// <summary>
    /// Append the given buffer fragment
    /// 将源字节数组的指定范围追加到缓冲区末尾。
    /// </summary>
    /// <param name="buffer">Buffer to append</param>
    /// <param name="offset">Buffer offset</param>
    /// <param name="size">Buffer size</param>
    /// <returns>Count of append bytes</returns>
    /// <remarks>调用方需确保 <paramref name="offset"/> 与 <paramref name="size"/> 构成 <paramref name="buffer"/> 内的有效范围。</remarks>
    public long Append(byte[] buffer, long offset, long size)
    {
        Reserve(_size + size);
        Array.Copy(buffer, offset, _data, _size, size);
        _size += size;
        return size;
    }

    /// <summary>
    /// Append the given span of bytes
    /// 将只读字节跨度追加到缓冲区末尾。
    /// </summary>
    /// <param name="buffer">Buffer to append as a span of bytes</param>
    /// <returns>Count of append bytes</returns>
    public long Append(ReadOnlySpan<byte> buffer)
    {
        Reserve(_size + buffer.Length);
        buffer.CopyTo(new Span<byte>(_data, (int)_size, buffer.Length));
        _size += buffer.Length;
        return buffer.Length;
    }

    /// <summary>
    /// Append the given buffer
    /// 将另一个缓冲区的当前内容追加到本缓冲区末尾。
    /// </summary>
    /// <param name="buffer">Buffer to append</param>
    /// <returns>Count of append bytes</returns>
    public long Append(Buffer buffer) => Append(buffer.AsSpan());

    /// <summary>
    /// Append the given text in UTF-8 encoding
    /// 将字符串按 UTF-8 编码追加到缓冲区末尾。
    /// </summary>
    /// <param name="text">Text to append</param>
    /// <returns>Count of append bytes</returns>
    public long Append(string text)
    {
        int length = Encoding.UTF8.GetMaxByteCount(text.Length);
        Reserve(_size + length);
        long result = Encoding.UTF8.GetBytes(text, 0, text.Length, _data, (int)_size);
        _size += result;
        return result;
    }

    /// <summary>
    /// Append the given text in UTF-8 encoding
    /// 将字符跨度按 UTF-8 编码追加到缓冲区末尾。
    /// </summary>
    /// <param name="text">Text to append as a span of characters</param>
    /// <returns>Count of append bytes</returns>
    public long Append(ReadOnlySpan<char> text)
    {
        int length = Encoding.UTF8.GetMaxByteCount(text.Length);
        Reserve(_size + length);
        long result = Encoding.UTF8.GetBytes(text, new Span<byte>(_data, (int)_size, length));
        _size += result;
        return result;
    }

    /// <summary>
    /// 推进缓冲区已写入的逻辑长度。
    /// </summary>
    /// <param name="count">要增加的字节数；调用方需确保该值不会使逻辑长度超过底层数组容量。</param>
    public void Advance(int count)
    {
        _size += count;
        // 可选：更新 _offset 或其他状态
    }

    /// <summary>
    /// 获取从当前逻辑长度末尾开始、延伸至底层数组末尾的可写内存，供 <see cref="IBufferWriter{T}"/> 调用方使用。
    /// </summary>
    /// <param name="sizeHint">调用方建议的最小可用字节数；本实现不依据该值扩大缓冲区。</param>
    /// <returns>指向底层数组未使用尾部区域的 <see cref="Memory{T}"/>。</returns>
    public Memory<byte> GetMemory(int sizeHint = 0)
        => _data.AsMemory((int)_size, (int)(Capacity - _size));

    /// <summary>
    /// 获取从当前逻辑长度末尾开始、延伸至底层数组末尾的可写跨度，供 <see cref="IBufferWriter{T}"/> 调用方使用。
    /// </summary>
    /// <param name="sizeHint">调用方建议的最小可用字节数；本实现不依据该值扩大缓冲区。</param>
    /// <returns>指向底层数组未使用尾部区域的 <see cref="Span{T}"/>。</returns>
    public Span<byte> GetSpan(int sizeHint = 0)
        => _data.AsSpan((int)_size, (int)(Capacity - _size));

    #endregion
}