using Framework.Modbus.Util;

namespace Framework.Modbus.Messages;

/// <summary>FC05 写单个线圈请求。对应 jamod 的 <c>WriteCoilRequest</c>。</summary>
public class WriteCoilRequest : ModbusRequest
{
    /// <summary>构造请求。</summary>
    /// <param name="reference">线圈地址。</param>
    /// <param name="coilState">写入的状态：true = ON（0xFF00），false = OFF（0x0000）。</param>
    public WriteCoilRequest(int reference, bool coilState)
    {
        if (reference is < 0 or > 0xFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(reference), reference, "线圈地址必须落在 [0, 65535]。");
        }

        Reference = reference;
        CoilState = coilState;
    }

    /// <summary>线圈地址。</summary>
    public int Reference { get; }

    /// <summary>写入的状态。</summary>
    public bool CoilState { get; }

    /// <summary>线圈的线序取值：ON = 0xFF00，OFF = 0x0000。</summary>
    public int CoilValue => CoilState ? 0xFF00 : 0x0000;

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.WriteCoil;

    /// <inheritdoc />
    public override int ExpectedResponsePduLength => 5;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteUInt16(Reference);
        writer.WriteUInt16(CoilValue);
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析请求。</summary>
    public static WriteCoilRequest FromPdu(byte[] pdu)
    {
        var reader = new ModbusPduReader(pdu, 1);
        int reference = reader.ReadUInt16();
        return new WriteCoilRequest(reference, reader.ReadUInt16() == 0xFF00);
    }
}

/// <summary>FC05 写单个线圈响应（回显请求）。</summary>
public sealed class WriteCoilResponse : ModbusResponse
{
    /// <summary>以地址与状态构造响应。</summary>
    public WriteCoilResponse(int reference, bool coilState)
    {
        Reference = reference;
        CoilState = coilState;
    }

    /// <summary>线圈地址。</summary>
    public int Reference { get; }

    /// <summary>写入的状态。</summary>
    public bool CoilState { get; }

    /// <summary>线圈的线序取值。</summary>
    public int CoilValue => CoilState ? 0xFF00 : 0x0000;

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.WriteCoil;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteUInt16(Reference);
        writer.WriteUInt16(CoilValue);
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析响应。</summary>
    public static WriteCoilResponse FromPdu(byte[] pdu)
    {
        if (pdu.Length < 5)
        {
            throw new ModbusIOException("写线圈响应 PDU 长度不足（需要 5 字节）。");
        }

        var reader = new ModbusPduReader(pdu, 1);
        int reference = reader.ReadUInt16();
        return new WriteCoilResponse(reference, reader.ReadUInt16() == 0xFF00);
    }
}
