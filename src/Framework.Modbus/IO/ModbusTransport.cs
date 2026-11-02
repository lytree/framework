using Framework.Modbus.Messages;
using Framework.Modbus.Net;
using Framework.Modbus.Util;

namespace Framework.Modbus.IO;

/// <summary>
/// 传输层公共实现：连接管理、重试、超时与事件。
/// 对应 jamod 的 <c>net.wimpi.modbus.io.ModbusTransport</c>。
/// </summary>
public abstract class ModbusTransport : IModbusTransport
{
    private int _timeout = ModbusConstants.DefaultTimeout;
    private int _transactionId = ModbusConstants.DefaultTransactionId;
    private PooledByteBuf? _sendBuffer;
    private PooledByteBuf? _receiveBuffer;

    /// <summary>以底层连接构造传输层。</summary>
    protected ModbusTransport(ITransportConnection connection)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <summary>底层字节流连接。</summary>
    protected ITransportConnection Connection { get; }

    /// <summary>连接是否建立失败时自动重连。</summary>
    public bool AutoReconnect { get; set; } = true;

    /// <inheritdoc />
    public int Timeout
    {
        get => _timeout;
        set
        {
            _timeout = value;
            Connection.ReadTimeout = value;
            Connection.WriteTimeout = value;
        }
    }

    /// <inheritdoc />
    public int Retries { get; set; } = ModbusConstants.DefaultRetries;

    /// <inheritdoc />
    public int UnitId { get; set; } = ModbusConstants.DefaultUnitId;

    /// <inheritdoc />
    public int TransactionId
    {
        get => _transactionId;
        set => _transactionId = value;
    }

    /// <inheritdoc />
    public virtual bool IsConnected => Connection.IsConnected;

    /// <inheritdoc />
    public event EventHandler<ModbusTransportEventArgs>? MessageSent;

    /// <inheritdoc />
    public event EventHandler<ModbusTransportEventArgs>? MessageReceived;

    /// <inheritdoc />
    public virtual void Connect()
    {
        Connection.ReadTimeout = _timeout;
        Connection.WriteTimeout = _timeout;
        Connection.Connect();
    }

    /// <inheritdoc />
    public virtual void Close() => Connection.Close();

    /// <inheritdoc />
    public virtual void Flush() => Connection.Flush();

    /// <inheritdoc />
    public abstract void WriteMessage(ModbusMessage message);

    /// <inheritdoc />
    public abstract ModbusResponse ReadResponse(ModbusRequest request);

    /// <inheritdoc />
    public virtual ModbusResponse Send(ModbusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureConnected();

        request.UnitId = UnitId;
        request.TransactionId = _transactionId;

        int attempts = Math.Max(Retries, 1);
        ModbusException? lastError = null;

        for (int attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                WriteMessage(request);
                var response = ReadResponse(request);
                return response;
            }
            catch (ModbusException ex) when (attempt < attempts)
            {
                lastError = ex;
                Connection.DiscardBuffers();

                if (AutoReconnect && !Connection.IsConnected)
                {
                    TryReconnect();
                }
            }
        }

