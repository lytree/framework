using System.Buffers;
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

        var buffer = SendBuffer;
        buffer.Clear();
        WriteFrame(message, buffer);

        // 直接写入底层流的 Span 重载，不再为每帧分配数组
        Connection.Write(buffer.WrittenSpan());
        Connection.Flush();

        // 无订阅者时不物化线序字节
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
        ReadInto(buffer, ModbusConstants.MbapHeaderSize, "MBAP 报文头");

        // 头部已就位，按长度字段补齐剩余部分（长度字段 = 单元标识 1 字节 + PDU）
        int length = (buffer.WrittenSpan()[4] << 8) | buffer.WrittenSpan()[5];
        if (length < 2 || length > ModbusConstants.MaxPduSize + 1)
        {
            throw new ModbusIOException($"MBAP 长度字段非法：{length}。");
        }

        ReadInto(buffer, length - 1, "PDU");

        var response = ParseFrame(buffer.WrittenSpan(), request);

        if (HasMessageReceivedSubscribers)
        {
            OnMessageReceived(response, buffer.ToFullArray());
        }

        return response;
    }

    /// <summary>把报文封装为完整的 MBAP 帧。</summary>
    public static byte[] BuildFrame(ModbusMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        using var buffer = new PooledByteBuf(ModbusConstants.MbapHeaderSize + ModbusConstants.MaxPduSize);
        WriteFrame(message, buffer);
        return buffer.ToFullArray();
    }

    /// <summary>把报文按 MBAP 帧格式写入 <paramref name="buffer"/>，全程不分配新数组。</summary>
    /// <remarks>
    /// MBAP 的长度字段位于帧头、却要等 PDU 写完后才知道，因此先占位再回填，
    /// 避免"先编码 PDU 成数组 → 再拷进帧缓冲"的中间分配。
    /// </remarks>
    private static void WriteFrame(ModbusMessage message, PooledByteBuf buffer)
    {
        int start = buffer.WriterIndex;

        // 占位 MBAP 头，确保后续 Advance 不会越界
        buffer.GetSpan(ModbusConstants.MbapHeaderSize);
        buffer.Advance(ModbusConstants.MbapHeaderSize);

        message.WritePdu(buffer);

        int pduLength = buffer.WriterIndex - start - ModbusConstants.MbapHeaderSize;
        int length = pduLength + 1;

        buffer.SetByte(start, (byte)((message.TransactionId >> 8) & 0xFF));
        buffer.SetByte(start + 1, (byte)(message.TransactionId & 0xFF));
        buffer.SetByte(start + 2, (byte)((ModbusConstants.TcpProtocolId >> 8) & 0xFF));
        buffer.SetByte(start + 3, (byte)(ModbusConstants.TcpProtocolId & 0xFF));
        buffer.SetByte(start + 4, (byte)((length >> 8) & 0xFF));
        buffer.SetByte(start + 5, (byte)(length & 0xFF));
        buffer.SetByte(start + 6, (byte)message.UnitId);
    }

    private static ModbusResponse ParseFrame(ReadOnlySpan<byte> frame, ModbusRequest request)
    {
        int transactionId = (frame[0] << 8) | frame[1];
        int protocolId = (frame[2] << 8) | frame[3];
        int unitId = frame[6];

        if (protocolId != ModbusConstants.TcpProtocolId)
        {
            throw new ModbusIOException($"协议标识非法：期望 0，实际 {protocolId}。");
        }

        if (request.TransactionId != transactionId)
        {
            throw new ModbusIOException(
                $"事务标识不匹配：期望 {request.TransactionId}，实际 {transactionId}。");
        }

        var response = ModbusResponseFactory.FromPdu(frame[ModbusConstants.MbapHeaderSize..].ToArray());
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
