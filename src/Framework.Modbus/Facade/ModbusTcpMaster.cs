using Framework.Modbus.IO;
using Framework.Modbus.Net;

namespace Framework.Modbus.Facade;

/// <summary>
/// Modbus TCP 主站。对应 jamod 的 <c>net.wimpi.modbus.facade.ModbusTCPMaster</c>。
/// </summary>
public class ModbusTcpMaster : ModbusMasterFacade
{
    /// <summary>以远端地址构造 TCP 主站。</summary>
    /// <param name="host">从站 / 网关的 IP 或主机名。</param>
    /// <param name="port">端口，默认 502。</param>
    /// <param name="reconnect">链路中断时是否自动重连。</param>
    /// <param name="timeout">读写超时（毫秒）。</param>
    public ModbusTcpMaster(
        string host,
        int port = ModbusConstants.DefaultPort,
        bool reconnect = true,
        int timeout = ModbusConstants.DefaultTimeout)
        : base(new ModbusTcpTransport(new TcpConnection(host, port)))
    {
        Host = host;
        Port = port;
        Transport.AutoReconnect = reconnect;
        Timeout = timeout;
    }

    /// <summary>从站主机。</summary>
    public string Host { get; }

    /// <summary>从站端口。</summary>
    public int Port { get; }

    /// <summary>底层 TCP 连接。</summary>
    public TcpConnection Connection => ((ModbusTcpTransport)Transport).TcpConnection;
}
