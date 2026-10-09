using System;

namespace Framework.Repository.Attributes;

/// <summary>
/// 服务器时间特性。
/// 标注在时间字段属性上后，仓储层将在插入或更新时由数据库侧写入当前服务器时间，
/// 以保证多节点部署时的时间一致性。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ServerTimeAttribute : Attribute
{
    /// <summary>
    /// 是否在更新时由服务器端写入时间。
    /// 默认值为 <c>false</c>：更新时不写入服务器时间；设为 <c>true</c> 表示更新时由服务器侧覆盖该字段。
    /// </summary>
    public bool CanUpdate { get; set; } = false;

    /// <summary>
    /// 是否在插入时由服务器端写入时间。
    /// 默认值为 <c>true</c>：插入时由服务器侧写入创建时间；设为 <c>false</c> 表示插入时不写入。
    /// </summary>
    public bool CanInsert { get; set; } = true;
}
