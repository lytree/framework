using System;

namespace Infrastructure.Repository;

/// <summary>
/// 不生成特性
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class NotGenAttribute : Attribute
{
}