using LinqToDB.Mapping;
using System.ComponentModel;

namespace Framework.Repository.Entities;

/// <summary>
/// 实体删除
/// </summary>
public class EntityDelete<TKey> : EntityUpdate<TKey>, IDelete where TKey : struct
{
    /// <summary>
    /// 是否删除
    /// </summary>
    [Description("是否删除")]
    [Column(Order = -9)]
    public virtual bool IsDeleted { get; set; }
}

/// <summary>
/// 实体删除
/// </summary>
public class EntityDelete : EntityDelete<long>
{
}