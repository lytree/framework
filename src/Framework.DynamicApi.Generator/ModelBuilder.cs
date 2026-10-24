using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Framework.DynamicApi.Generator;

/// <summary>
/// 从编译期符号中抽取动态 API 端点模型。
/// </summary>
internal static class ModelBuilder
{
	private const string DynamicApiInterface = "Framework.DynamicApi.IDynamicApi";
	private const string NonDynamicApiAttribute = "Framework.DynamicApi.Attributes.NonDynamicApiAttribute";
	private const string NonDynamicMethodAttribute = "Framework.DynamicApi.Attributes.NonDynamicMethodAttribute";
	private const string NonFormatResultAttribute = "Framework.DynamicApi.Attributes.NonFormatResultAttribute";

	private static readonly string[] MvcAttributeNames =
	[
		"RouteAttribute", "ApiControllerAttribute", "HttpGetAttribute", "HttpPostAttribute",
		"HttpPutAttribute", "HttpDeleteAttribute", "HttpPatchAttribute", "HttpHeadAttribute",
		"HttpOptionsAttribute"
	];

	private static readonly Dictionary<string, ParameterKind?> BindingAttributes = new(StringComparer.Ordinal)
	{
		["FromBodyAttribute"] = ParameterKind.Body,
		["FromQueryAttribute"] = ParameterKind.Query,
		["FromRouteAttribute"] = ParameterKind.Route,
		["FromServicesAttribute"] = ParameterKind.Services,
		["FromFormAttribute"] = null,
		["AsParametersAttribute"] = null,
	};

	private static readonly Dictionary<string, ParameterKind> SpecialParameterTypes = new(StringComparer.Ordinal)
	{
		["System.Threading.CancellationToken"] = ParameterKind.CancellationToken,
		["Microsoft.AspNetCore.Http.HttpContext"] = ParameterKind.HttpContext,
		["Microsoft.AspNetCore.Http.HttpRequest"] = ParameterKind.HttpRequest,
		["Microsoft.AspNetCore.Http.HttpResponse"] = ParameterKind.HttpResponse,
		["System.Security.Claims.ClaimsPrincipal"] = ParameterKind.ClaimsPrincipal,
	};

	private static readonly HashSet<string> UnsupportedParameterTypes = new(StringComparer.Ordinal)
	{
		"Microsoft.AspNetCore.Http.IFormFile",
		"Microsoft.AspNetCore.Http.IFormFileCollection",
		"System.IO.Stream",
	};

	/// <summary>与方法名首个单词匹配的 HTTP 动词，与运行时 <c>DynamicApiRouteResolver.DefaultHttpVerbs</c> 保持一致。</summary>
	private static readonly Dictionary<string, string> VerbMap = new(StringComparer.OrdinalIgnoreCase)
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

	private static readonly Regex WordBoundary = new(
		@"(?=\p{Lu}\p{Ll})|(?<=\p{Ll})(?=\p{Lu})",
		RegexOptions.CultureInvariant | RegexOptions.Compiled);

	internal static ApiClassModel? Build(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		if (context.TargetSymbol is not INamedTypeSymbol type)
			return null;

		if (type.TypeKind != TypeKind.Class || type.IsAbstract || type.IsStatic || type.IsGenericType)
			return null;

		if (type.DeclaredAccessibility != Accessibility.Public)
			return null;

		if (!type.AllInterfaces.Any(i => i.ToDisplayString() == DynamicApiInterface))
			return null;

		if (HasAttribute(type, NonDynamicApiAttribute))
			return null;

		var diagnostics = new List<DiagnosticInfo>();
		var typeLocation = type.Locations.FirstOrDefault();

		if (type.GetAttributes().Any(a => a.AttributeClass is { } c && MvcAttributeNames.Contains(c.Name)))
		{
			diagnostics.Add(new DiagnosticInfo(
				DiagnosticDescriptors.MvcAttributesIgnored,
				$"类型 '{type.ToDisplayString()}' 声明了 MVC 路由特性，已跳过该类型的动态 API 生成。",
				typeLocation));

			return new ApiClassModel(
				type.Name,
				type.ToDisplayString(),
				"DynamicApi_" + Sanitize(type.ToDisplayString()),
				null,
				EquatableArray<string>.Empty,
				EquatableArray<MethodModel>.Empty,
				new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()),
				Skip: true);
		}

		var area = GetNamedString(type, "Framework.DynamicApi.Attributes.DynamicApiAttribute", "Area");
		var groups = GetNamedStrings(type, "Framework.DynamicApi.Attributes.DynamicApiAttribute", "GroupNames");

		var methods = new List<MethodModel>();

		foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (method.MethodKind != MethodKind.Ordinary ||
				method.DeclaredAccessibility != Accessibility.Public ||
				method.IsStatic ||
				method.IsGenericMethod ||
				method.IsOverride ||
				method.IsImplicitlyDeclared)
				continue;

