using System;

namespace Framework.Repository.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class OrderGuidAttribute : Attribute
{
    public bool Enable { get; set; } = true;
}