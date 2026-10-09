using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Framework.DynamicApi;

/// <summary>
/// ASP.NET Core MVC 控制器发现阶段的特性提供器。
/// 将"某个类型是否应被视为动态 API 控制器"的判定逻辑委托给 <see cref="ISelectController"/>，避免默认按 Controller 后缀扫描所有公共非抽象类。
/// </summary>
public class DynamicApiControllerFeatureProvider : ControllerFeatureProvider
{
	private ISelectController _selectController;

	/// <summary>
	/// 使用给定的控制器筛选器初始化 <see cref="DynamicApiControllerFeatureProvider"/>。
	/// </summary>
	/// <param name="selectController">用于判断某个 <see cref="TypeInfo"/> 是否被注册为动态 API 控制器的策略实例。</param>
	public DynamicApiControllerFeatureProvider(ISelectController selectController)
	{
		_selectController = selectController;
	}

	/// <summary>
	/// 由 MVC 在控制器发现阶段调用，返回指定类型是否应作为控制器参与路由注册。
	/// 此实现完全转发给注入的 <see cref="ISelectController.IsController(Type)"/>。
	/// </summary>
	/// <param name="typeInfo">待检测的程序集类型元数据。</param>
	/// <returns>若该类型应被注册为动态 API 控制器则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
	protected override bool IsController(TypeInfo typeInfo)
	{
		return _selectController.IsController(typeInfo);
	}
}