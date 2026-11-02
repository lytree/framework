using System.Buffers;
using System.Buffers.Binary;

namespace Framework.Modbus.Util;

/// <summary>
/// <see cref="IBufferWriter{T}"/> 上的 Modbus 大端（网络序）写入扩展。
/// </summary>
/// <remarks>
/// PDU 编码统一走这里，使同一份编码逻辑既能写进复用的帧缓冲（<see cref="PooledByteBuf"/>），
/// 也能写进 <see cref="ModbusPduWriter"/>，从而省掉"先编码成数组再拷贝"的中间分配。
/// </remarks>
internal static class ModbusBufferWriterExtensions
{
    /// <summary>写入 1 字节（取低 8 位）。</summary>
    public static void WriteByte(this IBufferWriter<byte> writer, int value)
    {
        var span = writer.GetSpan(1);
        span[0] = (byte)value;
        writer.Advance(1);
    }

    /// <summary>写入 16 位大端整数（取低 16 位）。</summary>
    public static void WriteUInt16(this IBufferWriter<byte> writer, int value)
    {
        var span = writer.GetSpan(2);
        BinaryPrimitives.WriteUInt16BigEndian(span, (ushort)value);
        writer.Advance(2);
    }

    /// <summary>写入 32 位大端整数。</summary>
    public static void WriteUInt32(this IBufferWriter<byte> writer, int value)
    {
        var span = writer.GetSpan(4);
        BinaryPrimitives.WriteUInt32BigEndian(span, (uint)value);
        writer.Advance(4);
    }

    /// <summary>写入字节序列。</summary>
    public static void WriteBytes(this IBufferWriter<byte> writer, ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return;
        }

        data.CopyTo(writer.GetSpan(data.Length));
        writer.Advance(data.Length);
    }
}
