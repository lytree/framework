using Framework.Modbus.Messages;

namespace Framework.Modbus;

/// <summary>
/// Modbus 相关异常的基类。对应 jamod 的 <c>net.wimpi.modbus.ModbusException</c>。
/// </summary>
public class ModbusException : Exception
{
    /// <summary>构造异常。</summary>
    public ModbusException()
    {
    }

    /// <summary>以指定消息构造异常。</summary>
    public ModbusException(string message) : base(message)
    {
    }

    /// <summary>以指定消息与内部异常构造异常。</summary>
    public ModbusException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// 通信 I/O 异常：超时、帧损坏、长度不合法、响应功能码不匹配等。
/// 对应 jamod 的 <c>net.wimpi.modbus.ModbusIOException</c>。
/// </summary>
public class ModbusIOException : ModbusException
{
    /// <summary>构造异常。</summary>
    public ModbusIOException()
    {
    }

    /// <summary>以指定消息构造异常。</summary>
    public ModbusIOException(string message) : base(message)
    {
    }

    /// <summary>以指定消息与内部异常构造异常。</summary>
    public ModbusIOException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// 连接建立 / 关闭失败。对应 jamod 的 <c>net.wimpi.modbus.ConnectionException</c>。
/// </summary>
public class ConnectionException : ModbusException
{
    /// <summary>构造异常。</summary>
    public ConnectionException()
    {
    }

    /// <summary>以指定消息构造异常。</summary>
    public ConnectionException(string message) : base(message)
    {
    }

    /// <summary>以指定消息与内部异常构造异常。</summary>
    public ConnectionException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// 从站返回异常响应（功能码最高位置 1）。
/// 对应 jamod 的 <c>net.wimpi.modbus.SlaveException</c>。
/// </summary>
public class SlaveException : ModbusException
{
    /// <summary>原始请求功能码。</summary>
    public byte FunctionCode { get; }

    /// <summary>从站返回的异常码。</summary>
    public byte ExceptionCode { get; }

    /// <summary>以功能码与异常码构造异常。</summary>
    public SlaveException(byte functionCode, byte exceptionCode)
        : base(BuildMessage(functionCode, exceptionCode))
    {
        FunctionCode = functionCode;
        ExceptionCode = exceptionCode;
    }

    /// <summary>以异常响应报文构造异常。</summary>
    public SlaveException(ExceptionResponse response)
        : this(response.OriginalFunctionCode, response.ExceptionCode)
    {
        Response = response;
    }

    /// <summary>触发本异常的异常响应报文（若可用）。</summary>
    public ExceptionResponse? Response { get; }

    private static string BuildMessage(byte functionCode, byte exceptionCode) =>
        $"Slave returned an exception for function code 0x{functionCode:X2} " +
        $"({ModbusFunctionCode.GetName(functionCode)}): " +
        $"0x{exceptionCode:X2} ({ModbusExceptionCode.GetMessage(exceptionCode)}).";
}
