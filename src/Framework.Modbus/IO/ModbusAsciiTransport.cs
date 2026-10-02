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

        var pdu = message.ToPdu();
        var adus = new byte[1 + pdu.Length];
        adus[0] = (byte)message.UnitId;
        Array.Copy(pdu, 0, adus, 1, pdu.Length);
        byte lrc = ModbusUtil.CalculateLrc(adus);

        var text = new StringBuilder((adus.Length * 2) + 3);
        text.Append(ModbusConstants.AsciiStart);
        text.Append(ModbusUtil.BytesToHex(adus));
        text.Append(lrc.ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
        text.Append(ModbusConstants.AsciiEnd);

        var frame = Encoding.ASCII.GetBytes(text.ToString());

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

        DiscardEcho();
        var ascii = ReadAsciiPayload();

        byte[] decoded;
        try
        {
            decoded = ModbusUtil.HexToBytes(Encoding.ASCII.GetString(ascii));
        }
        catch (FormatException ex)
        {
            throw new ModbusIOException("ASCII 帧不是合法的十六进制字符串。", ex);
        }

        if (decoded.Length < 3)
        {
            throw new ModbusIOException("ASCII 帧长度不足（至少需要 地址 + 功能码 + LRC）。");
        }

        byte expectedLrc = ModbusUtil.CalculateLrc(decoded, 0, decoded.Length - 1);
        if (expectedLrc != decoded[^1])
        {
            throw new ModbusIOException($"ASCII 帧 LRC 校验失败：期望 0x{expectedLrc:X2}，实际 0x{decoded[^1]:X2}。");
        }

        int unitId = decoded[0];
        var pdu = decoded.AsSpan(1, decoded.Length - 2).ToArray();
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
        OnMessageReceived(response, decoded);
        return response;
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

    /// <summary>读取 '<c>:</c>' 与 CRLF 之间的十六进制字符。</summary>
    private byte[] ReadAsciiPayload()
    {
        var collected = new List<byte>(64);
        var one = new byte[1];
        long deadline = Environment.TickCount64 + Math.Max(Timeout, 1);

        while (Environment.TickCount64 < deadline)
        {
            int read = Connection.Read(one, 0, 1);
            if (read <= 0)
            {
                continue;
            }

            byte b = one[0];
            if (b == (byte)'\n')
            {
                break;
            }

            if (b is (byte)'\r' or (byte)':')
            {
                continue;
            }

            collected.Add(b);
        }

        if (collected.Count == 0)
        {
            throw new ModbusIOException($"读取 ASCII 帧超时（{Timeout} ms）。");
        }

        return collected.ToArray();
    }
}
