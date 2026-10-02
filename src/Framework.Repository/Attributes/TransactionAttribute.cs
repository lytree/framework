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
    /// 事务传播方式
    /// </summary>
    public Propagation Propagation { get; set; } = Propagation.Required;

    /// <summary>
    /// 事务隔离级别
    /// </summary>
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;

    /// <summary>
    /// 数据库注册键
    /// </summary>
    public string? DbKey { get; set; }

    public TransactionAttribute()
    {
    }

    public TransactionAttribute(string dbKey)
    {
        DbKey = dbKey;
    }
}