using System;

namespace Framework.DynamicApi.Attributes;

[Serializable]
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method)]
public class NonDynamicApiAttribute : Attribute
{

}