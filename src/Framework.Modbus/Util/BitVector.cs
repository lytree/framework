using System.Text;

namespace Framework.Modbus.Util;

/// <summary>
/// 位向量：以字节数组承载连续的位，可直接映射到 Modbus 的线圈 / 离散输入数据段。
/// 对应 jamod 的 <c>net.wimpi.modbus.util.BitVector</c>。
/// </summary>
/// <remarks>
/// 位序遵循 Modbus 线序：<b>第 0 位是首字节的最低位（LSB）</b>，
/// 即第一个线圈对应首字节 bit0，第二个线圈对应 bit1，依此类推。
/// （Modbus Application Protocol V1.1b3, §6.1）
/// </remarks>
public sealed class BitVector : IEquatable<BitVector>
{
    private byte[] _bytes;
    private int _size;

    /// <summary>以指定位数创建位向量，初值全 0。</summary>
    /// <param name="size">位数，必须大于 0。</param>
    public BitVector(int size)
    {
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "位向量的位数必须大于 0。");
        }

        _bytes = new byte[(size + 7) / 8];
        _size = size;
    }

    /// <summary>以字节数组创建位向量，位数 = 字节数 × 8。</summary>
    public BitVector(byte[] bytes) : this(bytes, bytes.Length * 8)
    {
    }

    /// <summary>以字节数组与指定位数创建位向量。</summary>
    public BitVector(byte[] bytes, int size)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "位向量的位数必须大于 0。");
        }

        if (size > bytes.Length * 8)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "位数超出字节数组的容量。");
        }

        _bytes = new byte[(size + 7) / 8];
        Array.Copy(bytes, _bytes, _bytes.Length);
        _size = size;
        MaskTail();
    }

    /// <summary>位向量包含的位数。</summary>
    public int Size => _size;

    /// <summary>承载这些位所需的字节数。</summary>
    public int ByteCount => (_size + 7) / 8;

    /// <summary>按位读写。</summary>
    public bool this[int index]
    {
        get => GetBit(index);
        set => SetBit(index, value);
    }

    /// <summary>读取指定位。</summary>
    public bool GetBit(int index)
    {
        ValidateIndex(index);
        return (_bytes[index >> 3] & (1 << (index & 7))) != 0;
    }

    /// <summary>置位指定位。</summary>
    public void SetBit(int index)
    {
        ValidateIndex(index);
        _bytes[index >> 3] |= (byte)(1 << (index & 7));
    }

    /// <summary>按值写入指定位。</summary>
    public void SetBit(int index, bool value)
    {
        if (value)
        {
            SetBit(index);
        }
        else
        {
            ClearBit(index);
        }
    }

    /// <summary>清零指定位。</summary>
    public void ClearBit(int index)
    {
        ValidateIndex(index);
        _bytes[index >> 3] &= (byte)~(1 << (index & 7));
    }

    /// <summary>全部置 1。</summary>
    public void SetAll()
    {
        for (int i = 0; i < _size; i++)
        {
            SetBit(i);
        }
    }

    /// <summary>全部清零。</summary>
    public void ClearAll() => Array.Clear(_bytes);

    /// <summary>重新指定位数（保留已有内容，新增位为 0）。</summary>
    public void ForceSize(int size)
    {
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "位向量的位数必须大于 0。");
        }

        int byteCount = (size + 7) / 8;
        if (_bytes.Length != byteCount)
        {
            var resized = new byte[byteCount];
            Array.Copy(_bytes, resized, Math.Min(_bytes.Length, byteCount));
            _bytes = resized;
        }

        _size = size;
        MaskTail();
    }

    /// <summary>获取底层字节数组的副本（可能大于 <see cref="ByteCount"/>）。</summary>
    public byte[] GetBytes() => (byte[])_bytes.Clone();

    /// <summary>获取前 <paramref name="count"/> 个字节的副本。</summary>
    public byte[] GetBytes(int count)
    {
        if (count < 0 || count > _bytes.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "字节数超出位向量的容量。");
        }

        var result = new byte[count];
        Array.Copy(_bytes, result, count);
        return result;
    }

    /// <summary>恰好 <see cref="ByteCount"/> 个字节，用于组帧。</summary>
    public byte[] ToByteArray() => GetBytes(ByteCount);

    /// <summary>用字节数组整体覆盖内容，位数变为 字节数 × 8。</summary>
    public void SetBytes(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        _bytes = new byte[bytes.Length];
        Array.Copy(bytes, _bytes, bytes.Length);
        _size = bytes.Length * 8;
    }

    /// <summary>与另一个位向量按位与非（原地）。</summary>
    public void And(BitVector other)
    {
        CheckSameSize(other);
        for (int i = 0; i < _bytes.Length; i++)
        {
            _bytes[i] &= other._bytes[i];
        }

        MaskTail();
    }

    /// <summary>与另一个位向量按位或（原地）。</summary>
    public void Or(BitVector other)
    {
        CheckSameSize(other);
        for (int i = 0; i < _bytes.Length; i++)
        {
            _bytes[i] |= other._bytes[i];
        }

        MaskTail();
    }

    /// <summary>与另一个位向量按位异或（原地）。</summary>
    public void Xor(BitVector other)
    {
        CheckSameSize(other);
        for (int i = 0; i < _bytes.Length; i++)
        {
            _bytes[i] ^= other._bytes[i];
        }

        MaskTail();
    }

    /// <summary>按位取反（原地）。</summary>
    public void Not()
    {
        for (int i = 0; i < _bytes.Length; i++)
        {
            _bytes[i] = (byte)~_bytes[i];
        }

        MaskTail();
    }

    /// <summary>按 Modbus 线序解析一段位数据。</summary>
    public static BitVector FromBytes(byte[] bytes, int size) => new(bytes, size);

    /// <summary>以 <c>1 0 1 1</c> 形式的字符串创建位向量（空格可选）。</summary>
    public static BitVector Parse(string bits)
    {
        ArgumentNullException.ThrowIfNull(bits);
        var trimmed = bits.Replace(" ", string.Empty).Replace("\t", string.Empty);
        var vector = new BitVector(trimmed.Length);
        for (int i = 0; i < trimmed.Length; i++)
        {
            vector.SetBit(i, trimmed[i] switch
            {
                '1' => true,
                '0' => false,
                _ => throw new FormatException($"无法解析位字符串：'{bits}'。"),
            });
        }

        return vector;
    }

    /// <inheritdoc />
    public bool Equals(BitVector? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (_size != other._size)
        {
            return false;
        }

        return ToByteArray().AsSpan().SequenceEqual(other.ToByteArray());
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is BitVector other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_size);
        foreach (var b in ToByteArray())
        {
            hash.Add(b);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var builder = new StringBuilder(_size + (_size / 4));
        for (int i = 0; i < _size; i++)
        {
            if (i > 0 && i % 8 == 0)
            {
                builder.Append(' ');
            }

            builder.Append(GetBit(i) ? '1' : '0');
        }

        return builder.ToString();
    }

    private void ValidateIndex(int index)
    {
        if (index < 0 || index >= _size)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, $"索引必须落在 [0, {_size}) 区间内。");
        }
    }

    private void CheckSameSize(BitVector other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other._size != _size)
        {
            throw new ArgumentException("两个位向量的位数不一致。", nameof(other));
        }
    }

    /// <summary>清除末字节中超出 <see cref="Size"/> 的填充位，保证相等性比较稳定。</summary>
    private void MaskTail()
    {
        int tail = _size & 7;
        if (tail != 0)
        {
            _bytes[^1] &= (byte)((1 << tail) - 1);
        }
    }
}
