namespace Framework.Modbus.Messages;

/// <summary>FC18 读 FIFO 队列请求。对应 jamod 的 <c>ReadFIFOQueueRequest</c>。</summary>
public class ReadFifoQueueRequest : ModbusRequest
{
    /// <summary>以 FIFO 指针地址构造请求。</summary>
    public ReadFifoQueueRequest(int reference)
    {
        if (reference is < 0 or > 0xFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(reference), reference, "FIFO 地址必须落在 [0, 65535]。");
        }

        Reference = reference;
    }

    /// <summary>FIFO 指针地址。</summary>
    public int Reference { get; }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.ReadFifoQueue;

    /// <inheritdoc />
    public override int ExpectedResponsePduLength => -1;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteUInt16(Reference);
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析请求。</summary>
    public static ReadFifoQueueRequest FromPdu(byte[] pdu)
    {
        var reader = new ModbusPduReader(pdu, 1);
        return new ReadFifoQueueRequest(reader.ReadUInt16());
    }
}

/// <summary>FC18 读 FIFO 队列响应。</summary>
public sealed class ReadFifoQueueResponse : ModbusResponse
{
    /// <summary>以 FIFO 值构造响应。</summary>
    public ReadFifoQueueResponse(IEnumerable<int> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Values = values.ToArray();
    }

    /// <summary>FIFO 中的寄存器值，按先进先出顺序排列。</summary>
    public int[] Values { get; }

    /// <summary>FIFO 中的寄存器数量。</summary>
    public int FifoCount => Values.Length;

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.ReadFifoQueue;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteUInt16((Values.Length * 2) + 2);
        writer.WriteUInt16(Values.Length);
        foreach (var value in Values)
        {
            writer.WriteUInt16(value);
        }

        return writer.ToArray();
    }

    /// <summary>从 PDU 解析响应。</summary>
    public static ReadFifoQueueResponse FromPdu(byte[] pdu)
    {
        if (pdu.Length < 5)
        {
            throw new ModbusIOException("读 FIFO 队列响应 PDU 长度不足（至少需要 5 字节）。");
        }

        int byteCount = ((pdu[1] & 0xFF) << 8) | (pdu[2] & 0xFF);
        if (pdu.Length < 3 + byteCount)
        {
            throw new ModbusIOException($"读 FIFO 队列响应声明 {byteCount} 字节，但实际只有 {pdu.Length - 3} 字节。");
        }

        var reader = new ModbusPduReader(pdu, 3, byteCount);
        int fifoCount = reader.ReadUInt16();
        var values = new int[fifoCount];
        for (int i = 0; i < fifoCount; i++)
        {
            values[i] = reader.ReadUInt16();
        }

        return new ReadFifoQueueResponse(values);
    }
}
