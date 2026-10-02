namespace Framework.Repository.Data;

/// <summary>
/// 事务传播方式。
/// 与 System.Transactions.TransactionScope 选项保持一致，便于在不同 ORM/上下文间复用。
/// </summary>
public enum Propagation
{
    /// <summary>
    /// 若已有环境事务则加入；否则新建事务。
    /// </summary>
    Required = 0,

    /// <summary>
    /// 始终新建独立事务，挂起任何外部事务。
    /// </summary>
    RequiresNew = 1,

    /// <summary>
    /// 若已有外部事务则抛出异常；否则新建事务。
    /// </summary>
    Disabled = 2,

    /// <summary>
    /// 若已有环境事务则加入（嵌套保存点）；否则以非事务方式执行。
    /// </summary>
    Nested = 3,

    /// <summary>
    /// 抑制外部事务：以非事务方式强制执行。
    /// </summary>
    Suppressed = 4,
}