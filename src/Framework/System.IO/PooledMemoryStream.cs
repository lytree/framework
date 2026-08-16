using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.IO;

/// <summary>
/// 池化内存流。底层数组从 <see cref="ArrayPool{T}"/> 租借，
/// <see cref="Dispose(bool)"/> 或终结器触发时归还，避免长生命周期对象的 GC 压力。
/// </summary>
public sealed class PooledMemoryStream : Stream, IEnumerable<byte>
{
    private const float OverExpansionFactor = 2;

    private byte[]? _data = Array.Empty<byte>();
    private int _length;
    private readonly ArrayPool<byte> _pool;
    private bool _isDisposed;

    public PooledMemoryStream() : this(ArrayPool<byte>.Shared)
    {
    }

    public PooledMemoryStream(byte[] buffer) : this(ArrayPool<byte>.Shared, buffer.Length)
    {
        Buffer.BlockCopy(buffer, 0, _data!, 0, buffer.Length);
        _length = buffer.Length;
    }

    public PooledMemoryStream(ArrayPool<byte> arrayPool, int capacity = 0)
    {
        _pool = arrayPool ?? throw new ArgumentNullException(nameof(arrayPool));
        if (capacity > 0)
        {
            _data = _pool.Rent(capacity);
        }
    }

    public override bool CanRead => !_isDisposed;

    public override bool CanSeek => !_isDisposed;

    public override bool CanWrite => !_isDisposed;

    public override long Length => _length;

    public override long Position { get; set; }

    public long Capacity => _data?.Length ?? 0;

    public Span<byte> GetSpan() => _data.AsSpan(0, _length);

    public Memory<byte> GetMemory() => _data.AsMemory(0, _length);

    public ArraySegment<byte> ToArraySegment() => new(_data!, 0, _length);

    public override void Flush()
    {
        AssertNotDisposed();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        AssertNotDisposed();
        if (count == 0) return 0;

        var available = (int)Math.Min(count, Length - Position);
        Array.Copy(_data!, Position, buffer, offset, available);
        Position += available;
        return available;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long Seek(long offset, SeekOrigin origin)
    {
        AssertNotDisposed();
        switch (origin)
        {
            case SeekOrigin.Current:
                if (Position + offset < 0 || Position + offset > Capacity)
                    throw new ArgumentOutOfRangeException(nameof(offset));
                Position += offset;
                _length = (int)Math.Max(Position, _length);
                return Position;

            case SeekOrigin.Begin:
                if (offset < 0 || offset > Capacity)
                    throw new ArgumentOutOfRangeException(nameof(offset));
                Position = offset;
                _length = (int)Math.Max(Position, _length);
                return Position;

            case SeekOrigin.End:
                if (Length + offset < 0)
                    throw new ArgumentOutOfRangeException(nameof(offset));
                if (Length + offset > Capacity)
                    SetCapacity((int)(Length + offset));
                Position = Length + offset;
                _length = (int)Math.Max(Position, _length);
                return Position;

            default:
                throw new ArgumentOutOfRangeException(nameof(origin));
        }
    }

    public override void SetLength(long value)
    {
        AssertNotDisposed();
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        if (value > Capacity) SetCapacity((int)value);

        _length = (int)value;
        if (Position > Length) Position = Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Write(byte[] buffer, int offset, int count)
    {
        AssertNotDisposed();
        if (count == 0) return;

        if (Capacity - Position < count)
            SetCapacity((int)(OverExpansionFactor * (Position + count)));

        Array.Copy(buffer, offset, _data!, Position, count);
        Position += count;
        _length = (int)Math.Max(Position, _length);
    }

    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        AssertNotDisposed();
        stream.Write(_data!, 0, _length);
    }

    public byte[] GetBuffer()
    {
        AssertNotDisposed();
        if (_data!.Length == Length) return _data;

        var buffer = new byte[Length];
        Buffer.BlockCopy(_data!, 0, buffer, 0, buffer.Length);
        return buffer;
    }

    public byte[] ToArray() => GetBuffer();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        _isDisposed = true;
        Position = 0;
        _length = 0;

        // 无论 disposing 还是终结器触发，都归还底层 buffer。
        // 仅终结器触发的场景没有托管资源需要释放。
        if (_data is not null)
        {
            _pool.Return(_data);
            _data = null;
        }

        if (disposing)
        {
            GC.SuppressFinalize(this);
        }

        base.Dispose(disposing);
    }

    private void SetCapacity(int newCapacity)
    {
        var newData = _pool.Rent(newCapacity);
        if (_data is not null)
        {
            Array.Copy(_data, 0, newData, 0, _length);
            _pool.Return(_data);
        }
        _data = newData;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<byte> GetEnumerator()
    {
        for (var i = 0; i < Length; i++)
            yield return _data![i];
    }
}

