using System.Text;
using Framework.Modbus.Messages;
using Framework.Modbus.Net;
using Framework.Modbus.Util;

namespace Framework.Modbus.IO;

/// <summary>
/// Modbus ASCII 传输层：可打印十六进制字符 + LRC 校验。
/// 对应 jamod 的 <c>net.wimpi.modbus.io.ModbusASCIITransport</c>。
/// </summary>
/// <remarks>
/// 帧格式：<c>':' + 地址(2 hex) + PDU(N hex) + LRC(2 hex) + CRLF</c>。
/// </remarks>
public sealed class ModbusAsciiTransport : ModbusSerialTransport
{
    private PooledByteBuf? _asciiSendBuffer;

    /// <summary>以串口连接构造 ASCII 传输层。</summary>
    public ModbusAsciiTransport(SerialConnection connection) : this(connection, connection.Parameters)
    {
    }

    /// <summary>以任意字节流连接与串口参数构造 ASCII 传输层（便于测试与自定义链路）。</summary>
    public ModbusAsciiTransport(ITransportConnection connection, SerialParameters parameters)
        : base(connection, parameters)
    {
    }

    /// <inheritdoc />
    public override void WriteMessage(ModbusMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        // 先组二进制 ADU（地址 + PDU），用于计算 LRC
        var adu = SendBuffer;
        adu.Clear();
        adu.WriteByte((byte)message.UnitId);
        message.WritePdu(adu);

        byte lrc = ModbusUtil.CalculateLrc(adu.WrittenSpan());

        // 再直接转成十六进制字符写入发送缓冲，省掉 StringBuilder / 十六进制字符串 / 帧数组
        var frame = AsciiSendBuffer;
        frame.Clear();
        frame.WriteByte((byte)ModbusConstants.AsciiStart);
        ModbusUtil.WriteHex(frame, adu.WrittenSpan());
        ModbusUtil.WriteHex(frame, lrc);
        frame.WriteByte((byte)'\r');
        frame.WriteByte((byte)'\n');

        PrepareToSend();
        Connection.Write(frame.WrittenSpan());
        Connection.Flush();

        RecordSentFrame(frame.WriterIndex);

        if (HasMessageSentSubscribers)
        {
            OnMessageSent(message, frame.ToFullArray());
        }
    }

    /// <inheritdoc />
    public override ModbusResponse ReadResponse(ModbusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        DiscardEcho();

        // 十六进制字符先落进接收缓冲，随后就地解码，省掉 List<byte> / 字符串 / 中间数组
        var buffer = ReceiveBuffer;
        ReadAsciiPayload(buffer);

        var hex = buffer.WrittenSpan();
        if ((hex.Length & 1) != 0)
        {
            throw new ModbusIOException("ASCII 帧的十六进制字符数为奇数。");
        }

        int decodedLength = hex.Length / 2;
        if (decodedLength < 3)
        {
            throw new ModbusIOException("ASCII 帧长度不足（至少需要 地址 + 功能码 + LRC）。");
        }

        // 就地解码：写入位置 i 始终小于读取位置 2i，不会覆盖尚未读取的字符
        for (int i = 0; i < decodedLength; i++)
        {
            int hi = HexValue(hex[2 * i]);
            int lo = HexValue(hex[(2 * i) + 1]);
            if (hi < 0 || lo < 0)
            {
                throw new ModbusIOException("ASCII 帧不是合法的十六进制字符串。");
            }

            buffer.SetByte(i, (byte)((hi << 4) | lo));
        }

        buffer.SetWriterIndex(decodedLength);

        var decoded = buffer.WrittenSpan();
        byte expectedLrc = ModbusUtil.CalculateLrc(decoded[..^1]);
        if (expectedLrc != decoded[^1])
        {
            throw new ModbusIOException($"ASCII 帧 LRC 校验失败：期望 0x{expectedLrc:X2}，实际 0x{decoded[^1]:X2}。");
        }

        int unitId = decoded[0];
        // LRC 只占末尾 1 字节（RTU 的 CRC 才占 2 字节）
        var pdu = decoded[1..^1].ToArray();
        if (pdu.Length == 0)
        {
            throw new ModbusIOException("ASCII 帧中没有 PDU。");
        }

        if (pdu[0] != request.FunctionCode
            && pdu[0] != (request.FunctionCode | ModbusFunctionCode.ExceptionMask))
        {
            throw new ModbusIOException(
                $"响应功能码 0x{pdu[0]:X2} 与请求功能码 0x{request.FunctionCode:X2} 不匹配。");
        }

        var response = ModbusResponseFactory.FromPdu(pdu);
        response.UnitId = unitId;

        if (HasMessageReceivedSubscribers)
        {
            OnMessageReceived(response, buffer.ToFullArray());
        }

        return response;
    }

    /// <summary>送达的帧长度（十六进制字符版）上限：<c>':' + 2×(地址+PDU+LRC) + CRLF</c>。</summary>
    private PooledByteBuf AsciiSendBuffer =>
        _asciiSendBuffer ??= new PooledByteBuf(1 + (2 * (ModbusConstants.MaxPduSize + 2)) + 2);

    /// <inheritdoc />
    public override void Dispose()
    {
        _asciiSendBuffer?.Dispose();
        _asciiSendBuffer = null;
        base.Dispose();
    }

    /// <summary>以串口参数构造 ASCII 传输层。</summary>
    public static ModbusAsciiTransport Create(SerialParameters parameters) =>
        new(new SerialConnection(parameters));

    /// <summary>丢弃半双工链路回显的整帧数据。</summary>
    private void DiscardEcho()
    {
        if (!Parameters.Echo || LastSentFrameLength <= 0)
        {
            return;
        }

        var buffer = new byte[LastSentFrameLength];
        int total = 0;
        long deadline = Environment.TickCount64 + Math.Max(Timeout, 1);

        while (total < buffer.Length && Environment.TickCount64 < deadline)
        {
            int read = Connection.Read(buffer, total, buffer.Length - total);
            if (read > 0)
            {
                total += read;
            }
        }
    }

    /// <summary>读取 '<c>:</c>' 与 CRLF 之间的十六进制字符，写入 <paramref name="buffer"/>。</summary>
    private void ReadAsciiPayload(PooledByteBuf buffer)
    {
        buffer.Clear();
        long deadline = Environment.TickCount64 + Math.Max(Timeout, 1);

        while (Environment.TickCount64 < deadline)
        {
            // 每次只取 1 字节窗口：读到终止符时不推进，避免把 '\n' 计入载荷
            var span = buffer.GetSpan(1)[..1];
            int read = Connection.Read(span);
            if (read <= 0)
            {
                continue;
            }

            byte b = span[0];
            if (b == (byte)'\n')
            {
                break;
            }

            if (b is (byte)'\r' or (byte)':')
            {
                continue;
            }

            buffer.Advance(1);
        }

        if (buffer.WriterIndex == 0)
        {
            throw new ModbusIOException($"读取 ASCII 帧超时（{Timeout} ms）。");
        }
    }

    /// <summary>把十六进制字符转成 0-15；非法字符返回 -1。</summary>
    private static int HexValue(byte c) => c switch
    {
        >= (byte)'0' and <= (byte)'9' => c - '0',
        >= (byte)'A' and <= (byte)'F' => c - 'A' + 10,
        >= (byte)'a' and <= (byte)'f' => c - 'a' + 10,
        _ => -1,
    };
}
