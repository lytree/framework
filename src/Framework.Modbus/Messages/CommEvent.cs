using Abstractions = Framework.Modbus.Abstractions;

namespace Framework.Modbus.Messages;

/// <summary>FC0B 取通信事件计数器请求。对应 jamod 的 <c>GetCommEventCounterRequest</c>。</summary>
public class GetCommEventCounterRequest : ModbusRequest
{
    /// <summary>构造请求（无参数）。</summary>
    public GetCommEventCounterRequest()
    {
    }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.GetCommEventCounter;

    /// <inheritdoc />
    public override int ExpectedResponsePduLength => 5;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        using var writer = new ModbusPduWriter();
        PduBridge.Write(writer, new Abstractions.GetCommEventCounterRequest());
        return writer.ToArray();
    }
}

/// <summary>FC0B 取通信事件计数器响应。</summary>
public sealed class GetCommEventCounterResponse : ModbusResponse
{
    /// <summary>以状态字与事件计数构造响应。</summary>
    public GetCommEventCounterResponse(int status, int eventCount)
    {
        Status = status;
        EventCount = eventCount;
    }

    /// <summary>状态字：0xFFFF = 忙，0x0000 = 就绪。</summary>
    public int Status { get; }

    /// <summary>事件计数。</summary>
    public int EventCount { get; }

    /// <summary>从站是否忙。</summary>
    public bool IsBusy => Status == 0xFFFF;

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.GetCommEventCounter;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        using var writer = new ModbusPduWriter();
        PduBridge.Write(writer, new Abstractions.GetCommEventCounterResponse((ushort)Status, (ushort)EventCount));
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析响应。</summary>
    public static GetCommEventCounterResponse FromPdu(byte[] pdu)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        var decoded = PduBridge.DecodeAs(
            () => Abstractions.GetCommEventCounterResponse.Decode(ModbusFunctionCode.GetCommEventCounter, pdu),
            "取通信事件计数器响应");
        return new GetCommEventCounterResponse(decoded.Status, decoded.EventCount);
    }
}

/// <summary>FC0C 取通信事件记录请求。对应 jamod 的 <c>GetCommEventLogRequest</c>。</summary>
public class GetCommEventLogRequest : ModbusRequest
{
    /// <summary>构造请求（无参数）。</summary>
    public GetCommEventLogRequest()
    {
    }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.GetCommEventLog;

    /// <inheritdoc />
    public override int ExpectedResponsePduLength => -1;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        using var writer = new ModbusPduWriter();
        PduBridge.Write(writer, new Abstractions.GetCommEventLogRequest());
        return writer.ToArray();
    }
}

/// <summary>FC0C 取通信事件记录响应。</summary>
public sealed class GetCommEventLogResponse : ModbusResponse
{
    /// <summary>以各字段构造响应。</summary>
    public GetCommEventLogResponse(int status, int eventCount, int messageCount, byte[] events)
    {
        Status = status;
        EventCount = eventCount;
        MessageCount = messageCount;
        Events = events ?? Array.Empty<byte>();
    }

    /// <summary>状态字。</summary>
    public int Status { get; }

    /// <summary>事件计数。</summary>
    public int EventCount { get; }

    /// <summary>消息计数。</summary>
    public int MessageCount { get; }

    /// <summary>事件字节（每字节一个事件，0 = 正常，1 = 异常）。</summary>
    public byte[] Events { get; }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.GetCommEventLog;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        using var writer = new ModbusPduWriter();
        PduBridge.Write(
            writer,
            new Abstractions.GetCommEventLogResponse(
                (ushort)Status,
                (ushort)EventCount,
                (ushort)MessageCount,
                Events));
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析响应。</summary>
    public static GetCommEventLogResponse FromPdu(byte[] pdu)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        var decoded = PduBridge.DecodeAs(
            () => Abstractions.GetCommEventLogResponse.Decode(ModbusFunctionCode.GetCommEventLog, pdu),
            "取通信事件记录响应");
        return new GetCommEventLogResponse(decoded.Status, decoded.EventCount, decoded.MessageCount, decoded.Events.ToArray());
    }
}
