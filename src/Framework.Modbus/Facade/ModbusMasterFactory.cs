using System.IO.Ports;
using Framework.Modbus.Util;

namespace Framework.Modbus.Facade;

/// <summary>主站类型。</summary>
public enum ModbusMasterType
{
    /// <summary>Modbus TCP。</summary>
    Tcp,

    /// <summary>Modbus UDP。</summary>
    Udp,

    /// <summary>串口 RTU。</summary>
    Rtu,

    /// <summary>串口 ASCII。</summary>
    Ascii,
}

/// <summary>
/// 主站工厂。对应 jamod 的 <c>net.wimpi.modbus.facade.ModbusMasterFactory</c>。
/// </summary>
public static class ModbusMasterFactory
{
    /// <summary>创建 Modbus TCP 主站。</summary>
    public static ModbusTcpMaster CreateTcpMaster(
        string host,
        int port = ModbusConstants.DefaultPort,
        bool reconnect = true,
        int timeout = ModbusConstants.DefaultTimeout) =>
        new(host, port, reconnect, timeout);

    /// <summary>创建 Modbus UDP 主站。</summary>
    public static ModbusUdpMaster CreateUdpMaster(
        string host,
        int port = ModbusConstants.DefaultPort,
        bool reconnect = true,
        int timeout = ModbusConstants.DefaultTimeout) =>
        new(host, port, reconnect, timeout);

    /// <summary>以串口参数创建 RTU 主站。</summary>
    public static ModbusSerialMaster CreateRtuMaster(SerialParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Encoding = SerialEncoding.Rtu;
        return new ModbusSerialMaster(parameters);
    }

    /// <summary>以串口参数创建 ASCII 主站。</summary>
    public static ModbusSerialMaster CreateAsciiMaster(SerialParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Encoding = SerialEncoding.Ascii;
        return new ModbusSerialMaster(parameters);
    }

    /// <summary>以端口名创建 RTU 主站。</summary>
    public static ModbusSerialMaster CreateRtuMaster(
        string portName,
        int baudRate = 9600,
        int dataBits = 8,
        Parity parity = Parity.None,
        StopBits stopBits = StopBits.One) =>
        ModbusSerialMaster.CreateRtu(portName, baudRate, dataBits, parity, stopBits);

    /// <summary>以端口名创建 ASCII 主站。</summary>
    public static ModbusSerialMaster CreateAsciiMaster(
        string portName,
        int baudRate = 9600,
        int dataBits = 7,
        Parity parity = Parity.Even,
        StopBits stopBits = StopBits.One) =>
        ModbusSerialMaster.CreateAscii(portName, baudRate, dataBits, parity, stopBits);

    /// <summary>按类型创建主站。</summary>
    /// <param name="type">主站类型。</param>
    /// <param name="address">TCP / UDP 为主机名，RTU / ASCII 为端口名。</param>
    /// <param name="port">TCP / UDP 端口。</param>
    public static ModbusMasterFacade CreateMaster(ModbusMasterType type, string address, int port = ModbusConstants.DefaultPort) =>
        type switch
        {
            ModbusMasterType.Tcp => CreateTcpMaster(address, port),
            ModbusMasterType.Udp => CreateUdpMaster(address, port),
            ModbusMasterType.Rtu => CreateRtuMaster(address),
            ModbusMasterType.Ascii => CreateAsciiMaster(address),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "未知的主站类型。"),
        };
}
