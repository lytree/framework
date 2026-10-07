using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Framework.DynamicApi.Attributes;
using Framework.DynamicApi.Enums;
using Framework.DynamicApi.Extensions;
using Framework.DynamicApi.Helpers;
using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Framework.DynamicApi;

public partial class DynamicApiConvention : IApplicationModelConvention
{
	private readonly ISelectController _selectController;
	private readonly IActionRouteFactory _actionRouteFactory;

	public DynamicApiConvention(ISelectController selectController, IActionRouteFactory actionRouteFactory)
	{
		_selectController = selectController;
		_actionRouteFactory = actionRouteFactory;
	}

	public static string GetSeparateWords(string value, NamingConventionEnum namingConvention = NamingConventionEnum.KebabCase)
	{
		if (string.IsNullOrWhiteSpace(value))
			return value;

		var separator = "-";
		if (namingConvention == NamingConventionEnum.SnakeCase)
		{
			separator = "_";
		}
		else if (namingConvention == NamingConventionEnum.ExtensionCase)
		{
			separator = ".";
		}

		return SeparateWordsRegex().Replace(value, $"{separator}$1").Trim().ToLowerInvariant();
	}

	// 源生成正则：替代 RegexOptions.Compiled。Compiled 每次调用都要动态 emit 程序集，
	// 而命名转换只在启动期执行一次，Compiled 的预热成本远大于收益。
	[GeneratedRegex("(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])", RegexOptions.CultureInvariant)]
	private static partial Regex SeparateWordsRegex();

	public static string GetFormatName(string value, NamingConventionEnum namingConvention = NamingConventionEnum.KebabCase)
	{
		if (namingConvention == NamingConventionEnum.KebabCase ||
		   namingConvention == NamingConventionEnum.SnakeCase ||
		   namingConvention == NamingConventionEnum.ExtensionCase)
		{
			return GetSeparateWords(value, namingConvention);
		}
		else if (namingConvention == NamingConventionEnum.PascalCase)
		{
			return value.FirstCharToUpper();
		}
		else if (namingConvention == NamingConventionEnum.CamelCase)
		{
			return value.FirstCharToLower();
		}

		return value;
	}

	public void Apply(ApplicationModel application)
	{
		foreach (var controller in application.Controllers)
		{
			var type = controller.ControllerType.AsType();
			var DynamicApiAttr = ReflectionHelper.GetSingleAttributeOrDefaultByFullSearch<DynamicApiAttribute>(type.GetTypeInfo());

			if (!(_selectController is DefaultSelectController) && _selectController.IsController(type))
			{
				controller.ControllerName = controller.ControllerName.RemovePostFix(AppConsts.ControllerPostfixes.ToArray());
				if (AppConsts.NamingConvention == NamingConventionEnum.Custom)
				{
					controller.ControllerName = GetRestFulControllerName(controller.ControllerName);
				}
				else
				{
					controller.ControllerName = GetFormatName(controller.ControllerName, AppConsts.NamingConvention);
				}

				ConfigureDynamicApi(controller, DynamicApiAttr);
			}
			else
			{
				if (typeof(IDynamicApi).GetTypeInfo().IsAssignableFrom(type))
				{
					controller.ControllerName = controller.ControllerName.RemovePostFix(AppConsts.ControllerPostfixes.ToArray());
					if (AppConsts.NamingConvention == NamingConventionEnum.Custom)
					{
						controller.ControllerName = GetRestFulControllerName(controller.ControllerName);
					}
					else
					{
						controller.ControllerName = GetFormatName(controller.ControllerName, AppConsts.NamingConvention);
					}
					ConfigureArea(controller, DynamicApiAttr);
					ConfigureDynamicApi(controller, DynamicApiAttr);
				}
				else
				{
					if (DynamicApiAttr != null)
					{
						ConfigureArea(controller, DynamicApiAttr);
						ConfigureDynamicApi(controller, DynamicApiAttr);
					}
				}
			}
		}
	}

	private void ConfigureArea(ControllerModel controller, DynamicApiAttribute attr)
	{
		if (controller.RouteValues.ContainsKey("area"))
		{
			return;
		}

		// 原实现在此处对 attr == null 抛 ArgumentException，
		// 但「实现了 IDynamicApi 却没打 [DynamicApi] 特性」是完全合法的用法，
		// 会在应用启动扫描控制器时直接抛异常导致启动失败。改为静默跳过。
		if (!string.IsNullOrEmpty(attr?.Area))
		{
			controller.RouteValues["area"] = attr.Area;
		}
		else if (!string.IsNullOrEmpty(AppConsts.DefaultAreaName))
		{
			controller.RouteValues["area"] = AppConsts.DefaultAreaName;
		}
	}

	private void ConfigureDynamicApi(ControllerModel controller, DynamicApiAttribute controllerAttr)
	{
		ConfigureApiExplorer(controller);
		ConfigureSelector(controller, controllerAttr);
		ConfigureParameters(controller);
		if (AppConsts.FormatResult)
		{
			ConfigureFormatResult(controller);
		}
	}

