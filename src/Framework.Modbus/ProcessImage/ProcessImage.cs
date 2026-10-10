using Framework.Modbus.Util;

namespace Framework.Modbus.ProcessImage;

/// <summary>16 位寄存器抽象。</summary>
public interface IRegister
{
    /// <summary>寄存器的无符号 16 位值。</summary>
    int Value { get; }

    /// <summary>按大端导出 2 字节。</summary>
    byte[] ToBytes();

    /// <summary>以 16 位整数写入。</summary>
    void SetValue(int value);

    /// <summary>以 2 字节大端写入。</summary>
    void SetValue(byte[] bytes);
}

/// <summary>
/// 保持寄存器。对应 jamod 的 <c>net.wimpi.modbus.procimg.Register</c>。
/// 通过 <see cref="Create(int)"/> 等工厂方法构造。
/// </summary>
public class Register : IRegister
{
    /// <summary>寄存器的 2 个字节（大端）。</summary>
    protected readonly byte[] Bytes = new byte[2];

    /// <summary>以 16 位值创建寄存器。</summary>
    public static Register Create(int value) => new SimpleRegister(value);

    /// <summary>以高 / 低字节创建寄存器。</summary>
    public static Register Create(int hi, int lo) => new SimpleRegister(hi, lo);

    /// <summary>以 2 字节大端创建寄存器。</summary>
    public static Register Create(byte[] bytes) => new SimpleRegister(bytes);

    /// <inheritdoc />
    public virtual int Value
    {
        get => ModbusUtil.BytesToUInt16(Bytes, 0);
        set => SetValue(value);
    }

    /// <inheritdoc />
    public virtual byte[] ToBytes() => (byte[])Bytes.Clone();

    /// <inheritdoc />
    public virtual void SetValue(int value)
    {
        Bytes[0] = (byte)((value >> 8) & 0xFF);
        Bytes[1] = (byte)(value & 0xFF);
    }

    /// <inheritdoc />
    public virtual void SetValue(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length != 2)
        {
            throw new ArgumentException("寄存器的字节长度必须为 2。", nameof(bytes));
        }

        Bytes[0] = bytes[0];
        Bytes[1] = bytes[1];
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>保持寄存器的默认实现。</summary>
public sealed class SimpleRegister : Register
{
    /// <summary>创建值 0 的寄存器。</summary>
    public SimpleRegister()
    {
    }

    /// <summary>以 16 位值创建寄存器。</summary>
    public SimpleRegister(int value) => SetValue(value);

    /// <summary>以高 / 低字节创建寄存器。</summary>
    public SimpleRegister(int hi, int lo) => SetValue(((hi & 0xFF) << 8) | (lo & 0xFF));

    /// <summary>以 2 字节大端创建寄存器。</summary>
    public SimpleRegister(byte[] bytes) => SetValue(bytes);
}

/// <summary>输入寄存器抽象（只读语义，写方法由实现决定是否允许）。</summary>
public interface IInputRegister : IRegister
{
}

/// <summary>输入寄存器。对应 jamod 的 <c>net.wimpi.modbus.procimg.InputRegister</c>。</summary>
public class InputRegister : Register
{
    /// <summary>以 16 位值创建输入寄存器。</summary>
    public new static InputRegister Create(int value) => new SimpleInputRegister(value);

    /// <summary>以 2 字节大端创建输入寄存器。</summary>
    public new static InputRegister Create(byte[] bytes) => new SimpleInputRegister(bytes);
}

/// <summary>输入寄存器的默认实现。</summary>
public sealed class SimpleInputRegister : InputRegister, IInputRegister
{
    /// <summary>创建值 0 的输入寄存器。</summary>
    public SimpleInputRegister()
    {
    }

    /// <summary>以 16 位值创建输入寄存器。</summary>
    public SimpleInputRegister(int value) => SetValue(value);

    /// <summary>以 2 字节大端创建输入寄存器。</summary>
    public SimpleInputRegister(byte[] bytes) => SetValue(bytes);
}

/// <summary>离散输入抽象。</summary>
public interface IDigitalIn
{
    /// <summary>当前状态。</summary>
    bool IsSet { get; }

    /// <summary>设置状态。</summary>
    void Set(bool value);
}

/// <summary>线圈输出抽象。</summary>
public interface IDigitalOut
{
    /// <summary>当前状态。</summary>
    bool IsSet { get; }

    /// <summary>设置状态。</summary>
    void Set(bool value);
}

/// <summary>离散输入。对应 jamod 的 <c>net.wimpi.modbus.procimg.DigitalIn</c>。</summary>
public class DigitalIn : IDigitalIn
{
    private bool _set;

    /// <inheritdoc />
    public virtual bool IsSet => _set;

    /// <inheritdoc />
    public virtual void Set(bool value) => _set = value;
}

/// <summary>离散输入的默认实现。</summary>
public sealed class SimpleDigitalIn : DigitalIn
{
    /// <summary>创建默认状态的离散输入。</summary>
    public SimpleDigitalIn()
    {
    }

    /// <summary>以指定状态创建离散输入。</summary>
    public SimpleDigitalIn(bool value) => Set(value);
}

/// <summary>线圈输出。对应 jamod 的 <c>net.wimpi.modbus.procimg.DigitalOut</c>。</summary>
public class DigitalOut : IDigitalOut
{
    private bool _set;

    /// <inheritdoc />
    public virtual bool IsSet => _set;

    /// <inheritdoc />
    public virtual void Set(bool value) => _set = value;
}

/// <summary>线圈输出的默认实现。</summary>
public sealed class SimpleDigitalOut : DigitalOut
{
    /// <summary>创建默认状态的线圈输出。</summary>
    public SimpleDigitalOut()
    {
    }

    /// <summary>以指定状态创建线圈输出。</summary>
    public SimpleDigitalOut(bool value) => Set(value);
}

/// <summary>文件记录中的一个 16 位记录项。</summary>
public sealed class Record
{
    /// <summary>创建记录项。</summary>
    public Record(int reference, int value = 0)
    {
        Reference = reference;
        Value = value;
    }

    /// <summary>记录号（相对于所属文件）。</summary>
    public int Reference { get; }

    /// <summary>记录值。</summary>
    public int Value { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"{Reference}={Value}";
}

/// <summary>
/// 文件记录：一个文件号下连续的若干 <see cref="Record"/>。
/// 对应 jamod 的 <c>net.wimpi.modbus.procimg.FileRecord</c>。
/// </summary>
public sealed class FileRecord
{
    private readonly Record[] _records;

    /// <summary>以文件号与记录数量创建（记录号从 0 开始，值全 0）。</summary>
    public FileRecord(int fileNumber, int count)
        : this(fileNumber, Enumerable.Range(0, count).Select(i => new Record(i)).ToArray())
    {
    }

    /// <summary>以文件号与记录数组创建。</summary>
    public FileRecord(int fileNumber, Record[] records)
    {
        ArgumentNullException.ThrowIfNull(records);
        FileNumber = fileNumber;
        _records = records;
    }

    /// <summary>文件号。</summary>
    public int FileNumber { get; }

    /// <summary>记录条数。</summary>
    public int Count => _records.Length;

    /// <summary>按索引访问记录。</summary>
    public Record this[int index] => _records[index];

    /// <summary>全部记录。</summary>
    public IReadOnlyList<Record> Records => _records;

    /// <summary>起始记录号。</summary>
    public int StartReference => _records.Length == 0 ? 0 : _records[0].Reference;
}
