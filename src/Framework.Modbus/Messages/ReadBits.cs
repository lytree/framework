using Framework.Modbus.Util;

namespace Framework.Modbus.Messages;

/// <summary>读取位（线圈 / 离散输入）请求的公共实现。</summary>
public abstract class ReadBitsRequest : ModbusRequest
{
    /// <summary>构造请求。</summary>
    /// <param name="reference">起始地址（PDU 偏移，从 0 开始）。</param>
    /// <param name="bitCount">读取的位数。</param>
    protected ReadBitsRequest(int reference, int bitCount)
    {
        if (reference is < 0 or > 0xFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(reference), reference, "起始地址必须落在 [0, 65535]。");
        }

        if (bitCount is < 1 or > ModbusConstants.MaxReadCoils)
        {
            throw new ArgumentOutOfRangeException(nameof(bitCount), bitCount, $"读取位数必须落在 [1, {ModbusConstants.MaxReadCoils}]。");
        }

        Reference = reference;
        BitCount = bitCount;
    }

    /// <summary>起始地址。</summary>
    public int Reference { get; }

    /// <summary>读取的位数。</summary>
    public int BitCount { get; }

    /// <inheritdoc />
    public override int ExpectedResponsePduLength => 2 + ((BitCount + 7) / 8);

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteUInt16(Reference);
        writer.WriteUInt16(BitCount);
        return writer.ToArray();
    }
}

/// <summary>读取位（线圈 / 离散输入）响应的公共实现。</summary>
public abstract class ReadBitsResponse : ModbusResponse
{
    /// <summary>以位向量构造响应。</summary>
    protected ReadBitsResponse(BitVector bits)
    {
        Bits = bits ?? throw new ArgumentNullException(nameof(bits));
    }

    /// <summary>读取到的位。</summary>
    public BitVector Bits { get; }

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var data = Bits.ToByteArray();
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteByte(data.Length);
        writer.WriteBytes(data);
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析位数据。</summary>
    protected static BitVector ReadBitsFromPdu(byte[] pdu, byte expectedFunctionCode)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        if (pdu.Length < 2)
        {
            throw new ModbusIOException("位读取响应 PDU 长度不足。");
        }

        if (pdu[0] != expectedFunctionCode)
        {
            throw new ModbusIOException(
                $"响应功能码不匹配：期望 0x{expectedFunctionCode:X2}，实际 0x{pdu[0]:X2}。");
        }

        int byteCount = pdu[1];
        if (pdu.Length < 2 + byteCount)
        {
            throw new ModbusIOException(
                $"位读取响应声明 {byteCount} 字节数据，但实际只有 {pdu.Length - 2} 字节。");
        }

        return new BitVector(pdu.AsSpan(2, byteCount).ToArray());
    }
}

/// <summary>FC01 读线圈请求。</summary>
public class ReadCoilsRequest : ReadBitsRequest
{
    /// <summary>构造请求。</summary>
    public ReadCoilsRequest(int reference, int bitCount) : base(reference, bitCount)
    {
    }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.ReadCoils;

    /// <summary>从 PDU 解析请求。</summary>
    public static ReadCoilsRequest FromPdu(byte[] pdu)
    {
        var reader = new ModbusPduReader(pdu, 1);
        return new ReadCoilsRequest(reader.ReadUInt16(), reader.ReadUInt16());
    }
}

/// <summary>FC01 读线圈响应。</summary>
public sealed class ReadCoilsResponse : ReadBitsResponse
{
    /// <summary>以位向量构造响应。</summary>
    public ReadCoilsResponse(BitVector coils) : base(coils)
    {
    }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.ReadCoils;

    /// <summary>读取到的线圈状态。</summary>
    public BitVector Coils => Bits;

    /// <summary>从 PDU 解析响应。</summary>
    public static ReadCoilsResponse FromPdu(byte[] pdu) =>
        new(ReadBitsFromPdu(pdu, ModbusFunctionCode.ReadCoils));
}

/// <summary>FC02 读离散输入请求。</summary>
public class ReadInputDiscretesRequest : ReadBitsRequest
{
    /// <summary>构造请求。</summary>
    public ReadInputDiscretesRequest(int reference, int bitCount) : base(reference, bitCount)
    {
    }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.ReadInputDiscretes;

    /// <summary>从 PDU 解析请求。</summary>
    public static ReadInputDiscretesRequest FromPdu(byte[] pdu)
    {
        var reader = new ModbusPduReader(pdu, 1);
        return new ReadInputDiscretesRequest(reader.ReadUInt16(), reader.ReadUInt16());
    }
}

/// <summary>FC02 读离散输入响应。</summary>
public sealed class ReadInputDiscretesResponse : ReadBitsResponse
{
    /// <summary>以位向量构造响应。</summary>
    public ReadInputDiscretesResponse(BitVector discretes) : base(discretes)
    {
    }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.ReadInputDiscretes;

    /// <summary>读取到的离散输入状态。</summary>
    public BitVector Discretes => Bits;

    /// <summary>从 PDU 解析响应。</summary>
    public static ReadInputDiscretesResponse FromPdu(byte[] pdu) =>
        new(ReadBitsFromPdu(pdu, ModbusFunctionCode.ReadInputDiscretes));
}