			if (HasAttribute(method, NonDynamicMethodAttribute) || HasAttribute(method, NonDynamicApiAttribute))
				continue;

			if (method.GetAttributes().Any(a => a.AttributeClass is { } c && MvcAttributeNames.Contains(c.Name)))
			{
				diagnostics.Add(new DiagnosticInfo(
					DiagnosticDescriptors.MvcAttributesIgnored,
					$"方法 '{type.ToDisplayString()}.{method.Name}' 声明了 MVC 路由特性，已跳过该端点的生成。",
					method.Locations.FirstOrDefault()));
				continue;
			}

			var verb = ResolveVerb(method.Name);
			var parameters = new List<ParameterModel>();
			var unsupported = false;

			foreach (var parameter in method.Parameters)
			{
				if (TryBuildParameter(parameter, verb, out var parameterModel, out var error))
				{
					parameters.Add(parameterModel!);
					continue;
				}

				diagnostics.Add(new DiagnosticInfo(
					error!.Value.Descriptor,
					$"方法 '{type.ToDisplayString()}.{method.Name}' 的参数 '{parameter.Name}'：{error.Value.Message}该端点已跳过。",
					parameter.Locations.FirstOrDefault()));

				unsupported = true;
				break;
			}

			if (unsupported)
				continue;

			if (!TryBuildReturn(method, out var returnKind, out var payloadType, out var returnsTaskLike, out var returnError))
			{
				diagnostics.Add(new DiagnosticInfo(
					DiagnosticDescriptors.UnsupportedMember,
					$"方法 '{type.ToDisplayString()}.{method.Name}'：{returnError}该端点已跳过。",
					method.Locations.FirstOrDefault()));
				continue;
			}

