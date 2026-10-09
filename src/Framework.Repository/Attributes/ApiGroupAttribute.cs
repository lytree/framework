using System;

namespace Framework.Repository.Attributes;

/// <summary>
/// 接口分组。
/// 用于在生成 API 文档或对控制器/服务方法进行分组管理时打标。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class ApiGroupAttribute : Attribute
{
    /// <summary>
    /// 是否不归入任何分组。
    /// </summary>
    public bool NonGroup { get; set; }

    /// <summary>
    /// 分组名称列表。
    /// </summary>
    public string[] GroupNames { get; set; }

    /// <summary>
    /// 使用一个或多个分组名称初始化 <see cref="ApiGroupAttribute"/>。
    /// </summary>
    /// <param name="groupNames">分组名称列表。</param>
    public ApiGroupAttribute(params string[] groupNames)
    {
        GroupNames = groupNames;
    }
}
