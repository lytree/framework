using System;

namespace Framework.Repository.Attributes;

/// <summary>
/// 不注册到第三方IOC容器
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class NonRegisterIOCAttribute : Attribute
{
}