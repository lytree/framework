namespace Framework.DynamicApi;

/// <summary>
/// 动态 API 标记接口。
/// 公共、非抽象、非泛型且标注了 <see cref="Attributes.DynamicApiAttribute"/> 的实现类，
/// 会在编译期被源生成器识别并生成 Minimal API 端点注册代码。
/// 接口本身不约束任何成员，仅作为“该类型需要被暴露为动态 API”的契约标记。
/// </summary>
public interface IDynamicApi
{
}
