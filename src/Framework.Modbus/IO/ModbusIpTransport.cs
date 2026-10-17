using Framework.Modbus.Messages;
using Framework.Modbus.Net;

namespace Framework.Modbus.IO;

/// <summary>
/// 基于 MBAP 报文头的 IP 传输层（TCP / UDP 共用）。
/// 对应 jamod 的 <c>net.wimpi.modbus.io.ModbusTCPTransport</c> / <c>ModbusUDPTransport</c> 的公共部分。
/// </summary>
/// <remarks>
/// MBAP 报文头结构（7 字节）：事务标识(2) + 协议标识(2) + 长度(2) + 单元标识(1)，
/// 其中长度字段 = 单元标识(1) + PDU 长度。
/// </remarks>
public class ModbusIpTransport : ModbusTransport
{
    /// <summary>以底层连接构造传输层。</summary>
    public ModbusIpTransport(ITransportConnection connection) : base(connection)
    {
    }

    /// <inheritdoc />
    public override void WriteMessage(ModbusMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var frame = BuildFrame(message);
        Connection.Write(frame, 0, frame.Length);
        Connection.Flush();
        OnMessageSent(message, frame);
    }

    /// <inheritdoc />
    public override ModbusResponse ReadResponse(ModbusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var header = ReadFullyOrThrow(ModbusConstants.MbapHeaderSize, "MBAP 报文头");
        var response = ParseFrame(header, request, out var frame);
        OnMessageReceived(response, frame);
        return response;
    }

    /// <summary>把报文封装为完整的 MBAP 帧。</summary>
    public static byte[] BuildFrame(ModbusMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var pdu = message.ToPdu();
        var frame = new byte[ModbusConstants.MbapHeaderSize + pdu.Length];

        frame[0] = (byte)((message.TransactionId >> 8) & 0xFF);
        frame[1] = (byte)(message.TransactionId & 0xFF);
        frame[2] = (byte)((ModbusConstants.TcpProtocolId >> 8) & 0xFF);
        frame[3] = (byte)(ModbusConstants.TcpProtocolId & 0xFF);

        int length = pdu.Length + 1;
        frame[4] = (byte)((length >> 8) & 0xFF);
        frame[5] = (byte)(length & 0xFF);
        frame[6] = (byte)message.UnitId;

        Array.Copy(pdu, 0, frame, ModbusConstants.MbapHeaderSize, pdu.Length);
        return frame;
    }

    private ModbusResponse ParseFrame(byte[] header, ModbusRequest request, out byte[] frame)
    {
        int transactionId = (header[0] << 8) | header[1];
        int protocolId = (header[2] << 8) | header[3];
        int length = (header[4] << 8) | header[5];
        int unitId = header[6];

        if (protocolId != ModbusConstants.TcpProtocolId)
        {
            throw new ModbusIOException($"协议标识非法：期望 0，实际 {protocolId}。");
        }

        if (length < 2 || length > ModbusConstants.MaxPduSize + 1)
        {
            throw new ModbusIOException($"MBAP 长度字段非法：{length}。");
        }

        int pduLength = length - 1;
        var pdu = ReadFullyOrThrow(pduLength, "PDU");

        frame = new byte[header.Length + pdu.Length];
        header.CopyTo(frame, 0);
        pdu.CopyTo(frame, header.Length);

        if (request.TransactionId != transactionId)
        {
            throw new ModbusIOException(
                $"事务标识不匹配：期望 {request.TransactionId}，实际 {transactionId}。");
        }

        var response = ModbusResponseFactory.FromPdu(pdu);
        response.TransactionId = transactionId;
        response.UnitId = unitId;
        return response;
    }
}

/// <summary>
/// Modbus TCP 传输层。对应 jamod 的 <c>net.wimpi.modbus.io.ModbusTCPTransport</c>。
/// </summary>
public sealed class ModbusTcpTransport : ModbusIpTransport
{
    /// <summary>以 TCP 连接构造传输层。</summary>
    public ModbusTcpTransport(TcpConnection connection) : base(connection)
    {
    }

    /// <summary>底层 TCP 连接。</summary>
    public TcpConnection TcpConnection => (TcpConnection)Connection;

    /// <summary>以远端地址构造传输层。</summary>
    public static ModbusTcpTransport Create(string host, int port = ModbusConstants.DefaultPort) =>
        new(new TcpConnection(host, port));
}

/// <summary>
/// Modbus UDP 传输层。对应 jamod 的 <c>net.wimpi.modbus.io.ModbusUDPTransport</c>。
/// </summary>
public sealed class ModbusUdpTransport : ModbusIpTransport
{
    /// <summary>以 UDP 连接构造传输层。</summary>
    public ModbusUdpTransport(UdpConnection connection) : base(connection)
    {
    }

    /// <summary>底层 UDP 连接。</summary>
    public UdpConnection UdpConnection => (UdpConnection)Connection;

    /// <summary>以远端地址构造传输层。</summary>
    public static ModbusUdpTransport Create(string host, int port = ModbusConstants.DefaultPort) =>
        new(new UdpConnection(host, port));
}
