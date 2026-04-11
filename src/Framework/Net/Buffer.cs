
using System;
using System.Buffers;
using System.Diagnostics;
using System.Text;
namespace Framework.Net;
/// <summary>
/// Dynamic byte buffer
/// </summary>
class Buffer : IBufferWriter<byte>
{
    private byte[] _data;
    private long _size;
    private long _offset;

    /// <summary>
    /// Is the buffer empty?
    /// </summary>
    public bool IsEmpty => (_data == null) || (_size == 0);
    /// <summary>
    /// Bytes memory buffer
    /// </summary>
    public byte[] Data => _data;
    /// <summary>
    /// Bytes memory buffer capacity
    /// </summary>
    public long Capacity => _data.Length;
    /// <summary>
    /// Bytes memory buffer size
    /// </summary>
    public long Size => _size;
    /// <summary>
    /// Bytes memory buffer offset
    /// </summary>
    public long Offset => _offset;

    /// <summary>
    /// Buffer indexer operator
    /// </summary>
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
    /// </summary>
    public Buffer() { _data = []; _size = 0; _offset = 0; }
    /// <summary>
    /// Initialize a new expandable buffer with the given capacity
    /// </summary>
    public Buffer(int capacity) { _data = new byte[capacity]; _size = 0; _offset = 0; }
    /// <summary>
    /// Initialize a new expandable buffer with the given data
    /// </summary>
    public Buffer(byte[] data) { _data = data; _size = data.Length; _offset = 0; }

    #region Memory buffer methods

    /// <summary>
    /// Get a span of bytes from the current buffer
    /// </summary>
    public Span<byte> AsSpan()
    {
        return _data.AsSpan((int)_offset, (int)_size);
    }
    // 获取当前可读区域的 Span（考虑 _offset）
    public Span<byte> AsReadableSpan()
        => _data.AsSpan((int)_offset, (int)(_size - _offset));

        
    /// <summary>
    /// Get a string from the current buffer
    /// </summary>
    public override string ToString()
    {
        return ExtractString(0, _size);
    }

    /// <summary>
    /// Clear the current buffer and its offset
    /// </summary>
    public void Clear()
    {
        _size = 0;
        _offset = 0;
    }

    /// <summary>
    /// Extract the string from buffer of the given offset and size
    /// </summary>
    public string ExtractString(long offset, long size)
    {
        Debug.Assert(((offset + size) <= Size), "Invalid offset & size!");
        if ((offset + size) > Size)
            throw new ArgumentException("Invalid offset & size!", nameof(offset));

        return Encoding.UTF8.GetString(_data, (int)offset, (int)size);
    }

    /// <summary>
    /// Remove the buffer of the given offset and size
    /// </summary>
    public void Remove(long offset, long size)
    {
        Debug.Assert(((offset + size) <= Size), "Invalid offset & size!");
        if ((offset + size) > Size)
            throw new ArgumentException("Invalid offset & size!", nameof(offset));

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
    /// </summary>
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
    /// </summary>
    public void Resize(long size)
    {
        Reserve(size);
        _size = size;
        if (_offset > _size)
            _offset = _size;
    }

    /// <summary>
    /// Shift the current buffer offset
    /// </summary>
    public void Shift(long offset)
    {
        long newOffset = _offset + offset;
        if (newOffset < 0 || newOffset > _size)
            throw new ArgumentOutOfRangeException(nameof(offset));
        _offset = newOffset;
    }
    /// <summary>
    /// Unshift the current buffer offset
    /// </summary>
    public void Unshift(long offset) { _offset -= offset; }

    #endregion

    #region Buffer I/O methods

    /// <summary>
    /// Append the single byte
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
    /// </summary>
    /// <param name="buffer">Buffer to append</param>
    /// <param name="offset">Buffer offset</param>
    /// <param name="size">Buffer size</param>
    /// <returns>Count of append bytes</returns>
    public long Append(byte[] buffer, long offset, long size)
    {
        Reserve(_size + size);
        Array.Copy(buffer, offset, _data, _size, size);
        _size += size;
        return size;
    }

    /// <summary>
    /// Append the given span of bytes
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
    /// </summary>
    /// <param name="buffer">Buffer to append</param>
    /// <returns>Count of append bytes</returns>
    public long Append(Buffer buffer) => Append(buffer.AsSpan());

    /// <summary>
    /// Append the given text in UTF-8 encoding
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

    public void Advance(int count)
    {
        _size += count;
        // 可选：更新 _offset 或其他状态
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
        => _data.AsMemory((int)_size, (int)(Capacity - _size));

    public Span<byte> GetSpan(int sizeHint = 0)
        => _data.AsSpan((int)_size, (int)(Capacity - _size));

    #endregion
}