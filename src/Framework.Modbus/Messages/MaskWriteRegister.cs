using Abstractions = Framework.Modbus.Abstractions;

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
        using var writer = new ModbusPduWriter();
        PduBridge.Write(writer, new Abstractions.MaskWriteRegisterRequest((ushort)Reference, (ushort)AndMask, (ushort)OrMask));
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析请求。</summary>
    public static MaskWriteRegisterRequest FromPdu(byte[] pdu)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        var decoded = PduBridge.DecodeAs(
            () => Abstractions.MaskWriteRegisterRequest.Decode(pdu.AsSpan(1)),
            "屏蔽写寄存器请求");
        return new MaskWriteRegisterRequest(decoded.Address, decoded.AndMask, decoded.OrMask);
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
        using var writer = new ModbusPduWriter();
        PduBridge.Write(writer, new Abstractions.MaskWriteRegisterResponse((ushort)Reference, (ushort)AndMask, (ushort)OrMask));
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析响应。</summary>
    public static MaskWriteRegisterResponse FromPdu(byte[] pdu)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        var decoded = PduBridge.DecodeAs(
            () => Abstractions.MaskWriteRegisterResponse.Decode(ModbusFunctionCode.MaskWriteRegister, pdu),
            "屏蔽写寄存器响应");
        return new MaskWriteRegisterResponse(decoded.Address, decoded.AndMask, decoded.OrMask);
    }
}
