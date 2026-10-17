using Framework.Modbus.Master;
using Framework.Modbus.Util;

namespace Framework.Modbus.Facade;

/// <summary>地址区类型。</summary>
public enum ReferenceType
{
    /// <summary>线圈（可读可写，FC01 / FC05 / FC0F）。</summary>
    CoilStatus,

    /// <summary>离散输入（只读，FC02）。</summary>
    InputStatus,

    /// <summary>保持寄存器（可读可写，FC03 / FC06 / FC10 / FC16 / FC17）。</summary>
    HoldingRegister,

    /// <summary>输入寄存器（只读，FC04）。</summary>
    InputRegister,
}

/// <summary>
/// 地址引用：从站地址 + 区域类型 + 数据区偏移。
/// 对应 jamod 的 <c>net.wimpi.modbus.Reference</c>。
/// </summary>
public abstract class Reference
{
    /// <summary>构造地址引用。</summary>
    protected Reference(int unitId, int reference, ReferenceType type)
    {
        if (unitId is < 0 or > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(unitId), unitId, "从站地址必须落在 [0, 255]。");
        }

        if (reference is < 0 or > 0xFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(reference), reference, "数据区偏移必须落在 [0, 65535]。");
        }

        UnitId = unitId;
        Offset = reference;
        Type = type;
    }

    /// <summary>从站地址。</summary>
    public int UnitId { get; }

    /// <summary>数据区偏移（PDU 偏移，从 0 开始）。</summary>
    public int Offset { get; }

    /// <summary>地址区类型。</summary>
    public ReferenceType Type { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Type}[{UnitId}:{Offset}]";
}

/// <summary>
/// 读引用。对应 jamod 的 <c>net.wimpi.modbus.ReadReference</c>。
/// </summary>
public abstract class ReadReference : Reference
{
    /// <summary>构造读引用。</summary>
    protected ReadReference(int unitId, int reference, int count, ReferenceType type)
        : base(unitId, reference, type)
    {
        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "读取数量必须大于 0。");
        }

        Count = count;
    }

    /// <summary>读取数量（位或寄存器）。</summary>
    public int Count { get; }

    /// <summary>执行读取。返回 <see cref="BitVector"/>（位区）或 <c>Register[]</c>（寄存器区）。</summary>
    public abstract object Read(ModbusMaster master);

    /// <inheritdoc />
    public override string ToString() => $"{base.ToString()} ×{Count}";
}

/// <summary>
/// 写引用。对应 jamod 的 <c>net.wimpi.modbus.WriteReference</c>。
/// </summary>
public abstract class WriteReference : Reference
{
    /// <summary>构造写引用。</summary>
    protected WriteReference(int unitId, int reference, ReferenceType type)
        : base(unitId, reference, type)
    {
    }

    /// <summary>最近一次写入的值。</summary>
    public int Value { get; set; }

    /// <summary>以 <see cref="Value"/> 执行写入。</summary>
    public void Write(ModbusMaster master) => Write(master, Value);

    /// <summary>以指定值执行写入。</summary>
    public abstract void Write(ModbusMaster master, int value);
}

/// <summary>线圈读取引用（FC01）。</summary>
public sealed class ReadCoilReference : ReadReference
{
    /// <summary>构造引用。</summary>
    public ReadCoilReference(int unitId, int reference, int count)
        : base(unitId, reference, count, ReferenceType.CoilStatus)
    {
    }

    /// <inheritdoc />
    public override object Read(ModbusMaster master)
    {
        ArgumentNullException.ThrowIfNull(master);
        return master.ReadCoils(Offset, Count);
    }
}

/// <summary>离散输入读取引用（FC02）。</summary>
public sealed class ReadDiscreteInputReference : ReadReference
{
    /// <summary>构造引用。</summary>
    public ReadDiscreteInputReference(int unitId, int reference, int count)
        : base(unitId, reference, count, ReferenceType.InputStatus)
    {
    }

