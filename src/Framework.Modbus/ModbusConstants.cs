namespace Framework.Modbus;

/// <summary>
/// Modbus 协议默认参数与尺寸约束。
/// 对应 jamod 的 <c>net.wimpi.modbus.Modbus</c> 常量类。
/// </summary>
public static class ModbusConstants
{
    /// <summary>默认从站地址（单元标识）。对应 jamod <c>Modbus.DEFAULT_UNIT_ID</c>。</summary>
    public const int DefaultUnitId = 1;

    /// <summary>默认事务标识（仅 TCP/UDP 有效）。对应 jamod <c>DEFAULT_TRANSACTION_ID</c>。</summary>
    public const int DefaultTransactionId = 0;

    /// <summary>默认重试次数。对应 jamod <c>DEFAULT_RETRIES</c>。</summary>
    public const int DefaultRetries = 3;

    /// <summary>默认读写超时（毫秒）。对应 jamod <c>DEFAULT_TIMEOUT</c>。</summary>
    public const int DefaultTimeout = 3000;

    /// <summary>默认发送间隔（毫秒），串口总线需要时可以拉大。</summary>
    public const int DefaultTransmitDelay = 0;

    /// <summary>Modbus TCP / UDP 默认端口。</summary>
    public const int DefaultPort = 502;

    /// <summary>PDU（功能码 + 数据）最大长度，Modbus 规范为 253 字节。</summary>
    public const int MaxPduSize = 253;

    /// <summary>串口帧最大长度（地址 + PDU + 校验）。</summary>
    public const int MaxSerialFrameSize = 256;

    /// <summary>Modbus TCP MBAP 报文头长度。</summary>
    public const int MbapHeaderSize = 7;

    /// <summary>MBAP 中的协议标识，Modbus 固定为 0。</summary>
    public const int TcpProtocolId = 0;

    /// <summary>FC01 单次最多读取的线圈数量。</summary>
    public const int MaxReadCoils = 2000;

    /// <summary>FC02 单次最多读取的离散输入数量。</summary>
    public const int MaxReadDiscretes = 2000;

    /// <summary>FC03 / FC04 单次最多读取的寄存器数量。</summary>
    public const int MaxReadRegisters = 125;

    /// <summary>FC15 单次最多写入的线圈数量。</summary>
    public const int MaxWriteCoils = 1968;

    /// <summary>FC16 单次最多写入的寄存器数量。</summary>
    public const int MaxWriteRegisters = 123;

    /// <summary>ASCII 帧起始字符。</summary>
    public const char AsciiStart = ':';

    /// <summary>ASCII 帧结束符。</summary>
    public const string AsciiEnd = "\r\n";
}

/// <summary>
/// Modbus 功能码。对应 jamod 的 <c>net.wimpi.modbus.Modbus.READ_COILS</c> 等常量。
/// </summary>
public static class ModbusFunctionCode
{
    /// <summary>读线圈（Read Coils），位操作。</summary>
    public const byte ReadCoils = 0x01;

    /// <summary>读离散输入（Read Input Discretes），位操作。</summary>
    public const byte ReadInputDiscretes = 0x02;

    /// <summary>读保持寄存器（Read Multiple Registers），字操作。</summary>
    public const byte ReadMultipleRegisters = 0x03;

    /// <summary>读输入寄存器（Read Input Registers），字操作。</summary>
    public const byte ReadInputRegisters = 0x04;

    /// <summary>写单个线圈（Write Coil），位操作。</summary>
    public const byte WriteCoil = 0x05;

    /// <summary>写单个寄存器（Write Single Register），字操作。</summary>
    public const byte WriteSingleRegister = 0x06;

    /// <summary>读异常状态（Read Exception Status），串行链路诊断。</summary>
    public const byte ReadExceptionStatus = 0x07;

    /// <summary>诊断（Diagnostics），串行链路诊断。</summary>
    public const byte Diagnostics = 0x08;

    /// <summary>取通信事件计数器（Get Comm Event Counter）。</summary>
    public const byte GetCommEventCounter = 0x0B;

    /// <summary>取通信事件记录（Get Comm Event Log）。</summary>
    public const byte GetCommEventLog = 0x0C;

    /// <summary>写多个线圈（Write Multiple Coils），位操作。</summary>
    public const byte WriteMultipleCoils = 0x0F;

    /// <summary>写多个寄存器（Write Multiple Registers），字操作。</summary>
    public const byte WriteMultipleRegisters = 0x10;

    /// <summary>报告从站标识（Report Slave Id）。</summary>
    public const byte ReportSlaveId = 0x11;

    /// <summary>读文件记录（Read File Record）。</summary>
    public const byte ReadFileRecord = 0x14;

    /// <summary>写文件记录（Write File Record）。</summary>
    public const byte WriteFileRecord = 0x15;

    /// <summary>屏蔽写寄存器（Mask Write Register）。</summary>
    public const byte MaskWriteRegister = 0x16;

    /// <summary>读写多个寄存器（Read/Write Multiple Registers）。</summary>
    public const byte ReadWriteMultipleRegisters = 0x17;

    /// <summary>读 FIFO 队列（Read FIFO Queue）。</summary>
    public const byte ReadFifoQueue = 0x18;

