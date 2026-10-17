using System.IO.Ports;
using Framework.Modbus.IO;
using Framework.Modbus.Net;
using Framework.Modbus.Util;

namespace Framework.Modbus.Facade;

/// <summary>
/// Modbus 串口主站（RTU / ASCII 由 <see cref="SerialParameters.Encoding"/> 决定）。
/// 对应 jamod 的 <c>net.wimpi.modbus.facade.ModbusSerialMaster</c>。
/// </summary>
public class ModbusSerialMaster : ModbusMasterFacade
{
    /// <summary>以串口参数构造主站。</summary>
    public ModbusSerialMaster(SerialParameters parameters)
        : base(CreateTransport(parameters))
    {
        Parameters = parameters;
        Transport.AutoReconnect = parameters.AutoReconnect;
        Timeout = parameters.Timeout;
    }

    /// <summary>串口参数。</summary>
    public SerialParameters Parameters { get; }

    /// <summary>底层串口传输层。</summary>
    public ModbusSerialTransport SerialTransport => (ModbusSerialTransport)Transport;

    /// <summary>创建 RTU 主站。</summary>
    public static ModbusSerialMaster CreateRtu(
        string portName,
        int baudRate = 9600,
        int dataBits = 8,
        Parity parity = Parity.None,
        StopBits stopBits = StopBits.One) =>
        new(SerialParameters.CreateRtu(portName, baudRate, dataBits, parity, stopBits));

    /// <summary>创建 ASCII 主站。</summary>
    public static ModbusSerialMaster CreateAscii(
        string portName,
        int baudRate = 9600,
        int dataBits = 7,
        Parity parity = Parity.Even,
        StopBits stopBits = StopBits.One) =>
        new(SerialParameters.CreateAscii(portName, baudRate, dataBits, parity, stopBits));

    private static ModbusSerialTransport CreateTransport(SerialParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var connection = new SerialConnection(parameters);
        return parameters.Encoding == SerialEncoding.Ascii
            ? new ModbusAsciiTransport(connection)
            : new ModbusRtuTransport(connection);
    }
}
