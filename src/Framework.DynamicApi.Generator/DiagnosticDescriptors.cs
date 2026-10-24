using Microsoft.CodeAnalysis;

namespace Framework.DynamicApi.Generator;

/// <summary>源生成器使用的诊断描述。</summary>
internal static class DiagnosticDescriptors
{
	public static readonly DiagnosticDescriptor MvcAttributesIgnored = new(
		id: "YLDAPI001",
		title: "已跳过带有 MVC 路由特性的成员",
		messageFormat: "{0}",
		category: "Framework.DynamicApi",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "动态 API 生成器只处理未显式声明 MVC 路由特性的类型与成员，避免与 MVC 约定产生冲突。");

	public static readonly DiagnosticDescriptor UnsupportedComplexParameter = new(
		id: "YLDAPI003",
		title: "复杂类型参数无法在无请求体的 HTTP 动词下绑定",
		messageFormat: "{0}",
		category: "Framework.DynamicApi",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "GET/DELETE/HEAD/TRACE/OPTIONS 请求没有请求体，复杂类型参数需显式标注 [FromQuery] 或 [AsParameters]。");

	public static readonly DiagnosticDescriptor DuplicateRoute = new(
		id: "YLDAPI004",
		title: "检测到可能冲突的动态 API 路由",
		messageFormat: "{0}",
		category: "Framework.DynamicApi",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "同一程序集内两个端点拥有相同的控制器名、方法名与 HTTP 动词时，运行时生成的路由模板会重复。");

	public static readonly DiagnosticDescriptor MissingModuleEntry = new(
		id: "YLDAPI005",
		title: "未找到 DynamicApiModules 声明的模块入口",
		messageFormat: "{0}",
		category: "Framework.DynamicApi",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "DynamicApiModules 中的程序集必须自身引用生成器并生成 DynamicApiEndpoints 入口类型。");

	public static readonly DiagnosticDescriptor UnsupportedMember = new(
		id: "YLDAPI006",
		title: "生成式 Minimal API 不支持的参数或返回类型",
		messageFormat: "{0}",
		category: "Framework.DynamicApi",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "为保持 AOT 兼容，生成代码只支持标量查询/路由绑定、JSON 请求体、[FromServices] 与常用 HttpContext 类型。");

	public static readonly DiagnosticDescriptor DuplicateGeneratedType = new(
		id: "YLDAPI007",
		title: "多个服务类生成了相同的类型名",
		messageFormat: "{0}",
		category: "Framework.DynamicApi",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "生成代码的类型名由服务类的完全限定名替换非字母数字字符得到，例如 A.B.Svc 与 A_B.Svc 会得到同一个名字。"
			+ "冲突时后出现的服务类端点会被跳过，因此以错误级别提示重命名类型或命名空间。");
}
