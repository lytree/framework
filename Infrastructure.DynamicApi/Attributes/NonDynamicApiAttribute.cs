using System;

namespace Infrastructure.DynamicApi;

[Serializable]
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method)]
public class NonDynamicApiAttribute : Attribute
{

}