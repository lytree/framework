using System;
using System.Collections.Generic;
using Framework.DynamicApi.Enums;

namespace Framework.DynamicApi;

/// <summary>
/// Minimal API 模式下的运行时配置。
/// 生成代码在 <c>MapDynamicApis()</c> 时读取这些值计算最终路由与响应包装行为，
/// HTTP 动词、参数绑定等编译期信息不再受此处配置影响。
/// </summary>
public class DynamicApiOptions
{
	public DynamicApiOptions()
	{
		RemoveControllerPostfixes = new List<string> { "AppService", "ApplicationService", "ApiController", "Controller", "Services", "Service" };
		RemoveActionPostfixes = new List<string> { "Async" };
		DefaultAreaName = string.Empty;
		DefaultApiPrefix = "api";
	}

	/// <summary>
	/// 默认区域名称；<c>[DynamicApi]</c> 未指定 <c>Area</c> 时作为路由中的区域段使用。
	/// </summary>
	public string DefaultAreaName { get; set; }

	/// <summary>
	/// 所有动态 API 的公共路由前缀，默认 <c>api</c>。
	/// </summary>
	public string DefaultApiPrefix { get; set; }

	/// <summary>
	/// 控制器名需要剥离的后缀集合，默认包含 <c>Service</c>、<c>AppService</c> 等。
	/// </summary>
	public List<string> RemoveControllerPostfixes { get; set; }

	/// <summary>
	/// Action 名需要剥离的后缀集合，默认包含 <c>Async</c>。
	/// </summary>
	public List<string> RemoveActionPostfixes { get; set; }

	/// <summary>
	/// 控制器名与 Action 名的命名约定，默认 <see cref="NamingConventionEnum.KebabCase"/>。
	/// </summary>
	public NamingConventionEnum NamingConvention { get; set; } = NamingConventionEnum.KebabCase;

	/// <summary>
	/// 自定义 Action 名格式化委托；仅在 <see cref="NamingConventionEnum.Custom"/> 下生效，返回 <c>null</c> 时回退默认规则。
	/// </summary>
	public Func<string, string> GetRestFulActionName { get; set; }

	/// <summary>
	/// 自定义控制器名格式化委托；仅在 <see cref="NamingConventionEnum.Custom"/> 下生效，返回 <c>null</c> 时保留剥离后缀后的名称。
	/// </summary>
	public Func<string, string> GetRestFulControllerName { get; set; }

	/// <summary>
	/// 是否把返回值包装为 <see cref="Response.ResponseResul{T}"/>，默认 <c>true</c>。
	/// 标记 <c>[NonFormatResult]</c> 的端点始终不包装。
	/// </summary>
	public bool FormatResult { get; set; } = true;
}
