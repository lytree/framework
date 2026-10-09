using Framework.Repository.Data;
using System;
using System.Data;

namespace Framework.Repository.Attributes;

/// <summary>
/// 启用事务。
/// 该特性基于 linq2db 的 <see cref="System.Data.IDbConnection"/> / <see cref="System.Data.Common.DbTransaction"/>
/// 实现，调用方可在 AOP 切面中解析后调用 <see cref="Data.TransactionScopeFactory"/>
/// 来实际开启事务。
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public sealed class TransactionAttribute : Attribute
{
    /// <summary>
    /// 事务传播方式。
    /// 默认为 <see cref="Propagation.Required"/>：若已有环境事务则加入，否则新建事务。
    /// </summary>
    public Propagation Propagation { get; set; } = Propagation.Required;

    /// <summary>
    /// 事务隔离级别。
    /// 默认为 <see cref="IsolationLevel.ReadCommitted"/>。
    /// </summary>
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;

    /// <summary>
    /// 数据库注册键。
    /// 用于在多库场景下指定本事务对应的数据库连接；为 null 时使用默认数据库。
    /// </summary>
    public string? DbKey { get; set; }

    /// <summary>
    /// 初始化 <see cref="TransactionAttribute"/> 的新实例。
    /// </summary>
    public TransactionAttribute()
    {
    }

    /// <summary>
    /// 使用指定的数据库注册键初始化 <see cref="TransactionAttribute"/> 的新实例。
    /// </summary>
    /// <param name="dbKey">数据库注册键。</param>
    public TransactionAttribute(string dbKey)
    {
        DbKey = dbKey;
    }
}
