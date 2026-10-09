using System;

namespace Framework.Repository.Attributes;

/// <summary>
/// 有序 Guid 特性。
/// 标注在实体属性上后，仓储层在插入新记录时若该属性未赋值，将按顺序生成 Guid（连续段更利于索引性能）。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class OrderGuidAttribute : Attribute
{
    /// <summary>
    /// 是否启用有序 Guid 自动赋值。
    /// 默认值为 <c>true</c>：当字段为空时由仓储层写入新的有序 Guid。
    /// </summary>
    public bool Enable { get; set; } = true;
}
