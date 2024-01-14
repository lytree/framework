using System;

namespace Infrastructure.Repository;

[AttributeUsage(AttributeTargets.Property)]
public class OrderGuidAttribute : Attribute
{
    public bool Enable { get; set; } = true;
}