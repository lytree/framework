using Framework.Modbus.Util;

namespace Framework.Modbus.Messages;

/// <summary>FC11 报告从站标识请求。对应 jamod 的 <c>ReportSlaveIDRequest</c>。</summary>
public class ReportSlaveIdRequest : ModbusRequest
{
    /// <summary>构造请求（无参数）。</summary>
    public ReportSlaveIdRequest()
    {
    }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.ReportSlaveId;

    /// <inheritdoc />
    public override int ExpectedResponsePduLength => -1;

    /// <inheritdoc />
    public override byte[] ToPdu() => new[] { FunctionCode };
}

/// <summary>FC11 报告从站标识响应。</summary>
public sealed class ReportSlaveIdResponse : ModbusResponse
{
    /// <summary>以从站数据构造响应。</summary>
    public ReportSlaveIdResponse(byte[] slaveData)
    {
        SlaveData = slaveData ?? throw new ArgumentNullException(nameof(slaveData));
    }

    /// <summary>从站标识数据。首字节通常为从站类型，其余为实现相关。</summary>
    public byte[] SlaveData { get; }

    /// <summary>从站是否处于运行状态（首字节 0xFF = 运行，0x00 = 停止）。</summary>
    public bool? IsRunning => SlaveData.Length == 0
        ? null
        : SlaveData[0] switch
        {
            0xFF => true,
            0x00 => false,
            _ => null,
        };

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.ReportSlaveId;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteByte(SlaveData.Length);
        writer.WriteBytes(SlaveData);
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析响应。</summary>
    public static ReportSlaveIdResponse FromPdu(byte[] pdu)
    {
        if (pdu.Length < 2)
        {
            throw new ModbusIOException("报告从站标识响应 PDU 长度不足。");
        }

        int byteCount = pdu[1];
        if (pdu.Length < 2 + byteCount)
        {
            throw new ModbusIOException($"报告从站标识响应声明 {byteCount} 字节，但实际只有 {pdu.Length - 2} 字节。");
        }

        return new ReportSlaveIdResponse(pdu.AsSpan(2, byteCount).ToArray());
    }
}
