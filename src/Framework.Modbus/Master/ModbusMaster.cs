using Framework.Modbus.IO;
using Framework.Modbus.Messages;
using Framework.Modbus.ProcessImage;
using Framework.Modbus.Util;

namespace Framework.Modbus.Master;

/// <summary>
/// Modbus 主站抽象：提供全套功能码的请求构造方法与便捷读写方法。
/// 对应 jamod 的 <c>net.wimpi.modbus.ModbusMaster</c>。
/// </summary>
/// <remarks>
/// 该层只负责"构造请求 → 执行事务 → 校验响应"，具体链路由 <see cref="IModbusTransport"/> 决定。
/// </remarks>
public abstract class ModbusMaster : IDisposable
{
    /// <summary>以传输层构造主站。</summary>
    protected ModbusMaster(IModbusTransport transport)
    {
        Transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    /// <summary>底层传输层。</summary>
    public IModbusTransport Transport { get; }

    /// <summary>默认从站地址。</summary>
    public int UnitId
    {
        get => Transport.UnitId;
        set => Transport.UnitId = value;
    }

    /// <summary>重试次数。</summary>
    public int Retries
    {
        get => Transport.Retries;
        set => Transport.Retries = value;
    }

    /// <summary>读写超时（毫秒）。</summary>
    public int Timeout
    {
        get => Transport.Timeout;
        set => Transport.Timeout = value;
    }

    /// <summary>当前事务标识。</summary>
    public int TransactionId
    {
        get => Transport.TransactionId;
        set => Transport.TransactionId = value;
    }

    /// <summary>连接是否已建立。</summary>
    public bool IsConnected => Transport.IsConnected;

    /// <summary>建立连接。</summary>
    public virtual void Connect() => Transport.Connect();

    /// <summary>关闭连接。</summary>
    public virtual void Close() => Transport.Close();

    /// <inheritdoc />
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        Close();
        Transport.Dispose();
    }

    // ------------------------------------------------------------------
    // 请求工厂（与 jamod 的 createXxxRequest 一一对应）
    // ------------------------------------------------------------------

    /// <summary>构造 FC01 读线圈请求。</summary>
    public virtual ReadCoilsRequest CreateReadCoilsRequest(int reference, int bitCount) =>
        Prepare(new ReadCoilsRequest(reference, bitCount));

    /// <summary>构造 FC02 读离散输入请求。</summary>
    public virtual ReadInputDiscretesRequest CreateReadInputDiscretesRequest(int reference, int bitCount) =>
        Prepare(new ReadInputDiscretesRequest(reference, bitCount));

    /// <summary>构造 FC03 读保持寄存器请求。</summary>
    public virtual ReadMultipleRegistersRequest CreateReadMultipleRegistersRequest(int reference, int wordCount) =>
        Prepare(new ReadMultipleRegistersRequest(reference, wordCount));

    /// <summary>构造 FC04 读输入寄存器请求。</summary>
    public virtual ReadInputRegistersRequest CreateReadInputRegistersRequest(int reference, int wordCount) =>
        Prepare(new ReadInputRegistersRequest(reference, wordCount));

    /// <summary>构造 FC05 写单个线圈请求。</summary>
    public virtual WriteCoilRequest CreateWriteCoilRequest(int reference, bool state) =>
        Prepare(new WriteCoilRequest(reference, state));

    /// <summary>构造 FC06 写单个寄存器请求。</summary>
    public virtual WriteRegisterRequest CreateWriteRegisterRequest(int reference, Register register) =>
        Prepare(new WriteRegisterRequest(reference, register));

    /// <summary>构造 FC07 读异常状态请求。</summary>
    public virtual ReadExceptionStatusRequest CreateReadExceptionStatusRequest() =>
        Prepare(new ReadExceptionStatusRequest());

    /// <summary>构造 FC08 诊断请求。</summary>
    public virtual DiagnosticsRequest CreateDiagnosticsRequest(int subFunction, int data) =>
        Prepare(new DiagnosticsRequest(subFunction, data));

    /// <summary>构造 FC0B 取通信事件计数器请求。</summary>
    public virtual GetCommEventCounterRequest CreateGetCommEventCounterRequest() =>
        Prepare(new GetCommEventCounterRequest());

    /// <summary>构造 FC0C 取通信事件记录请求。</summary>
    public virtual GetCommEventLogRequest CreateGetCommEventLogRequest() =>
        Prepare(new GetCommEventLogRequest());

