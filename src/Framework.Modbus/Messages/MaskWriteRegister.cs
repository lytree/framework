namespace Framework.Modbus.Messages;

/// <summary>FC16 屏蔽写寄存器请求。对应 jamod 的 <c>MaskWriteRegisterRequest</c>。</summary>
/// <remarks>
/// 寄存器的新值按 <c>(当前值 AND AndMask) OR (OrMask AND (NOT AndMask))</c> 计算。
/// </remarks>
public class MaskWriteRegisterRequest : ModbusRequest
{
    /// <summary>构造请求。</summary>
    public MaskWriteRegisterRequest(int reference, int andMask, int orMask)
    {
        if (reference is < 0 or > 0xFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(reference), reference, "寄存器地址必须落在 [0, 65535]。");
        }

        if (andMask is < 0 or > 0xFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(andMask), andMask, "AND 掩码必须落在 [0, 65535]。");
        }

        if (orMask is < 0 or > 0xFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(orMask), orMask, "OR 掩码必须落在 [0, 65535]。");
        }

        Reference = reference;
        AndMask = andMask;
        OrMask = orMask;
    }

    /// <summary>寄存器地址。</summary>
    public int Reference { get; }

    /// <summary>AND 掩码。</summary>
    public int AndMask { get; }

    /// <summary>OR 掩码。</summary>
    public int OrMask { get; }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.MaskWriteRegister;

    /// <inheritdoc />
    public override int ExpectedResponsePduLength => 7;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteUInt16(Reference);
        writer.WriteUInt16(AndMask);
        writer.WriteUInt16(OrMask);
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析请求。</summary>
    public static MaskWriteRegisterRequest FromPdu(byte[] pdu)
    {
        var reader = new ModbusPduReader(pdu, 1);
        int reference = reader.ReadUInt16();
        int andMask = reader.ReadUInt16();
        int orMask = reader.ReadUInt16();
        return new MaskWriteRegisterRequest(reference, andMask, orMask);
    }
}

/// <summary>FC16 屏蔽写寄存器响应（回显请求）。</summary>
public sealed class MaskWriteRegisterResponse : ModbusResponse
{
    /// <summary>构造响应。</summary>
    public MaskWriteRegisterResponse(int reference, int andMask, int orMask)
    {
        Reference = reference;
        AndMask = andMask;
        OrMask = orMask;
    }

    /// <summary>寄存器地址。</summary>
    public int Reference { get; }

    /// <summary>AND 掩码。</summary>
    public int AndMask { get; }

    /// <summary>OR 掩码。</summary>
    public int OrMask { get; }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.MaskWriteRegister;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteUInt16(Reference);
        writer.WriteUInt16(AndMask);
        writer.WriteUInt16(OrMask);
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析响应。</summary>
    public static MaskWriteRegisterResponse FromPdu(byte[] pdu)
    {
        if (pdu.Length < 7)
        {
            throw new ModbusIOException("屏蔽写寄存器响应 PDU 长度不足（需要 7 字节）。");
        }

        var reader = new ModbusPduReader(pdu, 1);
        int reference = reader.ReadUInt16();
        int andMask = reader.ReadUInt16();
        int orMask = reader.ReadUInt16();
        return new MaskWriteRegisterResponse(reference, andMask, orMask);
    }
}
