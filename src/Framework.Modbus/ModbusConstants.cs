using Abstractions = Framework.Modbus.Abstractions;

namespace Framework.Modbus;

/// <summary>
/// Modbus 协议默认参数与尺寸约束。
/// 对应 jamod 的 <c>net.wimpi.modbus.Modbus</c> 常量类。
/// </summary>
/// <remarks>
/// 本类型保留为公开兼容层，取值统一转发到协议契约层 <see cref="Abstractions.ModbusConstants"/>，
/// 避免主站与从站各自维护一份常量。
/// </remarks>
public static class ModbusConstants
{
    /// <summary>默认从站地址（单元标识）。对应 jamod <c>Modbus.DEFAULT_UNIT_ID</c>。</summary>
    public const int DefaultUnitId = Abstractions.ModbusConstants.DefaultUnitId;

    /// <summary>默认事务标识（仅 TCP/UDP 有效）。对应 jamod <c>DEFAULT_TRANSACTION_ID</c>。</summary>
    public const int DefaultTransactionId = Abstractions.ModbusConstants.DefaultTransactionId;

    /// <summary>默认重试次数。对应 jamod <c>DEFAULT_RETRIES</c>。</summary>
    public const int DefaultRetries = Abstractions.ModbusConstants.DefaultRetries;

    /// <summary>默认读写超时（毫秒）。对应 jamod <c>DEFAULT_TIMEOUT</c>。</summary>
    public const int DefaultTimeout = Abstractions.ModbusConstants.DefaultTimeout;

    /// <summary>默认发送间隔（毫秒），串口总线需要时可以拉大。</summary>
    public const int DefaultTransmitDelay = Abstractions.ModbusConstants.DefaultTransmitDelay;

    /// <summary>Modbus TCP / UDP 默认端口。</summary>
    public const int DefaultPort = Abstractions.ModbusConstants.DefaultPort;

    /// <summary>PDU（功能码 + 数据）最大长度，Modbus 规范为 253 字节。</summary>
    public const int MaxPduSize = Abstractions.ModbusConstants.MaxPduSize;

    /// <summary>串口帧最大长度（地址 + PDU + 校验）。</summary>
    public const int MaxSerialFrameSize = Abstractions.ModbusConstants.MaxSerialFrameSize;

    /// <summary>Modbus TCP MBAP 报文头长度。</summary>
    public const int MbapHeaderSize = Abstractions.ModbusConstants.MbapHeaderSize;

    /// <summary>MBAP 中的协议标识，Modbus 固定为 0。</summary>
    public const int TcpProtocolId = Abstractions.ModbusConstants.TcpProtocolId;

    /// <summary>FC01 单次最多读取的线圈数量。</summary>
    public const int MaxReadCoils = Abstractions.ModbusConstants.MaxReadCoils;

    /// <summary>FC02 单次最多读取的离散输入数量。</summary>
    public const int MaxReadDiscretes = Abstractions.ModbusConstants.MaxReadDiscretes;

    /// <summary>FC03 / FC04 单次最多读取的寄存器数量。</summary>
    public const int MaxReadRegisters = Abstractions.ModbusConstants.MaxReadRegisters;

    /// <summary>FC15 单次最多写入的线圈数量。</summary>
    public const int MaxWriteCoils = Abstractions.ModbusConstants.MaxWriteCoils;

    /// <summary>FC16 单次最多写入的寄存器数量。</summary>
    public const int MaxWriteRegisters = Abstractions.ModbusConstants.MaxWriteRegisters;

    /// <summary>ASCII 帧起始字符。</summary>
    public const char AsciiStart = Abstractions.ModbusConstants.AsciiStart;

    /// <summary>ASCII 帧结束符。</summary>
    public const string AsciiEnd = Abstractions.ModbusConstants.AsciiEnd;
}

/// <summary>
/// Modbus 功能码。对应 jamod 的 <c>net.wimpi.modbus.Modbus.READ_COILS</c> 等常量。
/// </summary>
/// <remarks>
/// 取值统一来自 <see cref="Abstractions.ModbusFunctionCode"/>；本类型仅保留 jamod 命名以兼容既有调用方。
/// </remarks>
public static class ModbusFunctionCode
{
    /// <summary>读线圈（Read Coils），位操作。</summary>
    public const byte ReadCoils = Abstractions.ModbusFunctionCode.ReadCoils;