			methods.Add(new MethodModel(
				method.Name,
				verb,
				returnKind,
				payloadType,
				returnsTaskLike,
				new EquatableArray<ParameterModel>(parameters.ToArray()),
				method.Locations.FirstOrDefault()));
		}

		if (methods.Count == 0 && diagnostics.Count == 0)
			return null;

		return new ApiClassModel(
			type.Name,
			type.ToDisplayString(),
			"DynamicApi_" + Sanitize(type.ToDisplayString()),
			area,
			new EquatableArray<string>(groups),
			new EquatableArray<MethodModel>(methods.ToArray()),
			new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()),
			Skip: methods.Count == 0);
	}

	private static bool TryBuildParameter(
		IParameterSymbol parameter,
		string verb,
		out ParameterModel? model,
		out (DiagnosticDescriptor Descriptor, string Message)? error)
	{
		model = null;
		error = null;

		var displayName = parameter.Type.ToDisplayString();
		var isValueType = parameter.Type.IsValueType;
		var isNullable = isValueType
			? parameter.Type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T }
			: parameter.NullableAnnotation == NullableAnnotation.Annotated;

		var typeName = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

		if (isValueType && isNullable && typeName.EndsWith("?", StringComparison.Ordinal))
			typeName = typeName.Substring(0, typeName.Length - 1);

		if (UnsupportedParameterTypes.Contains(displayName))
		{
			error = (DiagnosticDescriptors.UnsupportedMember, "文件上传/流参数暂不支持生成式绑定；请改用专门的端点。");
			return false;
		}

		if (SpecialParameterTypes.TryGetValue(displayName, out var specialKind))
		{
			model = new ParameterModel(typeName, EscapeIdentifier(parameter.Name), specialKind, false, false);
			return true;
		}

		var bindingAttribute = parameter.GetAttributes()
			.FirstOrDefault(a => a.AttributeClass is { } c && BindingAttributes.ContainsKey(c.Name));

		if (bindingAttribute?.AttributeClass is { } bindingClass)
		{
			var kind = BindingAttributes[bindingClass.Name];

			if (kind is null)
			{
				error = (DiagnosticDescriptors.UnsupportedMember, "暂不支持 [FromForm] / [AsParameters] 参数。");
				return false;
			}

			if ((kind == ParameterKind.Query || kind == ParameterKind.Route) && !IsSimpleType(parameter.Type))
			{
				error = (DiagnosticDescriptors.UnsupportedMember, "复杂类型无法通过 [FromQuery] / [FromRoute] 绑定。");
				return false;
			}

			model = new ParameterModel(typeName, EscapeIdentifier(parameter.Name), kind.Value, isNullable, isValueType);
			return true;
		}

		if (IsSimpleType(parameter.Type))
		{
			model = new ParameterModel(typeName, EscapeIdentifier(parameter.Name), ParameterKind.Query, isNullable, isValueType);
			return true;
		}

		if (verb is "GET" or "DELETE" or "HEAD" or "TRACE" or "OPTIONS")
		{
			error = (DiagnosticDescriptors.UnsupportedComplexParameter, $"复杂类型无法在 {verb} 请求中绑定，请显式标注 [FromQuery]/[AsParameters] 或改用 POST。");
			return false;
		}

		model = new ParameterModel(typeName, EscapeIdentifier(parameter.Name), ParameterKind.Body, isNullable, isValueType);
		return true;
	}

	private static bool TryBuildReturn(
		IMethodSymbol method,
		out ReturnKind returnKind,
		out string? payloadTypeName,
		out bool returnsTaskLike,
		out string? error)
	{
		returnKind = ReturnKind.Void;
		payloadTypeName = null;
		returnsTaskLike = false;
		error = null;

		var returnType = method.ReturnType;

		if (returnType.SpecialType == SpecialType.System_Void)
			return true;

		var candidate = returnType;

		if (returnType is INamedTypeSymbol named &&
			named.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks" &&
			(named.Name is "Task" or "ValueTask"))
		{
			returnsTaskLike = true;

			if (named.TypeArguments.Length == 0)
				return true;

			candidate = named.TypeArguments[0];
		}

		if (Implements(candidate, "Microsoft.AspNetCore.Http.IResult"))
		{
			returnKind = ReturnKind.IResult;
			return true;
		}

		if (Implements(candidate, "Microsoft.AspNetCore.Mvc.IActionResult"))
		{
			error = "返回 IActionResult 需要 MVC 执行器，生成式 Minimal API 暂不支持。";
			return false;
		}

		payloadTypeName = candidate.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		returnKind = HasAttribute(method, NonFormatResultAttribute) ? ReturnKind.Payload : ReturnKind.WrappedPayload;

		return true;
	}

	private static string ResolveVerb(string methodName)
	{
		var firstWord = FirstWord(methodName).ToLowerInvariant();

		return VerbMap.TryGetValue(firstWord, out var verb) ? verb : "POST";
	}

	private static string FirstWord(string value)
	{
		if (value.Length <= 1)
			return value;

		return WordBoundary.Split(value).FirstOrDefault(part => part.Length > 0) ?? value;
	}

	private static bool Implements(ITypeSymbol type, string interfaceName)
	{
		if (type.ToDisplayString() == interfaceName)
			return true;

		return type.AllInterfaces.Any(i => i.ToDisplayString() == interfaceName);
	}

	private static bool IsSimpleType(ITypeSymbol type)
	{
		if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
			type = nullable.TypeArguments[0];

		switch (type.SpecialType)
		{
			case SpecialType.System_Boolean:
			case SpecialType.System_Char:
			case SpecialType.System_SByte:
			case SpecialType.System_Byte:
			case SpecialType.System_Int16:
			case SpecialType.System_UInt16:
			case SpecialType.System_Int32:
			case SpecialType.System_UInt32:
			case SpecialType.System_Int64:
			case SpecialType.System_UInt64:
			case SpecialType.System_Decimal:
			case SpecialType.System_Single:
			case SpecialType.System_Double:
			case SpecialType.System_String:
			case SpecialType.System_DateTime:
				return true;
		}

		var ns = type.ContainingNamespace?.ToDisplayString();

		return ns == "System" && type.Name is "Guid" or "TimeSpan" or "DateTimeOffset";
	}

	private static bool HasAttribute(ISymbol symbol, string attributeMetadataName)
	{
		return symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == attributeMetadataName);
	}

	private static string? GetNamedString(ISymbol symbol, string attributeMetadataName, string argumentName)
	{
		var attribute = symbol.GetAttributes()
			.FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == attributeMetadataName);

		if (attribute is null)
			return null;

		foreach (var argument in attribute.NamedArguments)
		{
			if (argument.Key == argumentName && argument.Value.Value is string value)
				return value;
		}

		return null;
	}

	private static string[] GetNamedStrings(ISymbol symbol, string attributeMetadataName, string argumentName)
	{
		var attribute = symbol.GetAttributes()
			.FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == attributeMetadataName);

		if (attribute is null)
			return Array.Empty<string>();

		foreach (var argument in attribute.NamedArguments)
		{
			if (argument.Key != argumentName || argument.Value.Kind != TypedConstantKind.Array)
				continue;

			return argument.Value.Values
				.Where(v => v.Value is string)
				.Select(v => (string)v.Value!)
				.ToArray();
		}

		return Array.Empty<string>();
	}

	private static string EscapeIdentifier(string name)
	{
		return Microsoft.CodeAnalysis.CSharp.SyntaxFacts.GetKeywordKind(name) != Microsoft.CodeAnalysis.CSharp.SyntaxKind.None
			? "@" + name
			: name;
	}

	internal static string Sanitize(string value)
	{
		var builder = new System.Text.StringBuilder(value.Length);

		foreach (var ch in value)
		{
			builder.Append(char.IsLetterOrDigit(ch) ? ch : '_');
		}

		return builder.ToString();
	}
}
