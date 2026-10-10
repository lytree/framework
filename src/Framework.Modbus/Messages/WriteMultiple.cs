using Framework.Modbus.ProcessImage;
using Framework.Modbus.Util;

namespace Framework.Modbus.Messages;

/// <summary>FC0F 写多个线圈请求。对应 jamod 的 <c>WriteMultipleCoilsRequest</c>。</summary>
public class WriteMultipleCoilsRequest : ModbusRequest
{
    /// <summary>以位向量构造请求。</summary>
    public WriteMultipleCoilsRequest(int reference, BitVector coils)
    {
        ArgumentNullException.ThrowIfNull(coils);
        if (reference is < 0 or > 0xFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(reference), reference, "起始地址必须落在 [0, 65535]。");
        }

        if (coils.Size is < 1 or > ModbusConstants.MaxWriteCoils)
        {
            throw new ArgumentOutOfRangeException(nameof(coils), coils.Size, $"写入位数必须落在 [1, {ModbusConstants.MaxWriteCoils}]。");
        }

        Reference = reference;
        Coils = coils;
    }

    /// <summary>起始地址。</summary>
    public int Reference { get; }

    /// <summary>待写入的线圈。</summary>
    public BitVector Coils { get; }

    /// <summary>写入的位数。</summary>
    public int BitCount => Coils.Size;

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.WriteMultipleCoils;

    /// <inheritdoc />
    public override int ExpectedResponsePduLength => 5;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var data = Coils.GetBytes((BitCount + 7) / 8);
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteUInt16(Reference);
        writer.WriteUInt16(BitCount);
        writer.WriteByte(data.Length);
        writer.WriteBytes(data);
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析请求。</summary>
    public static WriteMultipleCoilsRequest FromPdu(byte[] pdu)
    {
        var reader = new ModbusPduReader(pdu, 1);
        int reference = reader.ReadUInt16();
        int bitCount = reader.ReadUInt16();
        int byteCount = reader.ReadByte();
        return new WriteMultipleCoilsRequest(reference, new BitVector(reader.ReadBytes(byteCount), bitCount));
    }
}

/// <summary>FC0F 写多个线圈响应。</summary>
public sealed class WriteMultipleCoilsResponse : ModbusResponse
{
    /// <summary>以起始地址与位数构造响应。</summary>
    public WriteMultipleCoilsResponse(int reference, int bitCount)
    {
        Reference = reference;
        BitCount = bitCount;
    }

    /// <summary>起始地址。</summary>
    public int Reference { get; }

    /// <summary>写入的位数。</summary>
    public int BitCount { get; }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.WriteMultipleCoils;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteUInt16(Reference);
        writer.WriteUInt16(BitCount);
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析响应。</summary>
    public static WriteMultipleCoilsResponse FromPdu(byte[] pdu)
    {
        if (pdu.Length < 5)
        {
            throw new ModbusIOException("写多个线圈响应 PDU 长度不足（需要 5 字节）。");
        }

        var reader = new ModbusPduReader(pdu, 1);
        return new WriteMultipleCoilsResponse(reader.ReadUInt16(), reader.ReadUInt16());
    }
}

/// <summary>FC10 写多个寄存器请求。对应 jamod 的 <c>WriteMultipleRegistersRequest</c>。</summary>
public class WriteMultipleRegistersRequest : ModbusRequest
{
    /// <summary>以寄存器数组构造请求。</summary>
    public WriteMultipleRegistersRequest(int reference, Register[] registers)
    {
        ArgumentNullException.ThrowIfNull(registers);
        if (reference is < 0 or > 0xFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(reference), reference, "起始地址必须落在 [0, 65535]。");
        }

        if (registers.Length is < 1 or > ModbusConstants.MaxWriteRegisters)
        {
            throw new ArgumentOutOfRangeException(nameof(registers), registers.Length, $"寄存器数量必须落在 [1, {ModbusConstants.MaxWriteRegisters}]。");
        }

        Reference = reference;
        Registers = registers;
    }

    /// <summary>以寄存器值数组构造请求。</summary>
    public WriteMultipleRegistersRequest(int reference, IEnumerable<int> values)
        : this(reference, ToRegisters(values))
    {
    }

    /// <summary>起始地址。</summary>
    public int Reference { get; }

    /// <summary>待写入的寄存器。</summary>
    public Register[] Registers { get; }

    /// <summary>写入的寄存器数量。</summary>
    public int WordCount => Registers.Length;

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.WriteMultipleRegisters;

    /// <inheritdoc />
    public override int ExpectedResponsePduLength => 5;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var data = new byte[Registers.Length * 2];
        for (int i = 0; i < Registers.Length; i++)
        {
            var bytes = Registers[i].ToBytes();
            data[i * 2] = bytes[0];
            data[(i * 2) + 1] = bytes[1];
        }

        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteUInt16(Reference);
        writer.WriteUInt16(WordCount);
        writer.WriteByte(data.Length);
        writer.WriteBytes(data);
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析请求。</summary>
    public static WriteMultipleRegistersRequest FromPdu(byte[] pdu)
    {
        var reader = new ModbusPduReader(pdu, 1);
        int reference = reader.ReadUInt16();
        int wordCount = reader.ReadUInt16();
        int byteCount = reader.ReadByte();
        var data = reader.ReadBytes(byteCount);
        var registers = new Register[wordCount];
        for (int i = 0; i < wordCount; i++)
        {
            registers[i] = new SimpleRegister(data[i * 2], data[(i * 2) + 1]);
        }

        return new WriteMultipleRegistersRequest(reference, registers);
    }

    private static Register[] ToRegisters(IEnumerable<int> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.Select(v => (Register)new SimpleRegister(v)).ToArray();
    }
}

/// <summary>FC10 写多个寄存器响应。</summary>
public sealed class WriteMultipleRegistersResponse : ModbusResponse
{
    /// <summary>以起始地址与寄存器数量构造响应。</summary>
    public WriteMultipleRegistersResponse(int reference, int wordCount)
    {
        Reference = reference;
        WordCount = wordCount;
    }

    /// <summary>起始地址。</summary>
    public int Reference { get; }

    /// <summary>写入的寄存器数量。</summary>
    public int WordCount { get; }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.WriteMultipleRegisters;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteUInt16(Reference);
        writer.WriteUInt16(WordCount);
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析响应。</summary>
    public static WriteMultipleRegistersResponse FromPdu(byte[] pdu)
    {
        if (pdu.Length < 5)
        {
            throw new ModbusIOException("写多个寄存器响应 PDU 长度不足（需要 5 字节）。");
        }

        var reader = new ModbusPduReader(pdu, 1);
        return new WriteMultipleRegistersResponse(reader.ReadUInt16(), reader.ReadUInt16());
    }
}
