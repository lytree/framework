using Framework.Repository.Attributes;
using LinqToDB.Mapping;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Framework.Repository.Entities;

/// <summary>
/// 实体修改
/// </summary>
public class EntityUpdate<TKey> : EntityAdd, IEntityUpdate<TKey> where TKey : struct
{
    /// <summary>
    /// 修改者Id
    /// </summary>
    [Description("修改者Id")]
    [Column(Order = -12, SkipOnInsert = true)]
    [JsonPropertyOrder(10000)]
    public virtual TKey? ModifiedUserId { get; set; }

    /// <summary>
    /// 修改者
    /// </summary>
    [Description("修改者")]
    [Column(Order = -11, SkipOnInsert = true, Length = 50)]
    [JsonPropertyOrder(10001)]
    public virtual string? ModifiedUserName { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    [Description("修改时间")]
    [JsonPropertyOrder(10002)]
    [Column(Order = -10, SkipOnInsert = true)]
    [ServerTime(CanInsert = false, CanUpdate = true)]
    public virtual DateTime? ModifiedTime { get; set; }
}

/// <summary>
/// 实体修改
/// </summary>
public class EntityUpdate : EntityUpdate<long>
{
}