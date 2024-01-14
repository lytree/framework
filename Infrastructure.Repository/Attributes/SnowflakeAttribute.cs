using System;

namespace Infrastructure.Repository;

[AttributeUsage(AttributeTargets.Property)]
public class SnowflakeAttribute : Attribute
{
    public bool Enable { get; set; } = true;
}