	private void ConfigureFormatResult(ControllerModel controller)
	{
		foreach (var action in controller.Actions)
		{
			if (!CheckNoMapMethod(action) && !CheckNoFormatResultMethod(action))
			{
				var returnType = action.ActionMethod.GetReturnType();

				if (returnType == typeof(void)) continue;
				action.Filters.Add(new FormatResultAttribute(returnType));
			}
		}
	}

	private void ConfigureParameters(ControllerModel controller)
	{
		foreach (var action in controller.Actions)
		{
			if (!CheckNoMapMethod(action))
				foreach (var para in action.Parameters)
				{
					if (para.BindingInfo != null)
					{
						continue;
					}

					if (!TypeHelper.IsPrimitiveExtendedIncludingNullable(para.ParameterInfo.ParameterType))
					{
						if (CanUseFormBodyBinding(action, para))
						{
							para.BindingInfo = BindingInfo.GetBindingInfo(new[] { new FromBodyAttribute() });
						}
					}
				}
		}
	}


	private bool CanUseFormBodyBinding(ActionModel action, ParameterModel parameter)
	{
		if (AppConsts.FormBodyBindingIgnoredTypes.Any(t => t.IsAssignableFrom(parameter.ParameterInfo.ParameterType)))
		{
			return false;
		}

		foreach (var selector in action.Selectors)
		{
			if (selector.ActionConstraints == null)
			{
				continue;
			}

			foreach (var actionConstraint in selector.ActionConstraints)
			{

				var httpMethodActionConstraint = actionConstraint as HttpMethodActionConstraint;
				if (httpMethodActionConstraint == null)
				{
					continue;
				}

				if (httpMethodActionConstraint.HttpMethods.All(hm => hm.IsIn("GET", "DELETE", "TRACE", "HEAD")))
				{
					return false;
				}
			}
		}

		return true;
	}


	#region ConfigureApiExplorer

	private void ConfigureApiExplorer(ControllerModel controller)
	{
		if (controller.ApiExplorer.GroupName.IsNullOrEmpty())
		{
			controller.ApiExplorer.GroupName = controller.ControllerName;
		}

		if (controller.ApiExplorer.IsVisible == null)
		{
			controller.ApiExplorer.IsVisible = true;
		}

		foreach (var action in controller.Actions)
		{
			if (!CheckNoMapMethod(action))
				ConfigureApiExplorer(action);
		}
	}

	private void ConfigureApiExplorer(ActionModel action)
	{
		if (action.ApiExplorer.IsVisible == null)
		{
			action.ApiExplorer.IsVisible = true;
		}
	}

	#endregion
	/// <summary>
	/// //不映射指定的方法
	/// </summary>
	/// <param name="action"></param>
	/// <returns></returns>
	private bool CheckNoMapMethod(ActionModel action)
	{
		bool isExist = false;
		var noMapMethod = ReflectionHelper.GetSingleAttributeOrDefault<NonDynamicMethodAttribute>(action.ActionMethod);

		if (noMapMethod != null)
		{
			action.ApiExplorer.IsVisible = false;//对应的Api不映射
			isExist = true;
		}

		return isExist;
	}

	/// <summary>
	/// 不格式化结果数据
	/// </summary>
	/// <param name="action"></param>
	/// <returns></returns>
	private bool CheckNoFormatResultMethod(ActionModel action)
	{
		bool isExist = false;
		var nonFormatResult = ReflectionHelper.GetSingleAttributeOrDefault<NonFormatResultAttribute>(action.ActionMethod);

		if (nonFormatResult != null)
		{
			isExist = true;
		}

		return isExist;
	}
	private void ConfigureSelector(ControllerModel controller, DynamicApiAttribute controllerAttr)
	{

		if (controller.Selectors.Any(selector => selector.AttributeRouteModel != null))
		{
			return;
		}

		var areaName = string.Empty;

		if (controllerAttr != null)
		{
			areaName = controllerAttr.Area;
		}

		foreach (var action in controller.Actions)
		{
			if (!CheckNoMapMethod(action))
				ConfigureSelector(areaName, controller.ControllerName, action);
		}
	}

	private void ConfigureSelector(string areaName, string controllerName, ActionModel action)
	{

		var nonAttr = ReflectionHelper.GetSingleAttributeOrDefault<NonDynamicApiAttribute>(action.ActionMethod);

		if (nonAttr != null)
		{
			return;
		}

		if (action.Selectors.IsNullOrEmpty() || action.Selectors.Any(a => a.ActionConstraints.IsNullOrEmpty()))
		{
			if (!CheckNoMapMethod(action))
				AddAppServiceSelector(areaName, controllerName, action);
		}
		else
		{
			NormalizeSelectorRoutes(areaName, controllerName, action);
		}
	}

