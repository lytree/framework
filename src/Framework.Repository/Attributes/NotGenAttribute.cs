using System;

namespace Framework.Repository.Attributes;

/// <summary>
/// 不生成特性
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class NotGenAttribute : Attribute
{
}