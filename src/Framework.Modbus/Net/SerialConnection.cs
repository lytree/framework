using System.IO.Ports;
using Framework.Modbus.Util;

namespace Framework.Modbus.Net;

/// <summary>
/// 串口连接（RTU / ASCII 共用）。对应 jamod 的 <c>net.wimpi.modbus.net.SerialConnection</c>。
/// </summary>
public sealed class SerialConnection : ITransportConnection
{
    private readonly SerialParameters _parameters;
    private SerialPort? _port;

    /// <summary>以串口参数构造连接（尚未打开）。</summary>
    public SerialConnection(SerialParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        _parameters = parameters;
        ReadTimeout = parameters.Timeout;
        WriteTimeout = parameters.Timeout;
    }

    /// <summary>串口参数。</summary>
    public SerialParameters Parameters => _parameters;

    /// <inheritdoc />
    public bool IsConnected => _port?.IsOpen == true;

    /// <inheritdoc />
    public int ReadTimeout { get; set; }

    /// <inheritdoc />
    public int WriteTimeout { get; set; }

    /// <inheritdoc />
    public int Available
    {
        get
        {
            try
            {
                return _port?.BytesToRead ?? 0;
            }
            catch (InvalidOperationException)
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

        try
        {
            var port = new SerialPort(
                _parameters.PortName,
                _parameters.BaudRate,
                _parameters.Parity,
                _parameters.DataBits,
                _parameters.StopBits)
            {
                Handshake = _parameters.Handshake,
                ReadTimeout = Math.Max(ReadTimeout, 1),
                WriteTimeout = Math.Max(WriteTimeout, 1),
                DtrEnable = _parameters.DtrEnable,
                RtsEnable = _parameters.RtsEnable,
            };

            port.Open();
            _port = port;
        }
        catch (Exception ex)
        {
            Close();
            throw new ConnectionException($"无法打开串口 {_parameters.PortName}：{ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public void Close()
    {
        try
        {
            if (_port?.IsOpen == true)
            {
                _port.Close();
            }

            _port?.Dispose();
        }
        catch (IOException)
        {
            // 关闭阶段的异常可以忽略。
        }
        catch (InvalidOperationException)
        {
            // 同上。
        }

        _port = null;
    }

    /// <inheritdoc />
    public int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var port = _port ?? throw new ConnectionException("串口尚未打开。");
        port.ReadTimeout = Math.Max(ReadTimeout, 1);

        try
        {
            return port.Read(buffer, offset, count);
        }
        catch (TimeoutException)
        {
            return 0;
        }
        catch (InvalidOperationException)
        {
            return 0;
        }
    }

    /// <inheritdoc />
    public void Write(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var port = _port ?? throw new ConnectionException("串口尚未打开。");
        port.WriteTimeout = Math.Max(WriteTimeout, 1);

        try
        {
            port.Write(buffer, offset, count);
        }
        catch (TimeoutException ex)
        {
            throw new ModbusIOException($"串口写入超时（{WriteTimeout} ms）。", ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new ModbusIOException($"串口写入失败：{ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        try
        {
            _port?.BaseStream.Flush();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or NotSupportedException)
        {
            // 串口流刷新失败不影响协议流程。
        }
    }

    /// <inheritdoc />
    public void DiscardBuffers()
    {
        try
        {
            if (_port?.IsOpen == true)
            {
                _port.DiscardInBuffer();
                _port.DiscardOutBuffer();
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            // 部分驱动不支持 Discard，忽略。
        }
    }

    /// <inheritdoc />
    public void Dispose() => Close();
}