    /// <summary>构造 FC0F 写多个线圈请求。</summary>
    public virtual WriteMultipleCoilsRequest CreateWriteMultipleCoilsRequest(int reference, BitVector coils) =>
        Prepare(new WriteMultipleCoilsRequest(reference, coils));

    /// <summary>构造 FC10 写多个寄存器请求。</summary>
    public virtual WriteMultipleRegistersRequest CreateWriteMultipleRegistersRequest(int reference, Register[] registers) =>
        Prepare(new WriteMultipleRegistersRequest(reference, registers));

    /// <summary>构造 FC11 报告从站标识请求。</summary>
    public virtual ReportSlaveIdRequest CreateReportSlaveIdRequest() =>
        Prepare(new ReportSlaveIdRequest());

    /// <summary>构造 FC14 读文件记录请求。</summary>
    public virtual ReadFileRecordRequest CreateReadFileRecordRequest(IEnumerable<FileRecordReference> records) =>
        Prepare(new ReadFileRecordRequest(records));

    /// <summary>构造 FC15 写文件记录请求。</summary>
    public virtual WriteFileRecordRequest CreateWriteFileRecordRequest(IEnumerable<FileRecord> fileRecords) =>
        Prepare(new WriteFileRecordRequest(fileRecords));

    /// <summary>构造 FC16 屏蔽写寄存器请求。</summary>
    public virtual MaskWriteRegisterRequest CreateMaskWriteRegisterRequest(int reference, int andMask, int orMask) =>
        Prepare(new MaskWriteRegisterRequest(reference, andMask, orMask));

    /// <summary>构造 FC17 读写多个寄存器请求。</summary>
    public virtual ReadWriteMultipleRegistersRequest CreateReadWriteMultipleRegistersRequest(
        int readReference,
        int readWordCount,
        int writeReference,
        Register[] writeRegisters) =>
        Prepare(new ReadWriteMultipleRegistersRequest(readReference, readWordCount, writeReference, writeRegisters));

    /// <summary>构造 FC18 读 FIFO 队列请求。</summary>
    public virtual ReadFifoQueueRequest CreateReadFifoQueueRequest(int reference) =>
        Prepare(new ReadFifoQueueRequest(reference));

    // ------------------------------------------------------------------
    // 事务执行
    // ------------------------------------------------------------------

    /// <summary>为当前传输层创建匹配的事务对象。</summary>
    public virtual IModbusTransaction CreateTransaction() => Transport switch
    {
        ModbusTcpTransport => new ModbusTcpTransaction(Transport),
        ModbusUdpTransport => new ModbusUdpTransaction(Transport),
        _ => new ModbusSerialTransaction(Transport),
    };

    /// <summary>发送请求并返回响应；从站返回异常响应时抛出 <see cref="SlaveException"/>。</summary>
    public virtual ModbusResponse Send(ModbusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var transaction = CreateTransaction();
        transaction.Request = request;
        transaction.Retries = Retries;
        transaction.Execute();
        return transaction.Response
            ?? throw new ModbusIOException("事务执行完毕但没有响应。");
    }

    /// <summary>发送请求并返回强类型响应。</summary>
    public TResponse Send<TResponse>(ModbusRequest request) where TResponse : ModbusResponse =>
        (TResponse)Send(request);

    // ------------------------------------------------------------------
    // 便捷读写（与 jamod 的 readCoils / writeCoil 等一一对应）
    // ------------------------------------------------------------------

    /// <summary>FC01 读线圈。</summary>
    public virtual BitVector ReadCoils(int reference, int bitCount) =>
        Send<ReadCoilsResponse>(CreateReadCoilsRequest(reference, bitCount)).Coils;

    /// <summary>FC02 读离散输入。</summary>
    public virtual BitVector ReadInputDiscretes(int reference, int bitCount) =>
        Send<ReadInputDiscretesResponse>(CreateReadInputDiscretesRequest(reference, bitCount)).Discretes;

    /// <summary>FC03 读保持寄存器。</summary>
    public virtual Register[] ReadMultipleRegisters(int reference, int wordCount) =>
        Send<ReadMultipleRegistersResponse>(CreateReadMultipleRegistersRequest(reference, wordCount)).Registers;

    /// <summary>FC03 读保持寄存器，返回原始值。</summary>
    public virtual int[] ReadMultipleRegisterValues(int reference, int wordCount) =>
        Send<ReadMultipleRegistersResponse>(CreateReadMultipleRegistersRequest(reference, wordCount)).Values;

