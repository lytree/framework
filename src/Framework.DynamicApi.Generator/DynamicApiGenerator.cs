using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Framework.DynamicApi.Generator;

/// <summary>
/// 把 <c>[DynamicApi]</c> 服务类编译期生成为 Minimal API 端点注册代码。
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class DynamicApiGenerator : IIncrementalGenerator
{
	private const string AttributeMetadataName = "Framework.DynamicApi.Attributes.DynamicApiAttribute";
	private const string ModulesPropertyName = "build_property.DynamicApiModules";

	/// <inheritdoc />
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var models = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				AttributeMetadataName,
				static (node, _) => node is ClassDeclarationSyntax,
				static (ctx, ct) => ModelBuilder.Build(ctx, ct))
			.Where(static model => model is not null)
			.Select(static (model, _) => model!);

		var assemblyName = context.CompilationProvider.Select(static (compilation, _) => compilation.AssemblyName ?? "Assembly");

		var modules = context.AnalyzerConfigOptionsProvider
			.Select(static (provider, _) => ReadModuleNames(provider))
			.Combine(context.CompilationProvider)
			.Select(static (input, _) => ResolveModules(input.Right, input.Left));

		context.RegisterSourceOutput(models.Collect().Combine(assemblyName).Combine(modules), static (context, input) =>
		{
			Emit(context, input.Left.Left, input.Left.Right, input.Right);
		});
	}

	private static EquatableArray<string> ReadModuleNames(AnalyzerConfigOptionsProvider provider)
	{
		if (!provider.GlobalOptions.TryGetValue(ModulesPropertyName, out var raw) || string.IsNullOrWhiteSpace(raw))
			return EquatableArray<string>.Empty;

		var names = raw
			.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
			.Select(name => name.Trim())
			.Where(name => name.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		return new EquatableArray<string>(names);
	}

	private static ModuleListModel ResolveModules(Compilation compilation, EquatableArray<string> moduleNames)
	{
		var valid = new List<string>();
		var diagnostics = new List<DiagnosticInfo>();

		foreach (var moduleName in moduleNames)
		{
			var metadataName = $"Framework.DynamicApi.Generated.{ModelBuilder.Sanitize(moduleName)}.DynamicApiEndpoints";

			if (compilation.GetTypeByMetadataName(metadataName) is null)
			{
				diagnostics.Add(new DiagnosticInfo(
					DiagnosticDescriptors.MissingModuleEntry,
					$"未找到程序集 '{moduleName}' 的动态 API 入口类型 '{metadataName}'；请确认该程序集已引用生成器并包含 [DynamicApi] 服务类。",
					null));

				continue;
			}

			valid.Add(moduleName);
		}

		return new ModuleListModel(
			new EquatableArray<string>(valid.ToArray()),
			new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()));
	}

	/// <summary>
	/// 输出各服务类的端点映射源与程序集级入口。
	/// 生成类型名撞名的服务类只保留第一个，其余报 <c>YLDAPI007</c> 后跳过，避免端点静默丢失。
	/// </summary>
	private static void Emit(
		SourceProductionContext context,
		ImmutableArray<ApiClassModel> models,
		string assemblyName,
		ModuleListModel modules)
	{
		var emitted = new List<ApiClassModel>();
		var seen = new HashSet<string>(StringComparer.Ordinal);
		var routeKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach (var model in models)
		{
			foreach (var diagnostic in model.Diagnostics)
			{
				context.ReportDiagnostic(Diagnostic.Create(
					diagnostic.Descriptor,
					diagnostic.Location ?? Location.None,
					diagnostic.Message));
			}

			if (model.Skip || model.Methods.Count == 0)
				continue;

			if (!seen.Add(model.ClassId))
			{
				// ClassId 由完全限定名净化得到，A.B.Svc 与 A_B.Svc 会撞名。
				// 撞名后不能复用同一个生成类型，只能跳过；必须显式报错，避免端点静默丢失。
				context.ReportDiagnostic(Diagnostic.Create(
					DiagnosticDescriptors.DuplicateGeneratedType,
					model.Methods[0].Location ?? Location.None,
					$"服务类 '{model.FullTypeName}' 生成的类型名 '{model.ClassId}' 已被同程序集内另一个服务类占用，该服务类的端点已跳过；请重命名类型或其命名空间。"));

				continue;
			}

			foreach (var method in model.Methods)
			{
				var key = $"{model.TypeName}/{method.Name}/{method.Verb}";
				var display = $"{model.FullTypeName}.{method.Name}";

				if (routeKeys.TryGetValue(key, out var first))
				{
					context.ReportDiagnostic(Diagnostic.Create(
						DiagnosticDescriptors.DuplicateRoute,
						method.Location ?? Location.None,
						$"端点 '{display}' 与 '{first}' 的控制器名、方法名和 HTTP 动词相同，运行时生成的路由模板可能重复。"));
				}
				else
				{
					routeKeys[key] = display;
				}
			}

			context.AddSource(
				$"{model.ClassId}.cs",
				SourceText.From(EndpointEmitter.RenderClass(model), Encoding.UTF8));

			emitted.Add(model);
		}

		foreach (var diagnostic in modules.Diagnostics)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				diagnostic.Descriptor,
				diagnostic.Location ?? Location.None,
				diagnostic.Message));
		}

		if (emitted.Count == 0 && modules.Names.Count == 0)
			return;

		context.AddSource(
			$"DynamicApiEndpoints_{ModelBuilder.Sanitize(assemblyName)}.cs",
			SourceText.From(EndpointEmitter.RenderAssemblyEntry(assemblyName, emitted, modules.Names), Encoding.UTF8));
	}
}
