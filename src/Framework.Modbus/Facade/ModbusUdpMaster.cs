using Framework.Modbus.IO;
using Framework.Modbus.Net;

namespace Framework.Modbus.Facade;

/// <summary>
/// Modbus UDP 主站。对应 jamod 的 <c>net.wimpi.modbus.facade.ModbusUDPMaster</c>。
/// </summary>
public class ModbusUdpMaster : ModbusMasterFacade
{
    /// <summary>以远端地址构造 UDP 主站。</summary>
    public ModbusUdpMaster(
        string host,
        int port = ModbusConstants.DefaultPort,
        bool reconnect = true,
        int timeout = ModbusConstants.DefaultTimeout)
        : base(new ModbusUdpTransport(new UdpConnection(host, port)))
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

    /// <summary>底层 UDP 连接。</summary>
    public UdpConnection Connection => ((ModbusUdpTransport)Transport).UdpConnection;
}
