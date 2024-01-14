using FreeSql.DataAnnotations;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Infrastructure.Repository;

/// <summary>
/// 实体修改
/// </summary>
public class EntityUpdate<TKey> : EntityAdd, IEntityUpdate<TKey> where TKey : struct
{
    /// <summary>
    /// 修改者Id
    /// </summary>
    [Description("修改者Id")]
    [Column(Position = -12, CanInsert = false)]
    [JsonPropertyOrder(10000)]
    public virtual long? ModifiedUserId { get; set; }

    /// <summary>
    /// 修改者
    /// </summary>
    [Description("修改者")]
    [Column(Position = -11, CanInsert = false), MaxLength(50)]
    [JsonPropertyOrder(10001)]
    public virtual string ModifiedUserName { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    [Description("修改时间")]
    [JsonPropertyOrder(10002)]
    [Column(Position = -10, CanInsert = false, ServerTime = DateTimeKind.Local)]
    public virtual DateTime? ModifiedTime { get; set; }
}

/// <summary>
/// 实体修改
/// </summary>
public class EntityUpdate : EntityUpdate<long>
{
}