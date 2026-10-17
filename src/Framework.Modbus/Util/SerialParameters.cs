using System.IO.Ports;

namespace Framework.Modbus.Util;

/// <summary>串行链路的编码方式。</summary>
public enum SerialEncoding
{
    /// <summary>RTU：二进制 + CRC16。</summary>
    Rtu,

    /// <summary>ASCII：可打印十六进制字符 + LRC。</summary>
    Ascii,
}

/// <summary>
/// 串口参数集合。对应 jamod 的 <c>net.wimpi.modbus.util.SerialParameters</c>。
/// </summary>
public sealed class SerialParameters
{
    /// <summary>端口名，例如 <c>COM3</c>、<c>/dev/ttyUSB0</c>。</summary>
    public string PortName { get; set; } = "COM1";

    /// <summary>波特率。</summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>数据位。</summary>
    public int DataBits { get; set; } = 8;

    /// <summary>校验位。</summary>
    public Parity Parity { get; set; } = Parity.None;

    /// <summary>停止位。</summary>
    public StopBits StopBits { get; set; } = StopBits.One;

    /// <summary>编码方式（RTU / ASCII）。</summary>
    public SerialEncoding Encoding { get; set; } = SerialEncoding.Rtu;

    /// <summary>是否回显（RS-232 转 RS-485 之类的半双工链路需要丢弃回显字节）。</summary>
    public bool Echo { get; set; }

    /// <summary>读写超时（毫秒）。</summary>
    public int Timeout { get; set; } = ModbusConstants.DefaultTimeout;

    /// <summary>字符间隔超时（毫秒），RTU 判帧使用。</summary>
    public int InterCharacterTimeout { get; set; } = 50;

    /// <summary>发送前的静默间隔（毫秒），用于 RTU 的 3.5 字符时间近似。</summary>
    public int TransmitDelay { get; set; }

    /// <summary>流控输入。</summary>
    public Handshake Handshake { get; set; } = Handshake.None;

    /// <summary>是否在连接时自动重连（由上层 Master 使用）。</summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>是否启用 DTR。</summary>
    public bool DtrEnable { get; set; }

    /// <summary>是否启用 RTS。</summary>
    public bool RtsEnable { get; set; }

    /// <summary>创建一个 9600-8-N-1 的默认参数。</summary>
    public static SerialParameters CreateDefault(string portName = "COM1") => new()
    {
        PortName = portName,
    };

    /// <summary>创建 RTU 参数。</summary>
    public static SerialParameters CreateRtu(
        string portName,
        int baudRate = 9600,
        int dataBits = 8,
        Parity parity = Parity.None,
        StopBits stopBits = StopBits.One)
    {
        return new SerialParameters
        {
            PortName = portName,
            BaudRate = baudRate,
            DataBits = dataBits,
            Parity = parity,
            StopBits = stopBits,
            Encoding = SerialEncoding.Rtu,
        };
    }

    /// <summary>创建 ASCII 参数（默认 7 数据位 + 偶校验）。</summary>
    public static SerialParameters CreateAscii(
        string portName,
        int baudRate = 9600,
        int dataBits = 7,
        Parity parity = Parity.Even,
        StopBits stopBits = StopBits.One)
    {
        return new SerialParameters
        {
            PortName = portName,
            BaudRate = baudRate,
            DataBits = dataBits,
            Parity = parity,
            StopBits = stopBits,
            Encoding = SerialEncoding.Ascii,
        };
    }

    /// <summary>参数的可读描述。</summary>
    public override string ToString() =>
        $"{PortName} {BaudRate}-{DataBits}-{Parity}-{StopBits} {Encoding}";
}
