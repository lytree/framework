using Framework.Modbus.Messages;

namespace Framework.Modbus.IO;

/// <summary>
/// Modbus 事务：一次"发送请求 → 读取响应"的完整交互。
/// 对应 jamod 的 <c>net.wimpi.modbus.io.ModbusTransaction</c>。
/// </summary>
public interface IModbusTransaction
{
    /// <summary>事务请求。</summary>
    ModbusRequest? Request { get; set; }

    /// <summary>事务响应（<see cref="Execute"/> 之后可用）。</summary>
    ModbusResponse? Response { get; }

    /// <summary>重试次数。</summary>
    int Retries { get; set; }

    /// <summary>发送前的静默间隔（毫秒）。</summary>
    int TransmitDelay { get; set; }

    /// <summary>本次事务的事务标识。</summary>
    int Id { get; }

    /// <summary>执行事务；从站返回异常响应时抛出 <see cref="SlaveException"/>。</summary>
    void Execute();
}

/// <summary>事务公共实现。</summary>
public abstract class ModbusTransaction : IModbusTransaction
{
    /// <summary>以传输层构造事务。</summary>
    protected ModbusTransaction(IModbusTransport transport)
    {
        Transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    /// <summary>底层传输层。</summary>
    protected IModbusTransport Transport { get; }

    /// <inheritdoc />
    public ModbusRequest? Request { get; set; }

    /// <inheritdoc />
    public ModbusResponse? Response { get; protected set; }

    /// <inheritdoc />
    public int TransmitDelay { get; set; }

    /// <inheritdoc />
    public int Id { get; protected set; }

    /// <inheritdoc />
    public int Retries
    {
        get => Transport.Retries;
        set => Transport.Retries = value;
    }

    /// <inheritdoc />
    public virtual void Execute()
    {
        var request = Request ?? throw new InvalidOperationException("尚未设置事务请求。");
        Response = null;

        if (TransmitDelay > 0)
        {
            Thread.Sleep(TransmitDelay);
        }

        var response = Transport.Send(request);
        Response = response;
        Id = request.TransactionId;

        if (response.IsException)
        {
            throw new SlaveException((ExceptionResponse)response);
        }
    }
}

/// <summary>
/// Modbus TCP 事务：每次执行前分配新的递增事务标识。
/// 对应 jamod 的 <c>net.wimpi.modbus.io.ModbusTCPTransaction</c>。
/// </summary>
public sealed class ModbusTcpTransaction : ModbusTransaction
{
    /// <summary>以传输层构造事务。</summary>
    public ModbusTcpTransaction(IModbusTransport transport) : base(transport)
    {
    }

    /// <inheritdoc />
    public override void Execute()
    {
        if (Request is not null)
        {
            Request.TransactionId = Transport.GetNextTransactionId();
        }

        base.Execute();
    }
}

/// <summary>Modbus UDP 事务。对应 jamod 的 <c>net.wimpi.modbus.io.ModbusUDPTransaction</c>。</summary>
public sealed class ModbusUdpTransaction : ModbusTransaction
{
    /// <summary>以传输层构造事务。</summary>
    public ModbusUdpTransaction(IModbusTransport transport) : base(transport)
    {
    }

    /// <inheritdoc />
    public override void Execute()
    {
        if (Request is not null)
        {
            Request.TransactionId = Transport.GetNextTransactionId();
        }

        base.Execute();
    }
}

/// <summary>
/// Modbus 串口事务（RTU / ASCII）。
/// 对应 jamod 的 <c>net.wimpi.modbus.io.ModbusSerialTransaction</c>。
/// </summary>
public sealed class ModbusSerialTransaction : ModbusTransaction
{
    /// <summary>以传输层构造事务。</summary>
    public ModbusSerialTransaction(IModbusTransport transport) : base(transport)
    {
    }
}
