using Framework.Modbus.IO;
using Framework.Modbus.Master;

namespace Framework.Modbus.Facade;

/// <summary>
/// 门面主站基类：在底层 <see cref="ModbusMaster"/> 之上叠加定位器读写能力。
/// 对应 jamod 的 <c>net.wimpi.modbus.facade.ModbusMaster</c> 实现族。
/// </summary>
public abstract class ModbusMasterFacade : ModbusMaster, IModbusMaster
{
    /// <summary>以传输层构造门面主站。</summary>
    protected ModbusMasterFacade(IModbusTransport transport) : base(transport)
    {
    }

    /// <inheritdoc />
    public virtual void Disconnect() => Close();

    /// <inheritdoc />
    public virtual void SetUnitId(int unitId) => UnitId = unitId;

    /// <inheritdoc />
    public virtual object Read(ReadReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        int previousUnitId = UnitId;
        try
        {
            UnitId = reference.UnitId;
            return reference.Read(this);
        }
        finally
        {
            UnitId = previousUnitId;
        }
    }

    /// <inheritdoc />
    public virtual object[] Read(params ReadReference[] references)
    {
        ArgumentNullException.ThrowIfNull(references);
        var results = new object[references.Length];
        for (int i = 0; i < references.Length; i++)
        {
            results[i] = Read(references[i]);
        }

        return results;
    }

    /// <inheritdoc />
    public virtual void Write(WriteReference reference, int value)
    {
        ArgumentNullException.ThrowIfNull(reference);

        int previousUnitId = UnitId;
        try
        {
            UnitId = reference.UnitId;
            reference.Write(this, value);
        }
        finally
        {
            UnitId = previousUnitId;
        }
    }

    /// <inheritdoc />
    public virtual void Write(WriteReference reference) => Write(reference, reference.Value);

    /// <inheritdoc />
    public virtual void Write(WriteReference[] references, int[] values)
    {
        ArgumentNullException.ThrowIfNull(references);
        ArgumentNullException.ThrowIfNull(values);
        if (references.Length != values.Length)
        {
            throw new ArgumentException("写引用数量与值数量必须一致。", nameof(values));
        }

        for (int i = 0; i < references.Length; i++)
        {
            Write(references[i], values[i]);
        }
    }
}
