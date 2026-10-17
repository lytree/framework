namespace Framework.Modbus.IO;

/// <summary>报文收发事件的参数。</summary>
public sealed class ModbusTransportEventArgs : EventArgs
{
    /// <summary>以报文与线序字节构造事件参数。</summary>
    public ModbusTransportEventArgs(Messages.ModbusMessage message, byte[] data)
    {
        Message = message;
        Data = data;
    }

    /// <summary>相关报文。</summary>
    public Messages.ModbusMessage Message { get; }

    /// <summary>实际收发到线路上的字节。</summary>
    public byte[] Data { get; }

    /// <summary>线序字节的十六进制表示。</summary>
    public string Hex => Util.ModbusUtil.Dump(Data);
}

/// <summary>
/// Modbus 传输层抽象。对应 jamod 的 <c>net.wimpi.modbus.io.ModbusTransport</c>。
/// </summary>
public interface IModbusTransport : IDisposable
{
    /// <summary>读写超时（毫秒）。</summary>
    int Timeout { get; set; }

    /// <summary>失败重试次数（含首次尝试）。</summary>
    int Retries { get; set; }

    /// <summary>默认从站地址。</summary>
    int UnitId { get; set; }

    /// <summary>当前事务标识。</summary>
    int TransactionId { get; set; }

    /// <summary>连接是否已建立。</summary>
    bool IsConnected { get; }

    /// <summary>连接断开时是否自动重连。</summary>
    bool AutoReconnect { get; set; }

    /// <summary>报文发送后触发。</summary>
    event EventHandler<ModbusTransportEventArgs>? MessageSent;

    /// <summary>报文接收后触发。</summary>
    event EventHandler<ModbusTransportEventArgs>? MessageReceived;

    /// <summary>建立连接（幂等）。</summary>
    void Connect();

    /// <summary>关闭连接（幂等）。</summary>
    void Close();

    /// <summary>把报文写入链路（不含读响应）。</summary>
    void WriteMessage(Messages.ModbusMessage message);

    /// <summary>读取 <paramref name="request"/> 对应的响应。</summary>
    Messages.ModbusResponse ReadResponse(Messages.ModbusRequest request);

    /// <summary>发送请求并读取响应（含重试）。</summary>
    Messages.ModbusResponse Send(Messages.ModbusRequest request);

    /// <summary>刷新发送缓冲。</summary>
    void Flush();

    /// <summary>取得下一个事务标识。</summary>
    int GetNextTransactionId();
}