	private void AddAppServiceSelector(string areaName, string controllerName, ActionModel action)
	{

		var verb = GetHttpVerb(action);
		action.ActionName = ResolveActionName(action.ActionName);

		var appServiceSelectorModel = action.Selectors[0];

		if (appServiceSelectorModel.AttributeRouteModel == null)
		{
			appServiceSelectorModel.AttributeRouteModel = CreateActionRouteModel(areaName, controllerName, action);
		}

		if (!appServiceSelectorModel.ActionConstraints.Any())
		{
			appServiceSelectorModel.ActionConstraints.Add(new HttpMethodActionConstraint(new[] { verb }));

			// 让 EndpointMetadata 带上对应的 Http* 特性，小写形式的动词(如 PATCH)同样支持，
			// 否则 ApiExplorer 会因为缺少元数据而无法正确生成 OpenAPI 文档。
			appServiceSelectorModel.EndpointMetadata.Add(CreateHttpMethodAttribute(verb));
		}
	}

	private static HttpMethodAttribute CreateHttpMethodAttribute(string verb)
	{
		return verb switch
		{
			"GET" => new HttpGetAttribute(),
			"POST" => new HttpPostAttribute(),
			"PUT" => new HttpPutAttribute(),
			"DELETE" => new HttpDeleteAttribute(),
			"PATCH" => new HttpPatchAttribute(),
			"HEAD" => new HttpHeadAttribute(),
			"OPTIONS" => new HttpOptionsAttribute(),
			_ => throw new NotSupportedException(
				$"Unsupported http verb: '{verb}'. Supported verbs are GET/POST/PUT/DELETE/PATCH/HEAD/OPTIONS.")
		};
	}



	/// <summary>
	/// Processing action name
	/// </summary>
	/// <param name="actionName"></param>
	/// <returns></returns>
	/// <summary>
	/// 统一解析 action 名称：先剥离配置的后缀(如 Async)，再按命名约定格式化。
	/// </summary>
	/// <remarks>
	/// 原实现在非 Custom 约定分支里直接调用 GetFormatName，绕过了后缀剥离，
	/// 导致 RemoveActionPostfixes 配置的 "Async" 在默认 KebabCase 下完全失效
	/// （GetAsync 会被路由成 get-async 而不是 get）。
	/// </remarks>
	private static string ResolveActionName(string actionName)
	{
		if (string.IsNullOrEmpty(actionName))
		{
			return actionName;
		}

		// Custom 约定走 GetRestFulActionName，其内部已包含后缀剥离
		if (AppConsts.NamingConvention == NamingConventionEnum.Custom)
		{
			return GetRestFulActionName(actionName);
		}

		var postfixes = AppConsts.ActionPostfixes;
		if (postfixes is { Count: > 0 })
		{
			actionName = actionName.RemovePostFix(postfixes.ToArray());
		}

		return GetFormatName(actionName, AppConsts.NamingConvention);
	}

	private static string GetRestFulActionName(string actionName)
	{
		// custom process action name
		var appConstsActionName = AppConsts.GetRestFulActionName?.Invoke(actionName);
		if (appConstsActionName != null)
		{
			return appConstsActionName;
		}

		// default process action name.

		// Remove Postfix
		actionName = actionName.RemovePostFix(AppConsts.ActionPostfixes.ToArray());

		// Remove Prefix
		var verbKey = actionName.GetPascalOrCamelCaseFirstWord().ToLower();
		if (AppConsts.HttpVerbs.ContainsKey(verbKey))
		{
			if (actionName.Length == verbKey.Length)
			{
				return "";
			}
			else
			{
				return actionName.Substring(verbKey.Length);
			}
		}
		else
		{
			return actionName;
		}
	}

	private static string GetRestFulControllerName(string controllerName)
	{
		// custom process action name
		var appConstsControllerName = AppConsts.GetRestFulControllerName?.Invoke(controllerName);
		if (appConstsControllerName != null)
		{
			return appConstsControllerName;
		}
		else
		{
			return controllerName;
		}
	}

	private void NormalizeSelectorRoutes(string areaName, string controllerName, ActionModel action)
	{
		action.ActionName = ResolveActionName(action.ActionName);

		foreach (var selector in action.Selectors)
		{
			selector.AttributeRouteModel = selector.AttributeRouteModel == null ?
				 CreateActionRouteModel(areaName, controllerName, action) :
				 AttributeRouteModel.CombineAttributeRouteModel(CreateActionRouteModel(areaName, controllerName, action), selector.AttributeRouteModel);
		}
	}

	private static string GetHttpVerb(ActionModel action)
	{
		var getValueSuccess = AppConsts.AssemblyDynamicApiOptions
			.TryGetValue(action.Controller.ControllerType.Assembly, out AssemblyDynamicApiOptions assemblyDynamicApiOptions);
		if (getValueSuccess && !string.IsNullOrWhiteSpace(assemblyDynamicApiOptions?.HttpVerb))
		{
			return assemblyDynamicApiOptions.HttpVerb;
		}


		var verbKey = action.ActionName.GetPascalOrCamelCaseFirstWord().ToLower();

		var verb = AppConsts.HttpVerbs.ContainsKey(verbKey) ? AppConsts.HttpVerbs[verbKey] : AppConsts.DefaultHttpVerb;
		return verb;
	}

	private AttributeRouteModel CreateActionRouteModel(string areaName, string controllerName, ActionModel action)
	{
		var route = _actionRouteFactory.CreateActionRouteModel(areaName, controllerName, action);

		return new AttributeRouteModel(new RouteAttribute(route));
	}
}