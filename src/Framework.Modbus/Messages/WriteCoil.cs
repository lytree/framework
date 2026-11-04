using System.Buffers;
using Framework.Modbus.Util;
using Abstractions = Framework.Modbus.Abstractions;

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
        using var writer = new ModbusPduWriter();
        WritePdu(writer);
        return writer.ToArray();
    }

    /// <inheritdoc />
    public override void WritePdu(IBufferWriter<byte> writer)
    {
        PduBridge.Write(writer, new Abstractions.WriteSingleCoilRequest((ushort)Reference, CoilState));
    }

    /// <summary>从 PDU 解析请求。</summary>
    public static WriteCoilRequest FromPdu(byte[] pdu)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        var decoded = PduBridge.DecodeAs(
            () => Abstractions.WriteSingleCoilRequest.Decode(pdu.AsSpan(1)),
            "写单个线圈请求");
        return new WriteCoilRequest(decoded.Address, decoded.Value);
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
        using var writer = new ModbusPduWriter();
        WritePdu(writer);
        return writer.ToArray();
    }

    /// <inheritdoc />
    public override void WritePdu(IBufferWriter<byte> writer)
    {
        PduBridge.Write(
            writer,
            new Abstractions.WriteSingleResponse(FunctionCode, (ushort)Reference, (ushort)CoilValue));
    }

    /// <summary>从 PDU 解析响应。</summary>
    public static WriteCoilResponse FromPdu(byte[] pdu)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        var decoded = PduBridge.DecodeAs(
            () => Abstractions.WriteSingleResponse.Decode(ModbusFunctionCode.WriteCoil, pdu),
            "写单个线圈响应");
        return new WriteCoilResponse(decoded.Address, decoded.Value == Abstractions.WriteSingleCoilRequest.CoilOn);
    }
}