        throw lastError ?? new ModbusIOException("请求失败：未获得有效响应。");
    }

    /// <inheritdoc />
    public int GetNextTransactionId()
    {
        _transactionId = (_transactionId + 1) & 0xFFFF;
        return _transactionId;
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        _sendBuffer?.Dispose();
        _receiveBuffer?.Dispose();
        _sendBuffer = null;
        _receiveBuffer = null;
        Connection.Dispose();
    }

    /// <summary>是否有 <see cref="MessageSent"/> 订阅者；无订阅者时可跳过物化线序字节。</summary>
    protected bool HasMessageSentSubscribers => MessageSent is not null;

    /// <summary>是否有 <see cref="MessageReceived"/> 订阅者；无订阅者时可跳过物化线序字节。</summary>
    protected bool HasMessageReceivedSubscribers => MessageReceived is not null;

    /// <summary>
    /// 发送缓冲：按传输层实例复用，避免每帧 <c>new byte[]</c>。
    /// 仅在真正用到时才向内存池租借。
    /// </summary>
    private protected PooledByteBuf SendBuffer =>
        _sendBuffer ??= new PooledByteBuf(ModbusConstants.MbapHeaderSize + ModbusConstants.MaxPduSize);

    /// <summary>接收缓冲：语义同 <see cref="SendBuffer"/>。</summary>
    private protected PooledByteBuf ReceiveBuffer =>
        _receiveBuffer ??= new PooledByteBuf(ModbusConstants.MbapHeaderSize + ModbusConstants.MaxPduSize);

    /// <summary>触发 <see cref="MessageSent"/>。</summary>
    protected void OnMessageSent(ModbusMessage message, byte[] data) =>
        MessageSent?.Invoke(this, new ModbusTransportEventArgs(message, data));

    /// <summary>触发 <see cref="MessageReceived"/>。</summary>
    protected void OnMessageReceived(ModbusMessage message, byte[] data) =>
        MessageReceived?.Invoke(this, new ModbusTransportEventArgs(message, data));

    /// <summary>确保连接可用。</summary>
    protected void EnsureConnected()
    {
        if (!Connection.IsConnected)
        {
            Connect();
        }
    }

    /// <summary>
    /// 读取恰好 <paramref name="count"/> 个字节。
    /// 采用逐段"字节间隔超时"：每成功读到一段就重置截止时间；整段超时且一个字节都没读到时返回 null。
    /// </summary>
    protected byte[]? ReadFully(int count)
    {
        if (count == 0)
        {
            return Array.Empty<byte>();
        }

        var buffer = new byte[count];
        int total = 0;
        long deadline = Environment.TickCount64 + Math.Max(_timeout, 1);

        while (total < count)
        {
            int read = Connection.Read(buffer, total, count - total);
            if (read > 0)
            {
                total += read;
                deadline = Environment.TickCount64 + Math.Max(_timeout, 1);
                continue;
            }

            if (Environment.TickCount64 >= deadline)
            {
                if (total == 0)
                {
                    return null;
                }

                throw new ModbusIOException($"读取超时：期望 {count} 字节，实际只收到 {total} 字节。");
            }
        }

        return buffer;
    }

    /// <summary>读取恰好 <paramref name="count"/> 个字节，超时抛异常。</summary>
    protected byte[] ReadFullyOrThrow(int count, string what) =>
        ReadFully(count) ?? throw new ModbusIOException($"读取{what}超时（{_timeout} ms）。");

    /// <summary>
    /// 读取恰好 <paramref name="count"/> 个字节并追加到 <paramref name="buffer"/>，
    /// 超时抛 <see cref="ModbusIOException"/>。全程不分配新数组。
    /// </summary>
    /// <remarks>
    /// 超时语义与 <see cref="ReadFully"/> 一致：每读到一段就重置截止时间，
    /// 因此半帧数据持续到达时不会误判超时。
    /// </remarks>
    private protected void ReadInto(PooledByteBuf buffer, int count, string what)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (count <= 0)
        {
            return;
        }

        int total = 0;
        long deadline = Environment.TickCount64 + Math.Max(_timeout, 1);

        while (total < count)
        {
            // 必须把可读窗口限制在本段剩余字节内：否则底层流会一次读进下一帧的数据，
            // 后续按帧长补齐时就再也等不到字节（表现为"读取 PDU 超时"）。
            int remaining = count - total;
            int read = Connection.Read(buffer.GetSpan(remaining)[..remaining]);
            if (read > 0)
            {
                buffer.Advance(read);
                total += read;
                deadline = Environment.TickCount64 + Math.Max(_timeout, 1);
                continue;
            }

            if (Environment.TickCount64 >= deadline)
            {
                throw new ModbusIOException(
                    total == 0
                        ? $"读取{what}超时（{_timeout} ms）。"
                        : $"读取{what}超时：期望 {count} 字节，实际只收到 {total} 字节。");
            }
        }
    }

    private void TryReconnect()
    {
        try
        {
            Connection.Close();
            Connect();
        }
        catch (ModbusException)
        {
            // 重连失败时交给下一次尝试或最终抛出。
        }
    }
}
