using System.Net;
using System.Net.Sockets;

namespace Framework.Modbus.Net;

/// <summary>
/// Modbus TCP 连接。对应 jamod 的 <c>net.wimpi.modbus.net.TCPConnection</c>。
/// </summary>
/// <remarks>
/// 直接基于 <see cref="Socket"/> 实现，连接阶段使用 <c>BeginConnect</c> + 等待句柄获得可控超时，
/// 避免"同步等待异步任务"在大并发下耗尽线程池。
/// </remarks>
public sealed class TcpConnection : ITransportConnection
{
    private readonly string _host;
    private readonly int _port;
    private Socket? _socket;
    private NetworkStream? _stream;
    private volatile bool _connected;

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
    /// <remarks>
    /// 自行维护连接状态，而不依赖 <see cref="Socket.Connected"/>：
    /// 后者反映"上一次 I/O 的结果"，一次读超时也会让它变为 false，从而误判链路已断开。
    /// </remarks>
    public bool IsConnected => _connected && _stream is not null;

    /// <inheritdoc />
    public int ReadTimeout { get; set; } = ModbusConstants.DefaultTimeout;

    /// <inheritdoc />
    public int WriteTimeout { get; set; } = ModbusConstants.DefaultTimeout;

    /// <inheritdoc />
    public int Available
    {
        get
        {
            try
            {
                return _socket?.Available ?? 0;
            }
            catch (SocketException)
            {
                return 0;
            }
            catch (ObjectDisposedException)
            {
                return 0;
            }
        }
    }

    /// <inheritdoc />
    public void Connect()
    {
        if (IsConnected)
        {
            return;
        }

        Close();

        int timeout = Math.Max(ReadTimeout, 1000);
        Socket? socket = null;

        try
        {
            socket = ConnectSocket(_host, _port, timeout);
            _socket = socket;
            _stream = new NetworkStream(socket, ownsSocket: true)
            {
                ReadTimeout = ReadTimeout,
                WriteTimeout = WriteTimeout,
            };
            _connected = true;
        }
        catch (ConnectionException)
        {
            socket?.Dispose();
            throw;
        }
        catch (Exception ex)
        {
            socket?.Dispose();
            throw new ConnectionException($"无法连接 {_host}:{_port}：{ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public void Close()
    {
        _connected = false;

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
            _socket?.Dispose();
        }
        catch (SocketException)
        {
            // 同上。
        }

        _stream = null;
        _socket = null;
    }

    /// <inheritdoc />
    public int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var stream = _stream ?? throw new ConnectionException("TCP 连接尚未建立。");
        stream.ReadTimeout = ReadTimeout;

        try
        {
            int read = stream.Read(buffer, offset, count);
            if (read == 0 && count > 0)
            {
                // 对端已关闭连接。
                _connected = false;
            }

            return read;
        }
        catch (IOException ex) when (IsTimeout(ex))
        {
            // 读超时不代表链路断开。
            return 0;
        }
        catch (IOException)
        {
            _connected = false;
            return 0;
        }
        catch (ObjectDisposedException)
        {
            _connected = false;
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
            _connected = false;
            throw new ModbusIOException($"发送数据失败：{ex.Message}", ex);
        }
        catch (ObjectDisposedException ex)
        {
            _connected = false;
            throw new ModbusIOException("发送数据失败：连接已关闭。", ex);
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

    private static Socket ConnectSocket(string host, int port, int timeout)
    {
        IPAddress[] addresses;
        try
        {
            addresses = Dns.GetHostAddresses(host);
        }
        catch (SocketException ex)
        {
            throw new ConnectionException($"无法解析主机 {host}：{ex.Message}", ex);
        }

        var address = Array.Find(addresses, a => a.AddressFamily == AddressFamily.InterNetwork)
            ?? addresses.FirstOrDefault()
            ?? throw new ConnectionException($"无法解析主机 {host}。");

        var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true,
        };

        try
        {
            var pending = socket.BeginConnect(address, port, null, null);
            if (!pending.AsyncWaitHandle.WaitOne(timeout))
            {
                throw new ConnectionException($"连接 {host}:{port} 超时（{timeout} ms）。");
            }

            socket.EndConnect(pending);
            return socket;
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static bool IsTimeout(IOException ex) =>
        ex.InnerException is SocketException { SocketErrorCode: SocketError.TimedOut };
}