    /// <summary>读离散输入（Read Input Discretes），位操作。</summary>
    public const byte ReadInputDiscretes = Abstractions.ModbusFunctionCode.ReadInputDiscretes;

    /// <summary>读保持寄存器（Read Multiple Registers），字操作。</summary>
    public const byte ReadMultipleRegisters = Abstractions.ModbusFunctionCode.ReadMultipleRegisters;

    /// <summary>读输入寄存器（Read Input Registers），字操作。</summary>
    public const byte ReadInputRegisters = Abstractions.ModbusFunctionCode.ReadInputRegisters;

    /// <summary>写单个线圈（Write Coil），位操作。</summary>
    public const byte WriteCoil = Abstractions.ModbusFunctionCode.WriteCoil;

    /// <summary>写单个寄存器（Write Single Register），字操作。</summary>
    public const byte WriteSingleRegister = Abstractions.ModbusFunctionCode.WriteSingleRegister;

    /// <summary>读异常状态（Read Exception Status），串行链路诊断。</summary>
    public const byte ReadExceptionStatus = Abstractions.ModbusFunctionCode.ReadExceptionStatus;

    /// <summary>诊断（Diagnostics），串行链路诊断。</summary>
    public const byte Diagnostics = Abstractions.ModbusFunctionCode.Diagnostics;

    /// <summary>取通信事件计数器（Get Comm Event Counter）。</summary>
    public const byte GetCommEventCounter = Abstractions.ModbusFunctionCode.GetCommEventCounter;

    /// <summary>取通信事件记录（Get Comm Event Log）。</summary>
    public const byte GetCommEventLog = Abstractions.ModbusFunctionCode.GetCommEventLog;

    /// <summary>写多个线圈（Write Multiple Coils），位操作。</summary>
    public const byte WriteMultipleCoils = Abstractions.ModbusFunctionCode.WriteMultipleCoils;

    /// <summary>写多个寄存器（Write Multiple Registers），字操作。</summary>
    public const byte WriteMultipleRegisters = Abstractions.ModbusFunctionCode.WriteMultipleRegisters;

    /// <summary>报告从站标识（Report Slave Id）。</summary>
    public const byte ReportSlaveId = Abstractions.ModbusFunctionCode.ReportSlaveId;

    /// <summary>读文件记录（Read File Record）。</summary>
    public const byte ReadFileRecord = Abstractions.ModbusFunctionCode.ReadFileRecord;

    /// <summary>写文件记录（Write File Record）。</summary>
    public const byte WriteFileRecord = Abstractions.ModbusFunctionCode.WriteFileRecord;

    /// <summary>屏蔽写寄存器（Mask Write Register）。</summary>
    public const byte MaskWriteRegister = Abstractions.ModbusFunctionCode.MaskWriteRegister;

    /// <summary>读写多个寄存器（Read/Write Multiple Registers）。</summary>
    public const byte ReadWriteMultipleRegisters = Abstractions.ModbusFunctionCode.ReadWriteMultipleRegisters;

    /// <summary>读 FIFO 队列（Read FIFO Queue）。</summary>
    public const byte ReadFifoQueue = Abstractions.ModbusFunctionCode.ReadFifoQueue;

    /// <summary>异常响应功能码置位掩码：原功能码 | 0x80。</summary>
    public const byte ExceptionMask = Abstractions.ModbusFunctionCode.ExceptionMask;

    /// <summary>取功能码的可读名称。</summary>
    /// <param name="functionCode">功能码。</param>
    public static string GetName(byte functionCode) => Abstractions.ModbusFunctionCode.GetName(functionCode);
}

/// <summary>
/// Modbus 异常码。对应 jamod 的 <c>net.wimpi.modbus.SlaveException</c> 内部常量表。
/// </summary>
/// <remarks>
/// 取值统一来自 <see cref="Abstractions.ModbusExceptionCode"/>；本类型保留为公开兼容层。
/// </remarks>
public static class ModbusExceptionCode
{
    /// <summary>非法功能。</summary>
    public const byte IllegalFunction = Abstractions.ModbusExceptionCode.IllegalFunction;

    /// <summary>非法数据地址。</summary>
    public const byte IllegalDataAddress = Abstractions.ModbusExceptionCode.IllegalDataAddress;

    /// <summary>非法数据值。</summary>
    public const byte IllegalDataValue = Abstractions.ModbusExceptionCode.IllegalDataValue;

    /// <summary>从站设备故障。</summary>
    public const byte SlaveDeviceFailure = Abstractions.ModbusExceptionCode.SlaveDeviceFailure;

