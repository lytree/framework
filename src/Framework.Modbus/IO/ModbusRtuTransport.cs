using Framework.Modbus.Messages;
using Framework.Modbus.Net;
using Framework.Modbus.Util;

namespace Framework.Modbus.IO;

/// <summary>
/// Modbus RTU 传输层：二进制帧 + CRC16。
/// 对应 jamod 的 <c>net.wimpi.modbus.io.ModbusRTUTransport</c>。
/// </summary>
public sealed class ModbusRtuTransport : ModbusSerialTransport
{
    /// <summary>以串口连接构造 RTU 传输层。</summary>
    public ModbusRtuTransport(SerialConnection connection) : this(connection, connection.Parameters)
    {
    }

    /// <summary>以任意字节流连接与串口参数构造 RTU 传输层（便于测试与自定义链路）。</summary>
    public ModbusRtuTransport(ITransportConnection connection, SerialParameters parameters)
        : base(connection, parameters)
    {
    }

    /// <inheritdoc />
    public override void WriteMessage(ModbusMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var pdu = message.ToPdu();
        var frame = new byte[pdu.Length + 3];

        frame[0] = (byte)message.UnitId;
        Array.Copy(pdu, 0, frame, 1, pdu.Length);
        ModbusUtil.AppendCrc16(frame, 0, frame.Length - 2);

        PrepareToSend();
        Connection.Write(frame, 0, frame.Length);
        Connection.Flush();

        RecordSentFrame(frame.Length);
        OnMessageSent(message, frame);
    }

    /// <inheritdoc />
    public override ModbusResponse ReadResponse(ModbusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        int unitId = ReadByteOrThrow("单元标识");
        int functionCode = ReadByteOrThrow("功能码");

        if (Parameters.Echo)
        {
            int discard = LastSentFrameLength - 2;
            if (discard > 0)
            {
                ReadFullyOrThrow(discard, "回显数据");
            }

            unitId = ReadByteOrThrow("单元标识");
            functionCode = ReadByteOrThrow("功能码");
        }

        if (functionCode != request.FunctionCode
            && functionCode != (request.FunctionCode | ModbusFunctionCode.ExceptionMask))
        {
            throw new ModbusIOException(
                $"响应功能码 0x{functionCode:X2} 与请求功能码 0x{request.FunctionCode:X2} 不匹配。");
        }

        int remaining = ResolveRemainingLength(request, functionCode);
        var pdu = new byte[1 + remaining];
        pdu[0] = (byte)functionCode;

        if (remaining > 0)
        {
            var rest = ReadFullyOrThrow(remaining, "响应数据");
            Array.Copy(rest, 0, pdu, 1, remaining);
        }

        var crcBytes = ReadFullyOrThrow(2, "CRC");

        var frame = new byte[1 + pdu.Length + 2];
        frame[0] = (byte)unitId;
        Array.Copy(pdu, 0, frame, 1, pdu.Length);
        crcBytes.CopyTo(frame, 1 + pdu.Length);

        if (!ModbusUtil.ValidateCrc16(frame, 0, frame.Length))
        {
            throw new ModbusIOException($"RTU 帧 CRC 校验失败：{ModbusUtil.Dump(frame)}。");
        }

        var response = ModbusResponseFactory.FromPdu(pdu);
        response.UnitId = unitId;
        OnMessageReceived(response, frame);
        return response;
    }

    /// <summary>
    /// 计算功能码之后仍需读取的字节数。
    /// 优先采用请求声明的期望长度；变长响应则先读取字节计数再决定。
    /// </summary>
    private int ResolveRemainingLength(ModbusRequest request, int functionCode)
    {
        if ((functionCode & ModbusFunctionCode.ExceptionMask) != 0)
        {
            return 1;
        }

        int expected = request.ExpectedResponsePduLength;
        if (expected > 0)
        {
            return expected - 1;
        }

        return (byte)functionCode switch
        {
            ModbusFunctionCode.GetCommEventLog
                or ModbusFunctionCode.ReportSlaveId
                or ModbusFunctionCode.ReadFileRecord
                or ModbusFunctionCode.WriteFileRecord => 1 + ReadByteOrThrow("字节计数"),
            ModbusFunctionCode.ReadFifoQueue => ReadFifoByteCount(),
            _ => throw new ModbusIOException($"无法确定功能码 0x{functionCode:X2} 的响应长度。"),
        };
    }

    /// <summary>读 FIFO 队列响应的两字节字节计数，并换算为功能码之后的剩余长度。</summary>
    private int ReadFifoByteCount()
    {
        int hi = ReadByteOrThrow("字节计数");
        int lo = ReadByteOrThrow("字节计数");
        return 2 + ((hi << 8) | lo);
    }

    /// <summary>以串口参数构造 RTU 传输层。</summary>
    public static ModbusRtuTransport Create(SerialParameters parameters) =>
        new(new SerialConnection(parameters));
}