    /// <inheritdoc />
    public override object Read(ModbusMaster master)
    {
        ArgumentNullException.ThrowIfNull(master);
        return master.ReadInputDiscretes(Offset, Count);
    }
}

/// <summary>保持寄存器读取引用（FC03）。</summary>
public sealed class ReadHoldingRegisterReference : ReadReference
{
    /// <summary>构造引用。</summary>
    public ReadHoldingRegisterReference(int unitId, int reference, int count)
        : base(unitId, reference, count, ReferenceType.HoldingRegister)
    {
    }

    /// <inheritdoc />
    public override object Read(ModbusMaster master)
    {
        ArgumentNullException.ThrowIfNull(master);
        return master.ReadMultipleRegisters(Offset, Count);
    }
}

/// <summary>输入寄存器读取引用（FC04）。</summary>
public sealed class ReadInputRegisterReference : ReadReference
{
    /// <summary>构造引用。</summary>
    public ReadInputRegisterReference(int unitId, int reference, int count)
        : base(unitId, reference, count, ReferenceType.InputRegister)
    {
    }

    /// <inheritdoc />
    public override object Read(ModbusMaster master)
    {
        ArgumentNullException.ThrowIfNull(master);
        return master.ReadInputRegisters(Offset, Count);
    }
}

/// <summary>线圈写入引用（FC05）。值为 0 表示 OFF，非 0 表示 ON。</summary>
public sealed class WriteCoilReference : WriteReference
{
    /// <summary>构造引用。</summary>
    public WriteCoilReference(int unitId, int reference)
        : base(unitId, reference, ReferenceType.CoilStatus)
    {
    }

    /// <inheritdoc />
    public override void Write(ModbusMaster master, int value)
    {
        ArgumentNullException.ThrowIfNull(master);
        Value = value;
        master.WriteCoil(Offset, value != 0);
    }
}

/// <summary>保持寄存器写入引用（FC06）。</summary>
public sealed class WriteHoldingRegisterReference : WriteReference
{
    /// <summary>构造引用。</summary>
    public WriteHoldingRegisterReference(int unitId, int reference)
        : base(unitId, reference, ReferenceType.HoldingRegister)
    {
    }

    /// <inheritdoc />
    public override void Write(ModbusMaster master, int value)
    {
        ArgumentNullException.ThrowIfNull(master);
        if (value is < 0 or > 0xFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "寄存器值必须落在 [0, 65535]。");
        }

        Value = value;
        master.WriteRegister(Offset, value);
    }
}

/// <summary>
/// 地址定位器工厂。对应 jamod 的 <c>net.wimpi.modbus.util.BaseLocator</c>。
/// </summary>
public static class BaseLocator
{
    /// <summary>线圈读引用（FC01）。</summary>
    public static ReadReference CoilStatus(int unitId, int reference, int count = 1) =>
        new ReadCoilReference(unitId, reference, count);

    /// <summary>离散输入读引用（FC02）。</summary>
    public static ReadReference InputStatus(int unitId, int reference, int count = 1) =>
        new ReadDiscreteInputReference(unitId, reference, count);

    /// <summary>保持寄存器读引用（FC03）。</summary>
    public static ReadReference HoldingRegister(int unitId, int reference, int count = 1) =>
        new ReadHoldingRegisterReference(unitId, reference, count);

    /// <summary>输入寄存器读引用（FC04）。</summary>
    public static ReadReference InputRegister(int unitId, int reference, int count = 1) =>
        new ReadInputRegisterReference(unitId, reference, count);

    /// <summary>线圈写引用（FC05）。</summary>
    public static WriteReference WriteCoil(int unitId, int reference) =>
        new WriteCoilReference(unitId, reference);

    /// <summary>保持寄存器写引用（FC06）。</summary>
    public static WriteReference WriteHoldingRegister(int unitId, int reference) =>
        new WriteHoldingRegisterReference(unitId, reference);
}
