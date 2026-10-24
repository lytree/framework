using System;
using Microsoft.CodeAnalysis;

namespace Framework.DynamicApi.Generator;

/// <summary>生成代码中参数的取值来源。</summary>
internal enum ParameterKind
{
	Query,
	Route,
	Body,
	Services,
	HttpContext,
	HttpRequest,
	HttpResponse,
	ClaimsPrincipal,
	CancellationToken,
}

/// <summary>生成代码中端点的返回形态。</summary>
internal enum ReturnKind
{
	Void,
	Payload,
	WrappedPayload,
	IResult,
}

/// <summary>一个待暴露为 Minimal API 端点的参数。</summary>
internal sealed record ParameterModel(
	string TypeName,
	string Name,
	ParameterKind Kind,
	bool IsNullable,
	bool IsValueType);

/// <summary>一个待暴露为 Minimal API 端点的方法。</summary>
internal sealed record MethodModel(
	string Name,
	string Verb,
	ReturnKind Return,
	string? PayloadTypeName,
	bool ReturnsTaskLike,
	EquatableArray<ParameterModel> Parameters,
	Location? Location = null);

/// <summary>生成期诊断信息，延迟到输出阶段统一上报。</summary>
internal sealed record DiagnosticInfo(DiagnosticDescriptor Descriptor, string Message, Location? Location);

/// <summary>由 MSBuild 属性 <c>DynamicApiModules</c> 声明的跨程序集模块解析结果。</summary>
internal sealed record ModuleListModel(
	EquatableArray<string> Names,
	EquatableArray<DiagnosticInfo> Diagnostics);

/// <summary>一个 <c>[DynamicApi]</c> 服务类的端点描述。</summary>
internal sealed record ApiClassModel(
	string TypeName,
	string FullTypeName,
	string ClassId,
	string? Area,
	EquatableArray<string> Groups,
	EquatableArray<MethodModel> Methods,
	EquatableArray<DiagnosticInfo> Diagnostics,
	bool Skip = false);
