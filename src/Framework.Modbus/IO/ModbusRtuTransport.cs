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

        var buffer = SendBuffer;
        buffer.Clear();
        buffer.WriteByte((byte)message.UnitId);
        message.WritePdu(buffer);

        // CRC 直接对"已写入的字节"计算并追加，不再单独生成 PDU / 帧数组
        int crc = ModbusUtil.CalculateCrc16(buffer.WrittenSpan());
        buffer.WriteByte((byte)(crc & 0xFF));
        buffer.WriteByte((byte)((crc >> 8) & 0xFF));

        PrepareToSend();
        Connection.Write(buffer.WrittenSpan());
        Connection.Flush();

        RecordSentFrame(buffer.WriterIndex);

        if (HasMessageSentSubscribers)
        {
            OnMessageSent(message, buffer.ToFullArray());
        }
    }

    /// <inheritdoc />
    public override ModbusResponse ReadResponse(ModbusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var buffer = ReceiveBuffer;
        buffer.Clear();
        ReadInto(buffer, 2, "单元标识与功能码");

        if (Parameters.Echo)
        {
            // 半双工回显：先丢弃上一帧剩余字节，再重新读取真正的帧头
            int discard = LastSentFrameLength - 2;
            if (discard > 0)
            {
                ReadFullyOrThrow(discard, "回显数据");
            }

            buffer.Clear();
            ReadInto(buffer, 2, "单元标识与功能码");
        }

        var head = buffer.WrittenSpan();
        int unitId = head[0];
        int functionCode = head[1];

        if (functionCode != request.FunctionCode
            && functionCode != (request.FunctionCode | ModbusFunctionCode.ExceptionMask))
        {
            throw new ModbusIOException(
                $"响应功能码 0x{functionCode:X2} 与请求功能码 0x{request.FunctionCode:X2} 不匹配。");
        }

        ReadResponseTail(buffer, request, functionCode);
        ReadInto(buffer, 2, "CRC");

        var frame = buffer.WrittenSpan();
        if (!ModbusUtil.ValidateCrc16(frame))
        {
            throw new ModbusIOException($"RTU 帧 CRC 校验失败：{ModbusUtil.Dump(buffer.ToFullArray())}。");
        }

        var response = ModbusResponseFactory.FromPdu(frame[1..^2].ToArray());
        response.UnitId = unitId;

        if (HasMessageReceivedSubscribers)
        {
            OnMessageReceived(response, buffer.ToFullArray());
        }

        return response;
    }

    /// <summary>
    /// 读完功能码之后的尾部内容，直接追加进响应缓冲。
    /// 优先采用请求声明的期望长度；变长响应则先读长度前缀（字节计数），再按其声明的长度读取数据。
    /// </summary>
    private void ReadResponseTail(PooledByteBuf buffer, ModbusRequest request, int functionCode)
    {
        if ((functionCode & ModbusFunctionCode.ExceptionMask) != 0)
        {
            ReadInto(buffer, 1, "异常码");
            return;
        }

        int expected = request.ExpectedResponsePduLength;
        if (expected > 0)
        {
            // expected 含功能码本身，功能码已在帧头读入
            ReadInto(buffer, expected - 1, "响应数据");
            return;
        }

        switch ((byte)functionCode)
        {
            case ModbusFunctionCode.GetCommEventLog:
            case ModbusFunctionCode.ReportSlaveId:
            case ModbusFunctionCode.ReadFileRecord:
            case ModbusFunctionCode.WriteFileRecord:
            {
                ReadInto(buffer, 1, "字节计数");
                int byteCount = buffer.WrittenSpan()[^1];
                ReadInto(buffer, byteCount, "响应数据");
                return;
            }

            case ModbusFunctionCode.ReadFifoQueue:
            {
                ReadInto(buffer, 2, "字节计数");
                var counts = buffer.WrittenSpan();
                int count = (counts[^2] << 8) | counts[^1];
                ReadInto(buffer, count, "响应数据");
                return;
            }

            default:
                throw new ModbusIOException($"无法确定功能码 0x{functionCode:X2} 的响应长度。");
        }
    }

    /// <summary>以串口参数构造 RTU 传输层。</summary>
    public static ModbusRtuTransport Create(SerialParameters parameters) =>
        new(new SerialConnection(parameters));
}
