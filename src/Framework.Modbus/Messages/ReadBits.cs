using System.Buffers;
using Framework.Modbus.Util;
using Abstractions = Framework.Modbus.Abstractions;

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
        using var writer = new ModbusPduWriter();
        WritePdu(writer);
        return writer.ToArray();
    }

    /// <inheritdoc />
    public override void WritePdu(IBufferWriter<byte> writer)
    {
        Abstractions.ModbusRequest pdu = FunctionCode == ModbusFunctionCode.ReadCoils
            ? new Abstractions.ReadCoilsRequest((ushort)Reference, (ushort)BitCount)
            : new Abstractions.ReadDiscreteInputsRequest((ushort)Reference, (ushort)BitCount);
        PduBridge.Write(writer, pdu);
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
        using var writer = new ModbusPduWriter();
        WritePdu(writer);
        return writer.ToArray();
    }

    /// <inheritdoc />
    public override void WritePdu(IBufferWriter<byte> writer)
    {
        PduBridge.Write(writer, new Abstractions.ReadBitsResponse(FunctionCode, Bits.ToByteArray()));
    }

    /// <summary>从 PDU 解析位数据。</summary>
    protected static BitVector ReadBitsFromPdu(byte[] pdu, byte expectedFunctionCode)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        var decoded = PduBridge.DecodeAs(
            () => Abstractions.ReadBitsResponse.Decode(expectedFunctionCode, pdu),
            "位读取响应");
        return new BitVector(decoded.Data.ToArray());
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
        ArgumentNullException.ThrowIfNull(pdu);
        var decoded = PduBridge.DecodeAs(
            () => Abstractions.ReadCoilsRequest.Decode(pdu.AsSpan(1)),
            "读线圈请求");
        return new ReadCoilsRequest(decoded.Address, decoded.Count);
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
        ArgumentNullException.ThrowIfNull(pdu);
        var decoded = PduBridge.DecodeAs(
            () => Abstractions.ReadDiscreteInputsRequest.Decode(pdu.AsSpan(1)),
            "读离散输入请求");
        return new ReadInputDiscretesRequest(decoded.Address, decoded.Count);
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