    /// <summary>异常响应功能码置位掩码：原功能码 | 0x80。</summary>
    public const byte ExceptionMask = 0x80;

    /// <summary>取功能码的可读名称。</summary>
    public static string GetName(byte functionCode) => functionCode switch
    {
        ReadCoils => "Read Coils",
        ReadInputDiscretes => "Read Input Discretes",
        ReadMultipleRegisters => "Read Multiple Registers",
        ReadInputRegisters => "Read Input Registers",
        WriteCoil => "Write Coil",
        WriteSingleRegister => "Write Single Register",
        ReadExceptionStatus => "Read Exception Status",
        Diagnostics => "Diagnostics",
        GetCommEventCounter => "Get Comm Event Counter",
        GetCommEventLog => "Get Comm Event Log",
        WriteMultipleCoils => "Write Multiple Coils",
        WriteMultipleRegisters => "Write Multiple Registers",
        ReportSlaveId => "Report Slave Id",
        ReadFileRecord => "Read File Record",
        WriteFileRecord => "Write File Record",
        MaskWriteRegister => "Mask Write Register",
        ReadWriteMultipleRegisters => "Read/Write Multiple Registers",
        ReadFifoQueue => "Read FIFO Queue",
        _ => (functionCode & ExceptionMask) != 0
            ? $"Exception Response (0x{functionCode:X2})"
            : $"Unknown Function Code (0x{functionCode:X2})",
    };
}

/// <summary>
/// Modbus 异常码。对应 jamod 的 <c>net.wimpi.modbus.SlaveException</c> 内部常量表。
/// </summary>
public static class ModbusExceptionCode
{
    /// <summary>非法功能。</summary>
    public const byte IllegalFunction = 0x01;

    /// <summary>非法数据地址。</summary>
    public const byte IllegalDataAddress = 0x02;

    /// <summary>非法数据值。</summary>
    public const byte IllegalDataValue = 0x03;

    /// <summary>从站设备故障。</summary>
    public const byte SlaveDeviceFailure = 0x04;

    /// <summary>确认（已接受，正在处理）。</summary>
    public const byte Acknowledge = 0x05;

    /// <summary>从站设备忙。</summary>
    public const byte SlaveDeviceBusy = 0x06;

    /// <summary>内存奇偶校验错误。</summary>
    public const byte MemoryParityError = 0x08;

    /// <summary>网关路径不可用。</summary>
    public const byte GatewayPathUnavailable = 0x0A;

    /// <summary>网关目标设备无响应。</summary>
    public const byte GatewayTargetDeviceFailedToRespond = 0x0B;

    /// <summary>取异常码的可读描述。</summary>
    public static string GetMessage(byte exceptionCode) => exceptionCode switch
    {
        IllegalFunction => "Illegal function",
        IllegalDataAddress => "Illegal data address",
        IllegalDataValue => "Illegal data value",
        SlaveDeviceFailure => "Slave device failure",
        Acknowledge => "Acknowledge",
        SlaveDeviceBusy => "Slave device busy",
        MemoryParityError => "Memory parity error",
        GatewayPathUnavailable => "Gateway path unavailable",
        GatewayTargetDeviceFailedToRespond => "Gateway target device failed to respond",
        _ => "Unknown exception code",
    };
}

/// <summary>FC08 Diagnostics 的子功能码常量。</summary>
public static class ModbusDiagnosticsSubFunction
{
    /// <summary>返回查询数据（回显）。</summary>
    public const int ReturnQueryData = 0x0000;

    /// <summary>重启通信选项。</summary>
    public const int RestartCommunicationsOption = 0x0001;

    /// <summary>返回诊断寄存器。</summary>
    public const int ReturnDiagnosticRegister = 0x0002;

    /// <summary>修改 ASCII 输入分隔符。</summary>
    public const int ChangeAsciiInputDelimiter = 0x0003;

    /// <summary>强制进入只听模式。</summary>
    public const int ForceListenOnlyMode = 0x0004;

    /// <summary>清空计数器与诊断寄存器。</summary>
    public const int ClearCountersAndDiagnosticRegister = 0x000A;

    /// <summary>返回总线消息计数。</summary>
    public const int ReturnBusMessageCount = 0x000B;

    /// <summary>返回总线通信错误计数。</summary>
    public const int ReturnBusCommunicationErrorCount = 0x000C;

    /// <summary>返回总线异常错误计数。</summary>
    public const int ReturnBusExceptionErrorCount = 0x000D;

    /// <summary>返回从站消息计数。</summary>
    public const int ReturnSlaveMessageCount = 0x000E;

    /// <summary>返回从站无响应计数。</summary>
    public const int ReturnSlaveNoResponseCount = 0x000F;

    /// <summary>返回从站 NAK 计数。</summary>
    public const int ReturnSlaveNakCount = 0x0010;

    /// <summary>返回从站忙计数。</summary>
    public const int ReturnSlaveBusyCount = 0x0011;

    /// <summary>返回总线字符溢出计数。</summary>
    public const int ReturnBusCharacterOverrunCount = 0x0012;

    /// <summary>清空溢出计数器与标志。</summary>
    public const int ClearOverrunCounterAndFlag = 0x0014;
}
