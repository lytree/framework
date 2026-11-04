using System.Buffers;
using Framework.Modbus.ProcessImage;
using Framework.Modbus.Util;
using Abstractions = Framework.Modbus.Abstractions;

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
        using var writer = new ModbusPduWriter();
        WritePdu(writer);
        return writer.ToArray();
    }

    /// <inheritdoc />
    public override void WritePdu(IBufferWriter<byte> writer)
    {
        var values = new bool[BitCount];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = Coils[i];
        }

        PduBridge.Write(
            writer,
            new Abstractions.WriteMultipleCoilsRequest((ushort)Reference, (ushort)BitCount, values));
    }

    /// <summary>从 PDU 解析请求。</summary>
    public static WriteMultipleCoilsRequest FromPdu(byte[] pdu)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        var decoded = PduBridge.DecodeAs(
            () => Abstractions.WriteMultipleCoilsRequest.Decode(pdu.AsSpan(1)),
            "写多个线圈请求");

        var coils = new BitVector(decoded.Count);
        for (int i = 0; i < decoded.Count; i++)
        {
            coils.SetBit(i, decoded.Values[i]);
        }

        return new WriteMultipleCoilsRequest(decoded.Address, coils);
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
        using var writer = new ModbusPduWriter();
        WritePdu(writer);
        return writer.ToArray();
    }

    /// <inheritdoc />
    public override void WritePdu(IBufferWriter<byte> writer)
    {
        PduBridge.Write(
            writer,
            new Abstractions.WriteMultipleResponse(FunctionCode, (ushort)Reference, (ushort)BitCount));
    }

    /// <summary>从 PDU 解析响应。</summary>
    public static WriteMultipleCoilsResponse FromPdu(byte[] pdu)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        var decoded = PduBridge.DecodeAs(
            () => Abstractions.WriteMultipleResponse.Decode(ModbusFunctionCode.WriteMultipleCoils, pdu),
            "写多个线圈响应");
        return new WriteMultipleCoilsResponse(decoded.Address, decoded.Count);
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
        using var writer = new ModbusPduWriter();
        WritePdu(writer);
        return writer.ToArray();
    }

    /// <inheritdoc />
    public override void WritePdu(IBufferWriter<byte> writer)
    {
        var values = new ushort[Registers.Length];
        for (int i = 0; i < Registers.Length; i++)
        {
            values[i] = (ushort)Registers[i].Value;
        }

        PduBridge.Write(
            writer,
            new Abstractions.WriteMultipleRegistersRequest((ushort)Reference, (ushort)WordCount, values));
    }

    /// <summary>从 PDU 解析请求。</summary>
    public static WriteMultipleRegistersRequest FromPdu(byte[] pdu)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        var decoded = PduBridge.DecodeAs(
            () => Abstractions.WriteMultipleRegistersRequest.Decode(pdu.AsSpan(1)),
            "写多个寄存器请求");

        var registers = new Register[decoded.Values.Length];
        for (int i = 0; i < registers.Length; i++)
        {
            registers[i] = new SimpleRegister(decoded.Values[i]);
        }

        return new WriteMultipleRegistersRequest(decoded.Address, registers);
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
        using var writer = new ModbusPduWriter();
        WritePdu(writer);
        return writer.ToArray();
    }

    /// <inheritdoc />
    public override void WritePdu(IBufferWriter<byte> writer)
    {
        PduBridge.Write(
            writer,
            new Abstractions.WriteMultipleResponse(FunctionCode, (ushort)Reference, (ushort)WordCount));
    }

    /// <summary>从 PDU 解析响应。</summary>
    public static WriteMultipleRegistersResponse FromPdu(byte[] pdu)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        var decoded = PduBridge.DecodeAs(
            () => Abstractions.WriteMultipleResponse.Decode(ModbusFunctionCode.WriteMultipleRegisters, pdu),
            "写多个寄存器响应");
        return new WriteMultipleRegistersResponse(decoded.Address, decoded.Count);
    }
}
