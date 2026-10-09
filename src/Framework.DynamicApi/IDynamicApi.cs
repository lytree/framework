namespace Framework.DynamicApi;

/// <summary>
/// 动态 API 标记接口。
/// 应用服务类实现该接口后，会被 <see cref="DynamicApiControllerFeatureProvider"/> 与 <see cref="ISelectController"/> 识别并自动生成对应的 MVC 控制器与 RESTful 路由。
/// 接口本身不约束任何成员，仅作为"该类型需要被暴露为动态 API"的契约标记。
/// </summary>
public interface IDynamicApi
{

}