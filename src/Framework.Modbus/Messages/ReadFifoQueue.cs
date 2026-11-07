using Abstractions = Framework.Modbus.Abstractions;

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
        using var writer = new ModbusPduWriter();
        PduBridge.Write(writer, new Abstractions.ReadFifoQueueRequest((ushort)Reference));
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析请求。</summary>
    public static ReadFifoQueueRequest FromPdu(byte[] pdu)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        var decoded = PduBridge.DecodeAs(
            () => Abstractions.ReadFifoQueueRequest.Decode(pdu.AsSpan(1)),
            "读 FIFO 队列请求");
        return new ReadFifoQueueRequest(decoded.Address);
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
        var values = new ushort[Values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = (ushort)Values[i];
        }

        using var writer = new ModbusPduWriter();
        PduBridge.Write(writer, new Abstractions.ReadFifoQueueResponse(values));
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析响应。</summary>
    public static ReadFifoQueueResponse FromPdu(byte[] pdu)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        var decoded = PduBridge.DecodeAs(
            () => Abstractions.ReadFifoQueueResponse.Decode(ModbusFunctionCode.ReadFifoQueue, pdu),
            "读 FIFO 队列响应");

        var values = new int[decoded.Values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = decoded.Values[i];
        }

        return new ReadFifoQueueResponse(values);
    }
}
