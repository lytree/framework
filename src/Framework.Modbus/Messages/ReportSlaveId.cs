using Abstractions = Framework.Modbus.Abstractions;

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
    public override byte[] ToPdu()
    {
        using var writer = new ModbusPduWriter();
        PduBridge.Write(writer, new Abstractions.ReportSlaveIdRequest());
        return writer.ToArray();
    }
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
        using var writer = new ModbusPduWriter();
        PduBridge.Write(writer, new Abstractions.ReportSlaveIdResponse(SlaveData));
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析响应。</summary>
    public static ReportSlaveIdResponse FromPdu(byte[] pdu)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        var decoded = PduBridge.DecodeAs(
            () => Abstractions.ReportSlaveIdResponse.Decode(ModbusFunctionCode.ReportSlaveId, pdu),
            "报告从站标识响应");
        return new ReportSlaveIdResponse(decoded.Data.ToArray());
    }
}
