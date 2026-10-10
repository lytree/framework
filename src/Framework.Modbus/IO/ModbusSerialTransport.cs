using Framework.Modbus.Messages;
using Framework.Modbus.Net;
using Framework.Modbus.Util;

namespace Framework.Modbus.IO;

/// <summary>
/// 串行传输层公共实现（RTU / ASCII）。
/// 对应 jamod 的 <c>net.wimpi.modbus.io.ModbusSerialTransport</c>。
/// </summary>
public abstract class ModbusSerialTransport : ModbusTransport
{
    /// <summary>以串口连接与参数构造传输层。</summary>
    protected ModbusSerialTransport(ITransportConnection connection, SerialParameters parameters)
        : base(connection)
    {
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
    }

    /// <summary>串口参数。</summary>
    public SerialParameters Parameters { get; }

    /// <summary>上一次发送的完整帧长度（含地址与校验），用于回显丢弃。</summary>
    protected int LastSentFrameLength { get; private set; }

    /// <summary>记录刚发送的帧长度。</summary>
    protected void RecordSentFrame(int length) => LastSentFrameLength = length;

    /// <summary>发送前的总线清理与静默间隔。</summary>
    protected void PrepareToSend()
    {
        Connection.DiscardBuffers();
        if (Parameters.TransmitDelay > 0)
        {
            Thread.Sleep(Parameters.TransmitDelay);
        }
    }

    /// <summary>读取 1 字节，超时抛异常。</summary>
    protected int ReadByteOrThrow(string what)
    {
        var buffer = ReadFully(1) ?? throw new ModbusIOException($"读取{what}超时（{Timeout} ms）。");
        return buffer[0];
    }
}
