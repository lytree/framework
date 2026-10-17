using Framework.Modbus.ProcessImage;

namespace Framework.Modbus.Messages;

/// <summary>FC17 读写多个寄存器请求。对应 jamod 的 <c>ReadWriteMultipleRegistersRequest</c>。</summary>
/// <remarks>先执行写，再执行读；读与写的地址区间不允许重叠。</remarks>
public class ReadWriteMultipleRegistersRequest : ModbusRequest
{
    /// <summary>构造请求。</summary>
    public ReadWriteMultipleRegistersRequest(
        int readReference,
        int readWordCount,
        int writeReference,
        Register[] writeRegisters)
    {
        ArgumentNullException.ThrowIfNull(writeRegisters);
        if (readReference is < 0 or > 0xFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(readReference), readReference, "读起始地址必须落在 [0, 65535]。");
        }

        if (readWordCount is < 1 or > ModbusConstants.MaxReadRegisters)
        {
            throw new ArgumentOutOfRangeException(nameof(readWordCount), readWordCount, $"读取寄存器数量必须落在 [1, {ModbusConstants.MaxReadRegisters}]。");
        }

        if (writeReference is < 0 or > 0xFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(writeReference), writeReference, "写起始地址必须落在 [0, 65535]。");
        }

        if (writeRegisters.Length is < 1 or > ModbusConstants.MaxWriteRegisters)
        {
            throw new ArgumentOutOfRangeException(nameof(writeRegisters), writeRegisters.Length, $"写入寄存器数量必须落在 [1, {ModbusConstants.MaxWriteRegisters}]。");
        }

        ReadReference = readReference;
        ReadWordCount = readWordCount;
        WriteReference = writeReference;
        WriteRegisters = writeRegisters;
    }

    /// <summary>以寄存器值数组构造请求。</summary>
    public ReadWriteMultipleRegistersRequest(
        int readReference,
        int readWordCount,
        int writeReference,
        IEnumerable<int> writeValues)
        : this(readReference, readWordCount, writeReference, ToRegisters(writeValues))
    {
    }

    /// <summary>读起始地址。</summary>
    public int ReadReference { get; }

    /// <summary>读取的寄存器数量。</summary>
    public int ReadWordCount { get; }

    /// <summary>写起始地址。</summary>
    public int WriteReference { get; }

    /// <summary>待写入的寄存器。</summary>
    public Register[] WriteRegisters { get; }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.ReadWriteMultipleRegisters;

    /// <inheritdoc />
    public override int ExpectedResponsePduLength => 2 + (ReadWordCount * 2);

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var data = new byte[WriteRegisters.Length * 2];
        for (int i = 0; i < WriteRegisters.Length; i++)
        {
            var bytes = WriteRegisters[i].ToBytes();
            data[i * 2] = bytes[0];
            data[(i * 2) + 1] = bytes[1];
        }

        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteUInt16(ReadReference);
        writer.WriteUInt16(ReadWordCount);
        writer.WriteUInt16(WriteReference);
        writer.WriteUInt16(WriteRegisters.Length);
        writer.WriteByte(data.Length);
        writer.WriteBytes(data);
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析请求。</summary>
    public static ReadWriteMultipleRegistersRequest FromPdu(byte[] pdu)
    {
        var reader = new ModbusPduReader(pdu, 1);
        int readReference = reader.ReadUInt16();
        int readWordCount = reader.ReadUInt16();
        int writeReference = reader.ReadUInt16();
        int writeWordCount = reader.ReadUInt16();
        int byteCount = reader.ReadByte();
        var data = reader.ReadBytes(byteCount);
        var registers = new Register[writeWordCount];
        for (int i = 0; i < writeWordCount; i++)
        {
            registers[i] = new SimpleRegister(data[i * 2], data[(i * 2) + 1]);
        }

        return new ReadWriteMultipleRegistersRequest(readReference, readWordCount, writeReference, registers);
    }

    private static Register[] ToRegisters(IEnumerable<int> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.Select(v => (Register)new SimpleRegister(v)).ToArray();
    }
}

/// <summary>FC17 读写多个寄存器响应（格式与 FC03 相同）。</summary>
public sealed class ReadWriteMultipleRegistersResponse : ReadRegistersResponse
{
    /// <summary>以寄存器值构造响应。</summary>
    public ReadWriteMultipleRegistersResponse(IEnumerable<int> values) : base(values)
    {
    }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.ReadWriteMultipleRegisters;

    /// <summary>从 PDU 解析响应。</summary>
    public static ReadWriteMultipleRegistersResponse FromPdu(byte[] pdu) =>
        new(ReadRegistersFromPdu(pdu, ModbusFunctionCode.ReadWriteMultipleRegisters));
}
