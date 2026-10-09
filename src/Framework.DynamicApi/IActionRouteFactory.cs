using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Framework.DynamicApi;

/// <summary>
/// 动态 API 路由模板工厂。
/// 负责把单个 MVC <see cref="ActionModel"/> 渲染为最终的路由字符串，约定 <c>apiPrefix/areaName/controllerName/actionName</c> 的拼接方式。
/// 通过接口暴露以便在外部替换默认实现（例如增加版本前缀或租户前缀）。
/// </summary>
public interface IActionRouteFactory
{
	/// <summary>
	/// 生成指定 Action 的最终路由模板字符串。
	/// </summary>
	/// <param name="areaName">区域（Areas）名称，可能为空字符串。</param>
	/// <param name="controllerName">经过命名约定转换后的控制器名称（不含 Controller 后缀）。</param>
	/// <param name="action">当前要生成路由的 MVC Action 模型。</param>
	/// <returns>拼接后的路由模板字符串，例如 <c>api/admin/user/get-list</c>。</returns>
	string CreateActionRouteModel(string areaName, string controllerName, ActionModel action);
}

internal class DefaultActionRouteFactory : IActionRouteFactory
{
	// 优先使用该 Action 所属程序集单独配置的 ApiPrefix，否则回落到全局默认前缀
	private static string GetApiPreFix(ActionModel action)
	{
		var getValueSuccess = AppConsts.AssemblyDynamicApiOptions
			.TryGetValue(action.Controller.ControllerType.Assembly, out AssemblyDynamicApiOptions assemblyDynamicApiOptions);
		if (getValueSuccess && !string.IsNullOrWhiteSpace(assemblyDynamicApiOptions?.ApiPrefix))
		{
			return assemblyDynamicApiOptions.ApiPrefix;
		}

		return AppConsts.DefaultApiPreFix;
	}

	/// <summary>
	/// 默认的路由模板生成实现。
	/// 按 <c>{apiPrefix}/{areaName}/{controllerName}/{action.ActionName}</c> 顺序拼接，并对可能为空的 areaName 产生连续的 <c>//</c> 进行规范化。
	/// </summary>
	/// <param name="areaName">区域名，允许为空。</param>
	/// <param name="controllerName">控制器名（已剥离后缀）。</param>
	/// <param name="action">当前 MVC Action 模型。</param>
	/// <returns>规范化后的路由模板字符串，连续斜杠已被合并为单个斜杠。</returns>
	public string CreateActionRouteModel(string areaName, string controllerName, ActionModel action)
	{
		var apiPreFix = GetApiPreFix(action);
		var routeStr = $"{apiPreFix}/{areaName}/{controllerName}/{action.ActionName}".Replace("//", "/");
		return routeStr;
	}
}