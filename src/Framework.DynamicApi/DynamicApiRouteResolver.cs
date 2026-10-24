using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Framework.DynamicApi.Enums;
using Framework.DynamicApi.Helpers;

namespace Framework.DynamicApi;

/// <summary>
/// 动态 API 的路由解析器。
/// 负责把控制器名 / Action 名按命名约定转换为路由段，并拼接最终路径。
/// 全部为纯字符串运算，不涉及反射，供编译期生成的 Minimal API 代码在启动时调用。
/// </summary>
public static partial class DynamicApiRouteResolver
{
	private static readonly string[] EmptyPostfixes = Array.Empty<string>();

	/// <summary>
	/// 默认的“方法名首个单词 → HTTP 动词”映射。
	/// 编译期生成器使用同一份映射推断动词（见 <c>ModelBuilder.VerbMap</c>），修改时需保持两边一致。
	/// </summary>
	public static readonly IReadOnlyDictionary<string, string> DefaultHttpVerbs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["add"] = "POST",
		["create"] = "POST",
		["insert"] = "POST",
		["submit"] = "POST",
		["post"] = "POST",

		["get"] = "GET",
		["find"] = "GET",
		["fetch"] = "GET",
		["query"] = "GET",

		["update"] = "PUT",
		["change"] = "PUT",
		["put"] = "PUT",
		["batch"] = "PUT",
		["patch"] = "PATCH",

