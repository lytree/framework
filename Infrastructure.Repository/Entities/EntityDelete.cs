using FreeSql.DataAnnotations;
using System.ComponentModel;

namespace Infrastructure.Repository;

/// <summary>
/// 实体删除
/// </summary>
public class EntityDelete<TKey> : EntityUpdate, IDelete
{
    /// <summary>
    /// 是否删除
    /// </summary>
    [Description("是否删除")]
    [Column(Position = -9)]
    public virtual bool IsDeleted { get; set; } = false;
}

/// <summary>
/// 实体删除
/// </summary>
public class EntityDelete : EntityDelete<long>
{
}