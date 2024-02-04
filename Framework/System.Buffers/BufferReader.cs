using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Buffers;

/// <summary>
/// 表示Buffter读取器
/// </summary>
[DebuggerDisplay("Length = {Length}")]
public ref struct BufferReader
{
	/// <summary>
	/// 未读取的数据跨度
	/// </summary>
	private ReadOnlySpan<byte> span;

	/// <summary>
	/// 获取未读取的数据跨度
	/// </summary>
	public readonly ReadOnlySpan<byte> UnreadSpan => span;

	/// <summary>
	/// Buffter读取器
	/// </summary>
	/// <param name="span"></param>
	public BufferReader(ReadOnlySpan<byte> span)
	{
		this.span = span;
	}

	/// <summary>
	/// Buffter读取器
	/// </summary>
	/// <param name="arraySegment"></param>
	public BufferReader(ArraySegment<byte> arraySegment)
	{
		span = arraySegment.AsSpan();
	}

	/// <summary>
	/// 读取指定长度
	/// </summary>
	/// <param name="count"></param>
	/// <returns></returns>
	public ReadOnlySpan<byte> Read(int count)
	{
		var value = span[..count];
		span = span[count..];
		return value;
	}

	/// <summary>
	/// 读取指定长度并编码为文本
	/// </summary>
	/// <param name="byteCount">字节长度</param>
	/// <param name="encoding">编码</param>
	/// <returns></returns>
	public unsafe string Read(int byteCount, Encoding encoding)
	{
		var text = Read(byteCount);
		var bytes = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(text));
		return encoding.GetString(bytes, byteCount);
	}

	/// <summary>
	/// 读取int32
	/// </summary>
	/// <param name="value"></param>
	public void ReadBigEndian(out int value)
	{
		value = BinaryPrimitives.ReadInt32BigEndian(span);
		span = span[sizeof(int)..];
	}

	/// <summary>
	/// 读取int32
	/// </summary>
	/// <param name="value"></param>
	public void ReadLittleEndian(out int value)
	{
		value = BinaryPrimitives.ReadInt32LittleEndian(span);
		span = span[sizeof(int)..];
	}

	/// <summary>
	/// 读取int16
	/// </summary>
	/// <param name="value"></param>
	public void ReadBigEndian(out short value)
	{
		value = BinaryPrimitives.ReadInt16BigEndian(span);
		span = span[sizeof(short)..];
	}

	/// <summary>
	/// 读取int16
	/// </summary>
	/// <param name="value"></param>
	public void ReadLittleEndian(out short value)
	{
		value = BinaryPrimitives.ReadInt16LittleEndian(span);
		span = span[sizeof(short)..];
	}

	/// <summary>
	/// 读取int64
	/// </summary>
	/// <param name="value"></param>
	public void ReadBigEndian(out long value)
	{
		value = BinaryPrimitives.ReadInt64BigEndian(span);
		span = span[sizeof(long)..];
	}

	/// <summary>
	/// 读取int64
	/// </summary>
	/// <param name="value"></param>
	public void ReadLittleEndian(out long value)
	{
		value = BinaryPrimitives.ReadInt64LittleEndian(span);
		span = span[sizeof(long)..];
	}


	/// <summary>
	/// 读取uint32
	/// </summary>
	/// <param name="value"></param>
	public void ReadBigEndian(out uint value)
	{
		value = BinaryPrimitives.ReadUInt32BigEndian(span);
		span = span[sizeof(uint)..];
	}

	/// <summary>
	/// 读取uint32
	/// </summary>
	/// <param name="value"></param>
	public void ReadLittleEndian(out uint value)
	{
		value = BinaryPrimitives.ReadUInt32LittleEndian(span);
		span = span[sizeof(uint)..];
	}


	/// <summary>
	/// 读取uint16
	/// </summary>
	/// <param name="value"></param>
	public void ReadBigEndian(out ushort value)
	{
		value = BinaryPrimitives.ReadUInt16BigEndian(span);
		span = span[sizeof(ushort)..];
	}


	/// <summary>
	/// 读取uint16
	/// </summary>
	/// <param name="value"></param>
	public void ReadLittleEndian(out ushort value)
	{
		value = BinaryPrimitives.ReadUInt16LittleEndian(span);
		span = span[sizeof(ushort)..];
	}

	/// <summary>
	/// 读取uint64
	/// </summary>
	/// <param name="value"></param>
	public void ReadBigEndian(out ulong value)
	{
		value = BinaryPrimitives.ReadUInt64BigEndian(span);
		span = span[sizeof(ulong)..];
	}

	/// <summary>
	/// 读取uint64
	/// </summary>
	/// <param name="value"></param>
	public void ReadLittleEndian(out ulong value)
	{
		value = BinaryPrimitives.ReadUInt64LittleEndian(span);
		span = span[sizeof(ulong)..];
	}

	/// <summary>
	/// 读取double
	/// </summary>
	/// <param name="value"></param>
	public void ReadLittleEndian(out double value)
	{
		value = BinaryPrimitives.ReadDoubleLittleEndian(span);
		span = span[sizeof(long)..];
	}

	/// <summary>
	/// 读取double
	/// </summary>
	/// <param name="value"></param>
	public void ReadBigEndian(out double value)
	{
		value = BinaryPrimitives.ReadDoubleBigEndian(span);
		span = span[sizeof(long)..];
	}

	/// <summary>
	/// 读取float
	/// </summary>
	/// <param name="value"></param>
	public void ReadLittleEndian(out float value)
	{
		value = BinaryPrimitives.ReadSingleLittleEndian(span);
		span = span[sizeof(int)..];
	}


	/// <summary>
	/// 读取float
	/// </summary>
	/// <param name="value"></param>
	public void ReadBigEndian(out float value)
	{
		value = BinaryPrimitives.ReadSingleBigEndian(span);
		span = span[sizeof(int)..];
	}
}