		["delete"] = "DELETE",
		["soft"] = "DELETE",
		["remove"] = "DELETE",
		["clear"] = "DELETE",
	};

	/// <summary>
	/// 按命名约定把驼峰/帕斯卡命名拆分为由分隔符连接的小写单词。
	/// </summary>
	/// <param name="value">原始名称，例如 <c>GetUserList</c>。</param>
	/// <param name="namingConvention">命名约定，决定使用 <c>-</c>、<c>_</c> 还是 <c>.</c> 作为分隔符。</param>
	/// <returns>转换后的名称，例如 <c>get-user-list</c>。</returns>
	public static string GetSeparateWords(string value, NamingConventionEnum namingConvention = NamingConventionEnum.KebabCase)
	{
		if (string.IsNullOrWhiteSpace(value))
			return value;

		var separator = namingConvention switch
		{
			NamingConventionEnum.SnakeCase => "_",
			NamingConventionEnum.ExtensionCase => ".",
			_ => "-"
		};

		return SeparateWordsRegex().Replace(value, $"{separator}$1").Trim().ToLowerInvariant();
	}

	// 源生成正则：替代 RegexOptions.Compiled。Compiled 每次调用都要动态 emit 程序集，
	// 而命名转换只在启动期执行一次，Compiled 的预热成本远大于收益。
	[GeneratedRegex("(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])", RegexOptions.CultureInvariant)]
	private static partial Regex SeparateWordsRegex();

	/// <summary>
	/// 按命名约定格式化名称。
	/// KebabCase / SnakeCase / ExtensionCase 走单词拆分，PascalCase / CamelCase 只调整首字母大小写。
	/// </summary>
	/// <param name="value">原始名称。</param>
	/// <param name="namingConvention">命名约定。</param>
	/// <returns>格式化后的名称；未知约定时原样返回。</returns>
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

	/// <summary>
	/// 把服务类名转换为控制器路由段：先剥离配置的后缀，再按命名约定格式化。
	/// <see cref="NamingConventionEnum.Custom"/> 时优先使用 <paramref name="customFormatter"/>，返回 <c>null</c> 则保留剥离后的名称。
	/// </summary>
	/// <param name="controllerName">服务类名，例如 <c>UserService</c>。</param>
	/// <param name="controllerPostfixes">需要剥离的后缀集合，例如 <c>Service</c>、<c>AppService</c>。</param>
	/// <param name="namingConvention">命名约定。</param>
	/// <param name="customFormatter">自定义名称格式化委托，仅在 <see cref="NamingConventionEnum.Custom"/> 下生效。</param>
	/// <returns>控制器路由段，例如 <c>user</c>。</returns>
	public static string FormatControllerName(
		string controllerName,
		ICollection<string> controllerPostfixes = null,
		NamingConventionEnum namingConvention = NamingConventionEnum.KebabCase,
		Func<string, string> customFormatter = null)
	{
		if (string.IsNullOrEmpty(controllerName))
			return controllerName;

		var name = controllerName.RemovePostFix(ToArray(controllerPostfixes));

		if (namingConvention == NamingConventionEnum.Custom)
		{
			return customFormatter?.Invoke(name) ?? name;
		}

		return GetFormatName(name, namingConvention);
	}

	/// <summary>
	/// 把方法名转换为 Action 路由段：先剥离配置的后缀，再按命名约定格式化。
	/// <see cref="NamingConventionEnum.Custom"/> 时优先使用 <paramref name="customFormatter"/>；
	/// 未提供或返回 <c>null</c> 时，剥离后缀后再去掉动词前缀（如 <c>Get</c>）。
	/// </summary>
	/// <param name="actionName">方法名，例如 <c>GetUserAsync</c>。</param>
	/// <param name="actionPostfixes">需要剥离的后缀集合，例如 <c>Async</c>。</param>
	/// <param name="namingConvention">命名约定。</param>
	/// <param name="customFormatter">自定义名称格式化委托，仅在 <see cref="NamingConventionEnum.Custom"/> 下生效。</param>
	/// <param name="httpVerbs">动词映射表，用于识别并剥离方法名开头的动词；为 <c>null</c> 时使用 <see cref="DefaultHttpVerbs"/>。</param>
	/// <returns>Action 路由段，例如 <c>get-user</c>。</returns>
	public static string FormatActionName(
		string actionName,
		ICollection<string> actionPostfixes = null,
		NamingConventionEnum namingConvention = NamingConventionEnum.KebabCase,
		Func<string, string> customFormatter = null,
		IReadOnlyDictionary<string, string> httpVerbs = null)
	{
		if (string.IsNullOrEmpty(actionName))
			return actionName;

		var postfixes = ToArray(actionPostfixes);

		if (namingConvention == NamingConventionEnum.Custom)
		{
			var custom = customFormatter?.Invoke(actionName);
			if (custom != null)
				return custom;

			return StripVerbPrefix(actionName.RemovePostFix(postfixes), httpVerbs ?? DefaultHttpVerbs);
		}

		return GetFormatName(actionName.RemovePostFix(postfixes), namingConvention);
	}

	/// <summary>
	/// 拼接最终路由模板：<c>{apiPrefix}/{areaName}/{controllerName}/{actionName}</c>，并把连续斜杠规范化为单个斜杠。
	/// </summary>
	/// <param name="apiPrefix">接口前缀，例如 <c>api</c>。</param>
	/// <param name="areaName">区域名称，允许为空。</param>
	/// <param name="controllerName">控制器路由段。</param>
	/// <param name="actionName">Action 路由段。</param>
	/// <returns>规范化后的路由模板，例如 <c>api/user/get-list</c>。</returns>
	public static string BuildRoute(string apiPrefix, string areaName, string controllerName, string actionName)
	{
		return $"{apiPrefix}/{areaName}/{controllerName}/{actionName}".Replace("//", "/");
	}

	private static string StripVerbPrefix(string actionName, IReadOnlyDictionary<string, string> httpVerbs)
	{
		if (string.IsNullOrEmpty(actionName))
			return actionName;

		var verbKey = actionName.GetPascalOrCamelCaseFirstWord().ToLowerInvariant();

		if (httpVerbs != null && httpVerbs.ContainsKey(verbKey))
		{
			return actionName.Length == verbKey.Length ? string.Empty : actionName.Substring(verbKey.Length);
		}

		return actionName;
	}

	private static string[] ToArray(ICollection<string> values)
	{
		if (values == null || values.Count == 0)
			return EmptyPostfixes;

		return values as string[] ?? values.ToArray();
	}
}
