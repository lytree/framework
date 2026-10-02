using Framework.Modbus.Util;

namespace Framework.Modbus.Messages;

/// <summary>
/// 所有 Modbus 报文的基类。承载单元标识与事务标识，并负责 PDU 的序列化。
/// 对应 jamod 的 <c>net.wimpi.modbus.msg.ModbusMessage</c>。
/// </summary>
public abstract class ModbusMessage
{
    /// <summary>从站地址（单元标识）。</summary>
    public int UnitId { get; set; } = ModbusConstants.DefaultUnitId;

    /// <summary>事务标识（仅 TCP / UDP 使用）。</summary>
    public int TransactionId { get; set; } = ModbusConstants.DefaultTransactionId;

    /// <summary>功能码。</summary>
    public abstract byte FunctionCode { get; }

    /// <summary>是否为异常响应（功能码最高位置 1）。</summary>
    public virtual bool IsException => (FunctionCode & ModbusFunctionCode.ExceptionMask) != 0;

    /// <summary>把报文序列化为 PDU（功能码 + 数据）。</summary>
    public abstract byte[] ToPdu();

    /// <summary>PDU 长度（功能码 + 数据）。</summary>
    public int PduLength => ToPdu().Length;

    /// <inheritdoc />
    public override string ToString() =>
        $"{ModbusFunctionCode.GetName(FunctionCode)}: {ModbusUtil.Dump(ToPdu())}";
}

/// <summary>
/// 请求报文基类。对应 jamod 的 <c>net.wimpi.modbus.msg.ModbusRequest</c>。
/// </summary>
public abstract class ModbusRequest : ModbusMessage
{
    /// <summary>
    /// 期望的响应 PDU 长度（含功能码）。
    /// 串口传输层据此确定需要读取的字节数；返回 -1 表示长度需要读取响应中的字节计数。
    /// </summary>
    public abstract int ExpectedResponsePduLength { get; }
}

/// <summary>
/// 响应报文基类。对应 jamod 的 <c>net.wimpi.modbus.msg.ModbusResponse</c>。
/// </summary>
public abstract class ModbusResponse : ModbusMessage
{
}

/// <summary>
/// 异常响应：功能码为 <c>原功能码 | 0x80</c>，随后跟 1 字节异常码。
/// 对应 jamod 的 <c>net.wimpi.modbus.msg.ExceptionResponse</c>。
/// </summary>
public sealed class ExceptionResponse : ModbusResponse
{
    /// <summary>以原始功能码与异常码构造。</summary>
    public ExceptionResponse(byte originalFunctionCode, byte exceptionCode)
    {
        OriginalFunctionCode = originalFunctionCode;
        ExceptionCode = exceptionCode;
    }

    /// <summary>被拒绝的原始请求功能码。</summary>
    public byte OriginalFunctionCode { get; }

    /// <summary>从站返回的异常码。</summary>
    public byte ExceptionCode { get; }

    /// <inheritdoc />
    public override byte FunctionCode => (byte)(OriginalFunctionCode | ModbusFunctionCode.ExceptionMask);

    /// <inheritdoc />
    public override bool IsException => true;

    /// <summary>异常码的可读描述。</summary>
    public string ExceptionMessage => ModbusExceptionCode.GetMessage(ExceptionCode);

    /// <summary>从 PDU 解析异常响应。</summary>
    public static ExceptionResponse FromPdu(byte[] pdu, int offset = 0)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        if (pdu.Length - offset < 2)
        {
            throw new ModbusIOException("异常响应 PDU 长度不足（至少需要 2 字节）。");
        }

        byte original = (byte)(pdu[offset] & ~ModbusFunctionCode.ExceptionMask);
        return new ExceptionResponse(original, pdu[offset + 1]);
    }

    /// <inheritdoc />
    public override byte[] ToPdu() => new[] { FunctionCode, ExceptionCode };
}

/// <summary>按功能码把响应 PDU 解析为具体的响应对象。</summary>
public static class ModbusResponseFactory
{
    /// <summary>解析响应 PDU。</summary>
    /// <param name="pdu">响应 PDU（首字节为功能码）。</param>
    public static ModbusResponse FromPdu(byte[] pdu)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        if (pdu.Length == 0)
        {
            throw new ModbusIOException("响应 PDU 为空。");
        }

        byte functionCode = pdu[0];
        if ((functionCode & ModbusFunctionCode.ExceptionMask) != 0)
        {
            return ExceptionResponse.FromPdu(pdu);
        }

