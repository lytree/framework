using System;

namespace Framework.DynamicApi.Attributes;

/// <summary>
/// 标注后，该类型（接口/类）或成员方法将不参与动态 API 的自动注册。
/// 当一个类型满足动态 API 条件但希望整体排除，或某个方法在类级别被纳入时希望单独排除，使用本特性。
/// </summary>
[Serializable]
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method)]
public class NonDynamicApiAttribute : Attribute
{

}