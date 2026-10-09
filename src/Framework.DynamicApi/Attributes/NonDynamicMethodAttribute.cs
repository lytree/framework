using System;

namespace Framework.DynamicApi.Attributes;

/// <summary>
/// 标注后，该成员方法不会作为动态 API Action 暴露。
/// 与 <see cref="NonDynamicApiAttribute"/> 用途类似，本特性更聚焦于方法粒度的排除，便于在自动生成控制器时跳过非业务方法（如构造函数、Dispose 等）。
/// </summary>
[Serializable]
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method)]
public class NonDynamicMethodAttribute : Attribute
{

}
