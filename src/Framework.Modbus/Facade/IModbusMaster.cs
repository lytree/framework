namespace Framework.Modbus.Facade;

/// <summary>
/// 主站门面接口：以"定位器"为中心的统一读写入口。
/// 对应 jamod 的 <c>net.wimpi.modbus.facade.ModbusMaster</c>。
/// </summary>
public interface IModbusMaster : IDisposable
{
    /// <summary>建立连接。</summary>
    void Connect();

    /// <summary>断开连接。</summary>
    void Disconnect();

    /// <summary>连接是否已建立。</summary>
    bool IsConnected { get; }

    /// <summary>读写超时（毫秒）。</summary>
    int Timeout { get; set; }

    /// <summary>重试次数。</summary>
    int Retries { get; set; }

    /// <summary>当前默认从站地址。</summary>
    int UnitId { get; }

    /// <summary>当前事务标识。</summary>
    int TransactionId { get; }

    /// <summary>设置默认从站地址。</summary>
    void SetUnitId(int unitId);

    /// <summary>按读引用读取。位区返回 <see cref="Util.BitVector"/>，寄存器区返回 <c>Register[]</c>。</summary>
    object Read(ReadReference reference);

    /// <summary>批量按读引用读取。</summary>
    object[] Read(params ReadReference[] references);

    /// <summary>按写引用写入指定值。</summary>
    void Write(WriteReference reference, int value);

    /// <summary>按写引用写入其当前 <see cref="WriteReference.Value"/>。</summary>
    void Write(WriteReference reference);

    /// <summary>批量按写引用写入。</summary>
    void Write(WriteReference[] references, int[] values);
}
