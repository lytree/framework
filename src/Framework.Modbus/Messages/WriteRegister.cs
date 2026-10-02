using Framework.Modbus.ProcessImage;
using Framework.Modbus.Util;

namespace Framework.Modbus.Messages;

/// <summary>FC06 写单个寄存器请求。对应 jamod 的 <c>WriteRegisterRequest</c>。</summary>
public class WriteRegisterRequest : ModbusRequest
{
    /// <summary>以寄存器对象构造请求。</summary>
    public WriteRegisterRequest(int reference, Register register)
    {
        ArgumentNullException.ThrowIfNull(register);
        if (reference is < 0 or > 0xFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(reference), reference, "寄存器地址必须落在 [0, 65535]。");
        }

        Reference = reference;
        Register = register;
    }

    /// <summary>以 16 位整数构造请求。</summary>
    public WriteRegisterRequest(int reference, int value) : this(reference, new SimpleRegister(value))
    {
    }

    /// <summary>寄存器地址。</summary>
    public int Reference { get; }

    /// <summary>待写入的寄存器。</summary>
    public Register Register { get; }

    /// <summary>待写入的值。</summary>
    public int Value => Register.Value;

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.WriteSingleRegister;

    /// <inheritdoc />
    public override int ExpectedResponsePduLength => 5;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteUInt16(Reference);
        writer.WriteUInt16(Register.Value);
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析请求。</summary>
    public static WriteRegisterRequest FromPdu(byte[] pdu)
    {
        var reader = new ModbusPduReader(pdu, 1);
        int reference = reader.ReadUInt16();
        return new WriteRegisterRequest(reference, reader.ReadUInt16());
    }
}

/// <summary>FC06 写单个寄存器响应（回显请求）。</summary>
public sealed class WriteRegisterResponse : ModbusResponse
{
    /// <summary>以地址与值构造响应。</summary>
    public WriteRegisterResponse(int reference, int value)
    {
        Reference = reference;
        Value = value;
    }

    /// <summary>寄存器地址。</summary>
    public int Reference { get; }

    /// <summary>写入的值。</summary>
    public int Value { get; }

    /// <summary>写回的寄存器对象。</summary>
    public Register Register => new SimpleRegister(Value);

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.WriteSingleRegister;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteUInt16(Reference);
        writer.WriteUInt16(Value);
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析响应。</summary>
    public static WriteRegisterResponse FromPdu(byte[] pdu)
    {
        if (pdu.Length < 5)
        {
            throw new ModbusIOException("写寄存器响应 PDU 长度不足（需要 5 字节）。");
        }

        var reader = new ModbusPduReader(pdu, 1);
        int reference = reader.ReadUInt16();
        return new WriteRegisterResponse(reference, reader.ReadUInt16());
    }
}
