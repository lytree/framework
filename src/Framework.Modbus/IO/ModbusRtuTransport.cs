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

        var (prefix, remaining) = ResolveTail(request, functionCode);

        var pdu = new byte[1 + prefix.Length + remaining];
        pdu[0] = (byte)functionCode;
        if (prefix.Length > 0)
        {
            Array.Copy(prefix, 0, pdu, 1, prefix.Length);
        }

        if (remaining > 0)
        {
            var rest = ReadFullyOrThrow(remaining, "响应数据");
            Array.Copy(rest, 0, pdu, 1 + prefix.Length, remaining);
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
    /// 计算功能码之后的尾部内容。
    /// 优先采用请求声明的期望长度；变长响应则先读出长度前缀（字节计数），
    /// 把它作为 PDU 前缀返回，再读取其声明的数据长度。
    /// </summary>
    /// <returns>已读出的长度前缀，以及其后仍需读取的数据字节数。</returns>
    private (byte[] Prefix, int Remaining) ResolveTail(ModbusRequest request, int functionCode)
    {
        if ((functionCode & ModbusFunctionCode.ExceptionMask) != 0)
        {
            return (Array.Empty<byte>(), 1);
        }

        int expected = request.ExpectedResponsePduLength;
        if (expected > 0)
        {
            return (Array.Empty<byte>(), expected - 1);
        }

        switch ((byte)functionCode)
        {
            case ModbusFunctionCode.GetCommEventLog:
            case ModbusFunctionCode.ReportSlaveId:
            case ModbusFunctionCode.ReadFileRecord:
            case ModbusFunctionCode.WriteFileRecord:
            {
                int byteCount = ReadByteOrThrow("字节计数");
                return (new[] { (byte)byteCount }, byteCount);
            }

            case ModbusFunctionCode.ReadFifoQueue:
            {
                int hi = ReadByteOrThrow("字节计数");
                int lo = ReadByteOrThrow("字节计数");
                return (new[] { (byte)hi, (byte)lo }, (hi << 8) | lo);
            }

            default:
                throw new ModbusIOException($"无法确定功能码 0x{functionCode:X2} 的响应长度。");
        }
    }

    /// <summary>以串口参数构造 RTU 传输层。</summary>
    public static ModbusRtuTransport Create(SerialParameters parameters) =>
        new(new SerialConnection(parameters));
}
