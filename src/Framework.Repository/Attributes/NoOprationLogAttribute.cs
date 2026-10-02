using System;

namespace Framework.Repository.Attributes;

/// <summary>
/// 禁用操作日志
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class NoOprationLogAttribute : Attribute
{
}