using System.Net.Sockets;

namespace Framework.Modbus.Net;

/// <summary>
/// Modbus TCP 连接。对应 jamod 的 <c>net.wimpi.modbus.net.TCPConnection</c>。
/// </summary>
public sealed class TcpConnection : ITransportConnection
{
    private readonly string _host;
    private readonly int _port;
    private TcpClient? _client;
    private NetworkStream? _stream;

    /// <summary>以主机与端口构造连接（尚未建立）。</summary>
    public TcpConnection(string host, int port = ModbusConstants.DefaultPort)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), port, "端口必须落在 [1, 65535]。");
        }

        _host = host;
        _port = port;
    }

    /// <summary>远端主机。</summary>
    public string Host => _host;

    /// <summary>远端端口。</summary>
    public int Port => _port;

    /// <inheritdoc />
    public bool IsConnected => _client?.Connected == true && _stream is not null;

    /// <inheritdoc />
    public int ReadTimeout { get; set; } = ModbusConstants.DefaultTimeout;

    /// <inheritdoc />
    public int WriteTimeout { get; set; } = ModbusConstants.DefaultTimeout;

    /// <inheritdoc />
    public int Available => _client?.Available ?? 0;

    /// <inheritdoc />
    public void Connect()
    {
        if (IsConnected)
        {
            return;
        }

        Close();

        int timeout = Math.Max(ReadTimeout, 1000);
        var client = new TcpClient
        {
            NoDelay = true,
        };

        try
        {
            var connectTask = client.ConnectAsync(_host, _port);
            if (!connectTask.Wait(timeout))
            {
                client.Dispose();
                throw new ConnectionException($"连接 {_host}:{_port} 超时（{timeout} ms）。");
            }

            connectTask.GetAwaiter().GetResult();
        }
        catch (ConnectionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            client.Dispose();
            var reason = ex is AggregateException aggregate ? aggregate.GetBaseException() : ex;
            throw new ConnectionException($"无法连接 {_host}:{_port}：{reason.Message}", reason);
        }

        _client = client;
        _stream = client.GetStream();
        _stream.ReadTimeout = ReadTimeout;
        _stream.WriteTimeout = WriteTimeout;
    }

    /// <inheritdoc />
    public void Close()
    {
        try
        {
            _stream?.Dispose();
        }
        catch (IOException)
        {
            // 关闭阶段的异常可以忽略。
        }

        try
        {
            _client?.Dispose();
        }
        catch (SocketException)
        {
            // 同上。
        }

        _stream = null;
        _client = null;
    }

    /// <inheritdoc />
    public int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var stream = _stream ?? throw new ConnectionException("TCP 连接尚未建立。");
        stream.ReadTimeout = ReadTimeout;

        try
        {
            return stream.Read(buffer, offset, count);
        }
        catch (IOException ex) when (IsTimeout(ex))
        {
            return 0;
        }
        catch (ObjectDisposedException)
        {
            return 0;
        }
    }

    /// <inheritdoc />
    public void Write(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var stream = _stream ?? throw new ConnectionException("TCP 连接尚未建立。");
        stream.WriteTimeout = WriteTimeout;

        try
        {
            stream.Write(buffer, offset, count);
        }
        catch (IOException ex)
        {
            throw new ModbusIOException($"发送数据失败：{ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        try
        {
            _stream?.Flush();
        }
        catch (IOException)
        {
            // 流不支持 Flush 时忽略。
        }
    }

    /// <inheritdoc />
    public void DiscardBuffers()
    {
        // TCP 由协议栈管理缓冲，无需额外处理。
    }

    /// <inheritdoc />
    public void Dispose() => Close();

    private static bool IsTimeout(IOException ex) =>
        ex.InnerException is SocketException { SocketErrorCode: SocketError.TimedOut };
}
