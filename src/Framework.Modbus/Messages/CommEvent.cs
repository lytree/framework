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
    public override byte[] ToPdu() => new[] { FunctionCode };
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
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteUInt16(Status);
        writer.WriteUInt16(EventCount);
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析响应。</summary>
    public static GetCommEventCounterResponse FromPdu(byte[] pdu)
    {
        if (pdu.Length < 5)
        {
            throw new ModbusIOException("取通信事件计数器响应 PDU 长度不足（需要 5 字节）。");
        }

        var reader = new ModbusPduReader(pdu, 1);
        return new GetCommEventCounterResponse(reader.ReadUInt16(), reader.ReadUInt16());
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
    public override byte[] ToPdu() => new[] { FunctionCode };
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
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteByte(Events.Length + 6);
        writer.WriteUInt16(Status);
        writer.WriteUInt16(EventCount);
        writer.WriteUInt16(MessageCount);
        writer.WriteBytes(Events);
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析响应。</summary>
    public static GetCommEventLogResponse FromPdu(byte[] pdu)
    {
        if (pdu.Length < 8)
        {
            throw new ModbusIOException("取通信事件记录响应 PDU 长度不足（至少需要 8 字节）。");
        }

        int byteCount = pdu[1];
        if (pdu.Length < 2 + byteCount)
        {
            throw new ModbusIOException($"取通信事件记录响应声明 {byteCount} 字节，但实际只有 {pdu.Length - 2} 字节。");
        }

        var reader = new ModbusPduReader(pdu, 2, byteCount);
        int status = reader.ReadUInt16();
        int eventCount = reader.ReadUInt16();
        int messageCount = reader.ReadUInt16();
        var events = reader.Remaining > 0 ? reader.ReadBytes(reader.Remaining) : Array.Empty<byte>();
        return new GetCommEventLogResponse(status, eventCount, messageCount, events);
    }
}
