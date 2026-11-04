using System.Buffers;
using Abstractions = Framework.Modbus.Abstractions;

namespace Framework.Modbus.Messages;

/// <summary>
/// 主站报文与协议契约层（<c>Framework.Modbus.Abstractions</c>）PDU 之间的桥接工具。
/// </summary>
internal static class PduBridge
{
    /// <summary>把契约层 PDU 直接编码进目标缓冲，避免"先编码成数组再拷贝"的中间分配。</summary>
    public static void Write(IBufferWriter<byte> writer, Abstractions.ModbusPdu pdu)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(pdu);

        var span = writer.GetSpan(pdu.Length);
        pdu.Encode(span);
        writer.Advance(pdu.Length);
    }

    /// <summary>
    /// 执行契约层解码，并把 <see cref="Abstractions.ModbusSlaveException"/> 转成主站对外的
    /// <see cref="ModbusIOException"/>（保留原有异常类型）。
    /// </summary>
    public static T DecodeAs<T>(Func<T> decode, string context)
    {
        ArgumentNullException.ThrowIfNull(decode);
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return decode();
        }
        catch (Abstractions.ModbusSlaveException ex)
        {
            throw new ModbusIOException($"{context}: {ex.Message}", ex);
        }
    }
}