    /// <summary>FC04 读输入寄存器。</summary>
    public virtual Register[] ReadInputRegisters(int reference, int wordCount) =>
        Send<ReadInputRegistersResponse>(CreateReadInputRegistersRequest(reference, wordCount)).Registers;

    /// <summary>FC04 读输入寄存器，返回原始值。</summary>
    public virtual int[] ReadInputRegisterValues(int reference, int wordCount) =>
        Send<ReadInputRegistersResponse>(CreateReadInputRegistersRequest(reference, wordCount)).Values;

    /// <summary>FC05 写单个线圈。</summary>
    public virtual void WriteCoil(int reference, bool state) =>
        Send(CreateWriteCoilRequest(reference, state));

    /// <summary>FC06 写单个寄存器。</summary>
    public virtual void WriteRegister(int reference, Register register) =>
        Send(CreateWriteRegisterRequest(reference, register));

    /// <summary>FC06 写单个寄存器（16 位整数）。</summary>
    public virtual void WriteRegister(int reference, int value) =>
        WriteRegister(reference, new SimpleRegister(value));

    /// <summary>FC0F 写多个线圈。</summary>
    public virtual void WriteMultipleCoils(int reference, BitVector coils) =>
        Send(CreateWriteMultipleCoilsRequest(reference, coils));

    /// <summary>FC10 写多个寄存器。</summary>
    public virtual void WriteMultipleRegisters(int reference, Register[] registers) =>
        Send(CreateWriteMultipleRegistersRequest(reference, registers));

    /// <summary>FC10 写多个寄存器（16 位整数数组）。</summary>
    public virtual void WriteMultipleRegisters(int reference, IEnumerable<int> values) =>
        Send(CreateWriteMultipleRegistersRequest(reference, values.Select(v => (Register)new SimpleRegister(v)).ToArray()));

    /// <summary>FC07 读异常状态。</summary>
    public virtual ReadExceptionStatusResponse ReadExceptionStatus() =>
        Send<ReadExceptionStatusResponse>(CreateReadExceptionStatusRequest());

    /// <summary>FC08 诊断，返回从站回显的数据字段。</summary>
    public virtual int Diagnostics(int subFunction, int data) =>
        Send<DiagnosticsResponse>(CreateDiagnosticsRequest(subFunction, data)).Data;

    /// <summary>FC0B 取通信事件计数器。</summary>
    public virtual GetCommEventCounterResponse GetCommEventCounter() =>
        Send<GetCommEventCounterResponse>(CreateGetCommEventCounterRequest());

    /// <summary>FC0C 取通信事件记录。</summary>
    public virtual GetCommEventLogResponse GetCommEventLog() =>
        Send<GetCommEventLogResponse>(CreateGetCommEventLogRequest());

    /// <summary>FC11 报告从站标识。</summary>
    public virtual ReportSlaveIdResponse ReportSlaveId() =>
        Send<ReportSlaveIdResponse>(CreateReportSlaveIdRequest());

    /// <summary>FC14 读文件记录。</summary>
    public virtual FileRecord[] ReadFileRecord(IEnumerable<FileRecordReference> records) =>
        Send<ReadFileRecordResponse>(CreateReadFileRecordRequest(records)).FileRecords;

    /// <summary>FC15 写文件记录。</summary>
    public virtual void WriteFileRecord(IEnumerable<FileRecord> fileRecords) =>
        Send(CreateWriteFileRecordRequest(fileRecords));

    /// <summary>FC16 屏蔽写寄存器。</summary>
    public virtual MaskWriteRegisterResponse MaskWriteRegister(int reference, int andMask, int orMask) =>
        Send<MaskWriteRegisterResponse>(CreateMaskWriteRegisterRequest(reference, andMask, orMask));

    /// <summary>FC17 读写多个寄存器。</summary>
    public virtual Register[] ReadWriteMultipleRegisters(
        int readReference,
        int readWordCount,
        int writeReference,
        Register[] writeRegisters) =>
        Send<ReadWriteMultipleRegistersResponse>(
            CreateReadWriteMultipleRegistersRequest(readReference, readWordCount, writeReference, writeRegisters))
        .Registers;

    /// <summary>FC18 读 FIFO 队列。</summary>
    public virtual int[] ReadFifoQueue(int reference) =>
        Send<ReadFifoQueueResponse>(CreateReadFifoQueueRequest(reference)).Values;

    /// <summary>为请求填充默认从站地址。</summary>
    protected TRequest Prepare<TRequest>(TRequest request) where TRequest : ModbusRequest
    {
        request.UnitId = UnitId;
        return request;
    }
}