    /// <summary>确认（已接受，正在处理）。</summary>
    public const byte Acknowledge = Abstractions.ModbusExceptionCode.Acknowledge;

    /// <summary>从站设备忙。</summary>
    public const byte SlaveDeviceBusy = Abstractions.ModbusExceptionCode.SlaveDeviceBusy;

    /// <summary>内存奇偶校验错误。</summary>
    public const byte MemoryParityError = Abstractions.ModbusExceptionCode.MemoryParityError;

    /// <summary>网关路径不可用。</summary>
    public const byte GatewayPathUnavailable = Abstractions.ModbusExceptionCode.GatewayPathUnavailable;

    /// <summary>网关目标设备无响应。</summary>
    public const byte GatewayTargetDeviceFailedToRespond = Abstractions.ModbusExceptionCode.GatewayTargetDeviceFailedToRespond;

    /// <summary>取异常码的可读描述。</summary>
    /// <param name="exceptionCode">异常码。</param>
    public static string GetMessage(byte exceptionCode) => Abstractions.ModbusExceptionCode.GetMessage(exceptionCode);
}

/// <summary>
/// FC08 Diagnostics 的子功能码常量。
/// 保留为公开兼容层，取值统一转发到协议契约层 <see cref="Abstractions.ModbusDiagnosticsSubFunction"/>。
/// </summary>
public static class ModbusDiagnosticsSubFunction
{
    /// <summary>返回查询数据（回显）。</summary>
    public const int ReturnQueryData = Abstractions.ModbusDiagnosticsSubFunction.ReturnQueryData;

    /// <summary>重启通信选项。</summary>
    public const int RestartCommunicationsOption = Abstractions.ModbusDiagnosticsSubFunction.RestartCommunicationsOption;

    /// <summary>返回诊断寄存器。</summary>
    public const int ReturnDiagnosticRegister = Abstractions.ModbusDiagnosticsSubFunction.ReturnDiagnosticRegister;

    /// <summary>修改 ASCII 输入分隔符。</summary>
    public const int ChangeAsciiInputDelimiter = Abstractions.ModbusDiagnosticsSubFunction.ChangeAsciiInputDelimiter;

    /// <summary>强制进入只听模式。</summary>
    public const int ForceListenOnlyMode = Abstractions.ModbusDiagnosticsSubFunction.ForceListenOnlyMode;

    /// <summary>清空计数器与诊断寄存器。</summary>
    public const int ClearCountersAndDiagnosticRegister = Abstractions.ModbusDiagnosticsSubFunction.ClearCountersAndDiagnosticRegister;

    /// <summary>返回总线消息计数。</summary>
    public const int ReturnBusMessageCount = Abstractions.ModbusDiagnosticsSubFunction.ReturnBusMessageCount;

    /// <summary>返回总线通信错误计数。</summary>
    public const int ReturnBusCommunicationErrorCount = Abstractions.ModbusDiagnosticsSubFunction.ReturnBusCommunicationErrorCount;

    /// <summary>返回总线异常错误计数。</summary>
    public const int ReturnBusExceptionErrorCount = Abstractions.ModbusDiagnosticsSubFunction.ReturnBusExceptionErrorCount;

    /// <summary>返回从站消息计数。</summary>
    public const int ReturnSlaveMessageCount = Abstractions.ModbusDiagnosticsSubFunction.ReturnSlaveMessageCount;

    /// <summary>返回从站无响应计数。</summary>
    public const int ReturnSlaveNoResponseCount = Abstractions.ModbusDiagnosticsSubFunction.ReturnSlaveNoResponseCount;

    /// <summary>返回从站 NAK 计数。</summary>
    public const int ReturnSlaveNakCount = Abstractions.ModbusDiagnosticsSubFunction.ReturnSlaveNakCount;

    /// <summary>返回从站忙计数。</summary>
    public const int ReturnSlaveBusyCount = Abstractions.ModbusDiagnosticsSubFunction.ReturnSlaveBusyCount;

    /// <summary>返回总线字符溢出计数。</summary>
    public const int ReturnBusCharacterOverrunCount = Abstractions.ModbusDiagnosticsSubFunction.ReturnBusCharacterOverrunCount;

    /// <summary>清空溢出计数器与标志。</summary>
    public const int ClearOverrunCounterAndFlag = Abstractions.ModbusDiagnosticsSubFunction.ClearOverrunCounterAndFlag;
}
