using Framework.Modbus.ProcessImage;
using Framework.Modbus.Util;

namespace Framework.Modbus.Messages;

/// <summary>读取寄存器（保持寄存器 / 输入寄存器）请求的公共实现。</summary>
public abstract class ReadRegistersRequest : ModbusRequest
{
    /// <summary>构造请求。</summary>
    /// <param name="reference">起始地址（PDU 偏移，从 0 开始）。</param>
    /// <param name="wordCount">读取的寄存器数量。</param>
    protected ReadRegistersRequest(int reference, int wordCount)
    {
        if (reference is < 0 or > 0xFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(reference), reference, "起始地址必须落在 [0, 65535]。");
        }

        if (wordCount is < 1 or > ModbusConstants.MaxReadRegisters)
        {
            throw new ArgumentOutOfRangeException(nameof(wordCount), wordCount, $"寄存器数量必须落在 [1, {ModbusConstants.MaxReadRegisters}]。");
        }

        Reference = reference;
        WordCount = wordCount;
    }

    /// <summary>起始地址。</summary>
    public int Reference { get; }

    /// <summary>读取的寄存器数量。</summary>
    public int WordCount { get; }

    /// <inheritdoc />
    public override int ExpectedResponsePduLength => 2 + (WordCount * 2);

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteUInt16(Reference);
        writer.WriteUInt16(WordCount);
        return writer.ToArray();
    }
}

/// <summary>读取寄存器响应的公共实现。</summary>
public abstract class ReadRegistersResponse : ModbusResponse
{
    /// <summary>以寄存器数组构造响应。</summary>
    protected ReadRegistersResponse(IEnumerable<int> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Values = values.ToArray();
        Registers = Values.Select(v => (Register)new SimpleRegister(v)).ToArray();
    }

    /// <summary>读取到的寄存器原始值。</summary>
    public int[] Values { get; }

    /// <summary>读取到的寄存器对象。</summary>
    public Register[] Registers { get; }

    /// <summary>数据字节数。</summary>
    public int ByteCount => Values.Length * 2;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var data = ModbusUtil.RegistersToBytes(Values);
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteByte(data.Length);
        writer.WriteBytes(data);
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析寄存器数据。</summary>
    protected static int[] ReadRegistersFromPdu(byte[] pdu, byte expectedFunctionCode)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        if (pdu.Length < 2)
        {
            throw new ModbusIOException("寄存器读取响应 PDU 长度不足。");
        }

        if (pdu[0] != expectedFunctionCode)
        {
            throw new ModbusIOException(
                $"响应功能码不匹配：期望 0x{expectedFunctionCode:X2}，实际 0x{pdu[0]:X2}。");
        }

        int byteCount = pdu[1];
        if (byteCount % 2 != 0)
        {
            throw new ModbusIOException($"寄存器读取响应的字节计数 {byteCount} 不是偶数。");
        }

        if (pdu.Length < 2 + byteCount)
        {
            throw new ModbusIOException(
                $"寄存器读取响应声明 {byteCount} 字节数据，但实际只有 {pdu.Length - 2} 字节。");
        }

        return ModbusUtil.BytesToRegisters(pdu, 2, byteCount / 2);
    }
}

/// <summary>FC03 读保持寄存器请求。</summary>
public class ReadMultipleRegistersRequest : ReadRegistersRequest
{
    /// <summary>构造请求。</summary>
    public ReadMultipleRegistersRequest(int reference, int wordCount) : base(reference, wordCount)
    {
    }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.ReadMultipleRegisters;

    /// <summary>从 PDU 解析请求。</summary>
    public static ReadMultipleRegistersRequest FromPdu(byte[] pdu)
    {
        var reader = new ModbusPduReader(pdu, 1);
        return new ReadMultipleRegistersRequest(reader.ReadUInt16(), reader.ReadUInt16());
    }
}

/// <summary>FC03 读保持寄存器响应。</summary>
public sealed class ReadMultipleRegistersResponse : ReadRegistersResponse
{
    /// <summary>以寄存器值构造响应。</summary>
    public ReadMultipleRegistersResponse(IEnumerable<int> values) : base(values)
    {
    }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.ReadMultipleRegisters;

    /// <summary>从 PDU 解析响应。</summary>
    public static ReadMultipleRegistersResponse FromPdu(byte[] pdu) =>
        new(ReadRegistersFromPdu(pdu, ModbusFunctionCode.ReadMultipleRegisters));
}

/// <summary>FC04 读输入寄存器请求。</summary>
public class ReadInputRegistersRequest : ReadRegistersRequest
{
    /// <summary>构造请求。</summary>
    public ReadInputRegistersRequest(int reference, int wordCount) : base(reference, wordCount)
    {
    }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.ReadInputRegisters;

    /// <summary>从 PDU 解析请求。</summary>
    public static ReadInputRegistersRequest FromPdu(byte[] pdu)
    {
        var reader = new ModbusPduReader(pdu, 1);
        return new ReadInputRegistersRequest(reader.ReadUInt16(), reader.ReadUInt16());
    }
}

/// <summary>FC04 读输入寄存器响应。</summary>
public sealed class ReadInputRegistersResponse : ReadRegistersResponse
{
    /// <summary>以寄存器值构造响应。</summary>
    public ReadInputRegistersResponse(IEnumerable<int> values) : base(values)
    {
    }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.ReadInputRegisters;

    /// <summary>从 PDU 解析响应。</summary>
    public static ReadInputRegistersResponse FromPdu(byte[] pdu) =>
        new(ReadRegistersFromPdu(pdu, ModbusFunctionCode.ReadInputRegisters));
}
