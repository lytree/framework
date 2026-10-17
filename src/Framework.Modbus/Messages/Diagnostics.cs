namespace Framework.Modbus.Messages;

/// <summary>FC07 读异常状态请求。对应 jamod 的 <c>ReadExceptionStatusRequest</c>。</summary>
public class ReadExceptionStatusRequest : ModbusRequest
{
    /// <summary>构造请求（无参数）。</summary>
    public ReadExceptionStatusRequest()
    {
    }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.ReadExceptionStatus;

    /// <inheritdoc />
    public override int ExpectedResponsePduLength => 2;

    /// <inheritdoc />
    public override byte[] ToPdu() => new[] { FunctionCode };
}

/// <summary>FC07 读异常状态响应。</summary>
public sealed class ReadExceptionStatusResponse : ModbusResponse
{
    /// <summary>以状态字节构造响应。</summary>
    public ReadExceptionStatusResponse(int exceptionStatus)
    {
        if (exceptionStatus is < 0 or > 0xFF)
        {
            throw new ArgumentOutOfRangeException(nameof(exceptionStatus), exceptionStatus, "异常状态必须落在 [0, 255]。");
        }

        ExceptionStatus = (byte)exceptionStatus;
    }

    /// <summary>从站返回的异常状态字节。</summary>
    public byte ExceptionStatus { get; }

    /// <summary>按位读取异常状态。</summary>
    public bool this[int bitIndex] => bitIndex is >= 0 and < 8 && (ExceptionStatus & (1 << bitIndex)) != 0;

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.ReadExceptionStatus;

    /// <inheritdoc />
    public override byte[] ToPdu() => new[] { FunctionCode, ExceptionStatus };

    /// <summary>从 PDU 解析响应。</summary>
    public static ReadExceptionStatusResponse FromPdu(byte[] pdu)
    {
        if (pdu.Length < 2)
        {
            throw new ModbusIOException("读异常状态响应 PDU 长度不足（需要 2 字节）。");
        }

        return new ReadExceptionStatusResponse(pdu[1]);
    }
}

/// <summary>FC08 诊断请求。对应 jamod 的 <c>DiagnosticsRequest</c>。</summary>
public class DiagnosticsRequest : ModbusRequest
{
    /// <summary>构造请求。</summary>
    /// <param name="subFunction">子功能码，见 <see cref="ModbusDiagnosticsSubFunction"/>。</param>
    /// <param name="data">数据字段（多数子功能下为返回值 / 计数）。</param>
    public DiagnosticsRequest(int subFunction, int data)
    {
        if (subFunction is < 0 or > 0xFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(subFunction), subFunction, "子功能码必须落在 [0, 65535]。");
        }

        if (data is < 0 or > 0xFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(data), data, "数据字段必须落在 [0, 65535]。");
        }

        SubFunction = subFunction;
        Data = data;
    }

    /// <summary>构造请求（数据字段为 0）。</summary>
    public DiagnosticsRequest(int subFunction) : this(subFunction, 0)
    {
    }

    /// <summary>子功能码。</summary>
    public int SubFunction { get; }

    /// <summary>数据字段。</summary>
    public int Data { get; }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.Diagnostics;

    /// <inheritdoc />
    public override int ExpectedResponsePduLength => 5;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteUInt16(SubFunction);
        writer.WriteUInt16(Data);
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析请求。</summary>
    public static DiagnosticsRequest FromPdu(byte[] pdu)
    {
        var reader = new ModbusPduReader(pdu, 1);
        return new DiagnosticsRequest(reader.ReadUInt16(), reader.ReadUInt16());
    }
}

/// <summary>FC08 诊断响应（回显请求）。</summary>
public sealed class DiagnosticsResponse : ModbusResponse
{
    /// <summary>以子功能码与数据构造响应。</summary>
    public DiagnosticsResponse(int subFunction, int data)
    {
        SubFunction = subFunction;
        Data = data;
    }

    /// <summary>子功能码。</summary>
    public int SubFunction { get; }

    /// <summary>数据字段。</summary>
    public int Data { get; }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.Diagnostics;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteUInt16(SubFunction);
        writer.WriteUInt16(Data);
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析响应。</summary>
    public static DiagnosticsResponse FromPdu(byte[] pdu)
    {
        if (pdu.Length < 5)
        {
            throw new ModbusIOException("诊断响应 PDU 长度不足（需要 5 字节）。");
        }

        var reader = new ModbusPduReader(pdu, 1);
        return new DiagnosticsResponse(reader.ReadUInt16(), reader.ReadUInt16());
    }
}
