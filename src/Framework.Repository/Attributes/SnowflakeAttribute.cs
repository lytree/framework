using System;

namespace Framework.Repository.Attributes;

/// <summary>
/// 雪花 Id 特性。
/// 标注在主键属性上后，仓储层在插入新记录时若该属性未赋值，将生成雪花算法 Id 作为默认主键值。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SnowflakeAttribute : Attribute
{
    /// <summary>
    /// 是否启用雪花 Id 自动赋值。
    /// 默认值为 <c>true</c>：当主键字段为空时由仓储层写入雪花算法生成的 Id。
    /// </summary>
    public bool Enable { get; set; } = true;
}
