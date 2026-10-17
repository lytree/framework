using System.Net;
using System.Net.Sockets;

namespace Framework.Modbus.Net;

/// <summary>
/// Modbus UDP 连接。对应 jamod 的 <c>net.wimpi.modbus.net.UDPMasterConnection</c>。
/// </summary>
public sealed class UdpConnection : ITransportConnection
{
    private readonly string _host;
    private readonly int _port;
    private UdpClient? _client;
    private IPEndPoint? _remote;

    private byte[] _pending = Array.Empty<byte>();
    private int _pendingOffset;
    private int _pendingCount;

    /// <summary>以主机与端口构造连接（尚未建立）。</summary>
    public UdpConnection(string host, int port = ModbusConstants.DefaultPort)
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
    public bool IsConnected => _client is not null;

    /// <inheritdoc />
    public int ReadTimeout { get; set; } = ModbusConstants.DefaultTimeout;

    /// <inheritdoc />
    public int WriteTimeout { get; set; } = ModbusConstants.DefaultTimeout;

    /// <inheritdoc />
    public int Available => _pendingCount;

    /// <inheritdoc />
    public void Connect()
    {
        if (IsConnected)
        {
            return;
        }

        try
        {
            var addresses = Dns.GetHostAddresses(_host);
            var address = Array.Find(addresses, a => a.AddressFamily == AddressFamily.InterNetwork)
                ?? addresses.FirstOrDefault()
                ?? throw new ConnectionException($"无法解析主机 {_host}。");

            _remote = new IPEndPoint(address, _port);
            _client = new UdpClient(address.AddressFamily);
            _client.Connect(_remote);
            ApplyTimeouts();
        }
        catch (ConnectionException)
        {
            Close();
            throw;
        }
        catch (Exception ex)
        {
            Close();
            throw new ConnectionException($"无法建立到 {_host}:{_port} 的 UDP 通道：{ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public void Close()
    {
        try
        {
            _client?.Dispose();
        }
        catch (SocketException)
        {
            // 关闭阶段的异常可以忽略。
        }

        _client = null;
        _remote = null;
        _pending = Array.Empty<byte>();
        _pendingOffset = 0;
        _pendingCount = 0;
    }

    /// <inheritdoc />
    public int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var client = _client ?? throw new ConnectionException("UDP 通道尚未建立。");
        ApplyTimeouts();

        if (_pendingCount <= 0)
        {
            try
            {
                var endpoint = _remote!;
                _pending = client.Receive(ref endpoint);
                _pendingOffset = 0;
                _pendingCount = _pending.Length;
            }
            catch (SocketException ex) when (ex.SocketErrorCode is SocketError.TimedOut or SocketError.WouldBlock)
            {
                return 0;
            }
            catch (ObjectDisposedException)
            {
                return 0;
            }
        }

        int toCopy = Math.Min(count, _pendingCount);
        Array.Copy(_pending, _pendingOffset, buffer, offset, toCopy);
        _pendingOffset += toCopy;
        _pendingCount -= toCopy;
        return toCopy;
    }

    /// <inheritdoc />
    public void Write(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var client = _client ?? throw new ConnectionException("UDP 通道尚未建立。");
        ApplyTimeouts();

        try
        {
            client.Client.Send(buffer, offset, count, SocketFlags.None);
        }
        catch (SocketException ex)
        {
            throw new ModbusIOException($"发送 UDP 数据失败：{ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        // UDP 无发送缓冲需要刷新。
    }

    /// <inheritdoc />
    public void DiscardBuffers()
    {
        _pending = Array.Empty<byte>();
        _pendingOffset = 0;
        _pendingCount = 0;
    }

    /// <inheritdoc />
    public void Dispose() => Close();

    private void ApplyTimeouts()
    {
        if (_client is null)
        {
            return;
        }

        _client.Client.ReceiveTimeout = Math.Max(ReadTimeout, 1);
        _client.Client.SendTimeout = Math.Max(WriteTimeout, 1);
    }
}