        return functionCode switch
        {
            ModbusFunctionCode.ReadCoils => ReadCoilsResponse.FromPdu(pdu),
            ModbusFunctionCode.ReadInputDiscretes => ReadInputDiscretesResponse.FromPdu(pdu),
            ModbusFunctionCode.ReadMultipleRegisters => ReadMultipleRegistersResponse.FromPdu(pdu),
            ModbusFunctionCode.ReadInputRegisters => ReadInputRegistersResponse.FromPdu(pdu),
            ModbusFunctionCode.WriteCoil => WriteCoilResponse.FromPdu(pdu),
            ModbusFunctionCode.WriteSingleRegister => WriteRegisterResponse.FromPdu(pdu),
            ModbusFunctionCode.ReadExceptionStatus => ReadExceptionStatusResponse.FromPdu(pdu),
            ModbusFunctionCode.Diagnostics => DiagnosticsResponse.FromPdu(pdu),
            ModbusFunctionCode.GetCommEventCounter => GetCommEventCounterResponse.FromPdu(pdu),
            ModbusFunctionCode.GetCommEventLog => GetCommEventLogResponse.FromPdu(pdu),
            ModbusFunctionCode.WriteMultipleCoils => WriteMultipleCoilsResponse.FromPdu(pdu),
            ModbusFunctionCode.WriteMultipleRegisters => WriteMultipleRegistersResponse.FromPdu(pdu),
            ModbusFunctionCode.ReportSlaveId => ReportSlaveIdResponse.FromPdu(pdu),
            ModbusFunctionCode.ReadFileRecord => ReadFileRecordResponse.FromPdu(pdu),
            ModbusFunctionCode.WriteFileRecord => WriteFileRecordResponse.FromPdu(pdu),
            ModbusFunctionCode.MaskWriteRegister => MaskWriteRegisterResponse.FromPdu(pdu),
            ModbusFunctionCode.ReadWriteMultipleRegisters => ReadWriteMultipleRegistersResponse.FromPdu(pdu),
            ModbusFunctionCode.ReadFifoQueue => ReadFifoQueueResponse.FromPdu(pdu),
            _ => throw new ModbusIOException($"不支持的功能码 0x{functionCode:X2}。"),
        };
    }
}

/// <summary>
/// PDU 写入器：大端（网络序）字节缓冲。
/// 对应 jamod 的 <c>DataOutput</c> 写入路径。
/// </summary>
public sealed class ModbusPduWriter
{
    private byte[] _buffer = new byte[ModbusConstants.MaxPduSize];
    private int _length;

    /// <summary>已写入的长度。</summary>
    public int Length => _length;

    /// <summary>写入 1 字节。</summary>
    public void WriteByte(int value)
    {
        Ensure(1);
        _buffer[_length++] = (byte)value;
    }

    /// <summary>写入 16 位大端整数。</summary>
    public void WriteUInt16(int value)
    {
        Ensure(2);
        _buffer[_length++] = (byte)((value >> 8) & 0xFF);
        _buffer[_length++] = (byte)(value & 0xFF);
    }

    /// <summary>写入 32 位大端整数。</summary>
    public void WriteUInt32(int value)
    {
        WriteUInt16((value >> 16) & 0xFFFF);
        WriteUInt16(value & 0xFFFF);
    }

    /// <summary>写入字节序列。</summary>
    public void WriteBytes(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        WriteBytes(data, 0, data.Length);
    }

    /// <summary>写入字节序列的指定片段。</summary>
    public void WriteBytes(byte[] data, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (offset < 0 || count < 0 || offset + count > data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "偏移或长度超出缓冲区范围。");
        }

        Ensure(count);
        data.AsSpan(offset, count).CopyTo(_buffer.AsSpan(_length));
        _length += count;
    }

    /// <summary>导出已写入的内容。</summary>
    public byte[] ToArray() => _buffer.AsSpan(0, _length).ToArray();

    private void Ensure(int extra)
    {
        if (_length + extra > _buffer.Length)
        {
            int newSize = Math.Max(_buffer.Length * 2, _length + extra);
            Array.Resize(ref _buffer, newSize);
        }
    }
}

/// <summary>
/// PDU 读取器：大端（网络序）字节缓冲。
/// 对应 jamod 的 <c>DataInput</c> 读取路径。
/// </summary>
public sealed class ModbusPduReader
{
    private readonly byte[] _buffer;
    private readonly int _end;
    private int _position;

    /// <summary>在缓冲区上创建读取器。</summary>
    public ModbusPduReader(byte[] buffer, int offset = 0, int? length = null)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        _buffer = buffer;
        int len = length ?? (buffer.Length - offset);
        if (offset < 0 || len < 0 || offset + len > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), len, "偏移或长度超出缓冲区范围。");
        }

        _position = offset;
        _end = offset + len;
    }

    /// <summary>当前位置。</summary>
    public int Position => _position;

    /// <summary>剩余可读字节数。</summary>
    public int Remaining => _end - _position;

    /// <summary>读取 1 字节。</summary>
    public int ReadByte()
    {
        EnsureAvailable(1);
        return _buffer[_position++];
    }

    /// <summary>读取 16 位大端无符号整数。</summary>
    public int ReadUInt16()
    {
        EnsureAvailable(2);
        int value = ((_buffer[_position] & 0xFF) << 8) | (_buffer[_position + 1] & 0xFF);
        _position += 2;
        return value;
    }

    /// <summary>读取 16 位大端有符号整数。</summary>
    public short ReadInt16() => (short)ReadUInt16();

    /// <summary>读取 32 位大端整数。</summary>
    public int ReadInt32()
    {
        EnsureAvailable(4);
        int value = ModbusUtil.BytesToInt32(_buffer, _position);
        _position += 4;
        return value;
    }

    /// <summary>读取指定长度的字节数组。</summary>
    public byte[] ReadBytes(int count)
    {
        EnsureAvailable(count);
        var result = _buffer.AsSpan(_position, count).ToArray();
        _position += count;
        return result;
    }

    /// <summary>跳过指定字节。</summary>
    public void Skip(int count)
    {
        EnsureAvailable(count);
        _position += count;
    }

    private void EnsureAvailable(int count)
    {
        if (count < 0 || _position + count > _end)
        {
            throw new ModbusIOException($"响应 PDU 长度不足：需要 {count} 字节，剩余 {Remaining} 字节。");
        }
    }
}
