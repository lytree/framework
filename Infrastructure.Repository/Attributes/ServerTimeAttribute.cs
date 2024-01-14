using System;

namespace Infrastructure.Repository;

[AttributeUsage(AttributeTargets.Property)]
public class ServerTimeAttribute : Attribute
{
}