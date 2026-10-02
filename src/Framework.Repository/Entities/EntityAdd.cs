using Framework.Repository.Attributes;
using LinqToDB.Mapping;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Framework.Repository.Entities;

/// <summary>
/// 实体创建
/// </summary>
public class EntityAdd<TKey> : Entity<TKey>, IEntityAdd<TKey> where TKey : struct
{
    /// <summary>
    /// 创建者Id
    /// </summary>
    [Description("创建者Id")]
    [Column(Order = -22, SkipOnUpdate = true)]
    public virtual long? CreatedUserId { get; set; }

    /// <summary>
    /// 创建者
    /// </summary>
    [Description("创建者")]
    [Column(Order = -21, SkipOnUpdate = true, Length = 50)]
    public virtual string? CreatedUserName { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Description("创建时间")]
    [Column(Order = -20, SkipOnUpdate = true)]
    [ServerTime(CanInsert = true, CanUpdate = false)]
    public virtual DateTime? CreatedTime { get; set; }
}

/// <summary>
/// 实体创建
/// </summary>
public class EntityAdd : EntityAdd<long>
{
}