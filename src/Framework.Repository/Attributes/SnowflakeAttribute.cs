using System;

namespace Framework.Repository.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SnowflakeAttribute : Attribute
{
    public bool Enable { get; set; } = true;
}