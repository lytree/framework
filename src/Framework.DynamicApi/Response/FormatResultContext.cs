using System;

namespace Framework.DynamicApi.Response;

/// <summary>
/// 动态 API 结果包装类型上下文。
/// 集中保存用于统一包装 API 响应的开放泛型类型，默认为 <see cref="ResponseResul{T}"/>。
/// 通过集中配置可在启动时替换为项目自定义的统一响应结构。
/// </summary>
public static class FormatResultContext
{
	/// <summary>
	/// 用于包装动态 API 结果的开放泛型类型，例如 <c>typeof(ResponseResul&lt;&gt;)</c>。
	/// 替换为自定义类型时，应保证该类型接受一个泛型参数作为负载类型。
	/// </summary>
	internal static Type FormatResultType = typeof(ResponseResul<>);
}