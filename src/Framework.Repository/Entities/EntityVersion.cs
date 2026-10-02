using LinqToDB.Mapping;
using System.ComponentModel;

namespace Framework.Repository.Entities;

/// <summary>
/// 实体版本
/// </summary>
public class EntityVersion<TKey> : EntityBase, IVersion
{
    /// <summary>
    /// 版本
    /// </summary>
    [Description("版本")]
    [Column(Order = -30)]
    public virtual long Version { get; set; }
}

/// <summary>
/// 实体版本
/// </summary>
public sealed class EntityVersion : EntityVersion<long>
{
}