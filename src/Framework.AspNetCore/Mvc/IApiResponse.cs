using System;
using System.Runtime.CompilerServices;

namespace Framework.AspNetCore.Mvc;


/// <summary>
/// 携带业务数据载荷的 API 响应契约。
/// <para>在 <see cref="IApiResponse"/> 的基础上扩展一个强类型的 <see cref="Data"/> 字段，
/// 由 <c>ApiResponse<T></c> 等具体实现类型实现，便于序列化与按字段反序列化。</para>
/// </summary>
/// <typeparam name="T">业务数据载荷的类型。</typeparam>
public interface IApiResponse<T> : IApiResponse
{
	/// <summary>
	/// 业务数据载荷。
	/// <para>由调用方按业务需求赋值；未设置时保持 <typeparamref name="T"/> 的默认值。</para>
	/// </summary>
	T Data { get; set; }
}

/// <summary>
/// 统一的 API 响应契约（不带业务数据）。
/// <para>任何返回给前端的响应都应至少暴露这些基础字段，
/// 以便客户端按统一结构解析 HTTP 状态、业务成功位、业务码与消息。</para>
/// </summary>
public interface IApiResponse
{
	/// <summary>
	/// HTTP 状态码。
	/// <para>建议与实际响应行保持一致（如 200/400/401/403/404/500 等）。</para>
	/// </summary>
	int StatusCode { get; set; }
	/// <summary>
	/// 业务处理是否成功。
	/// <para><c>true</c> 表示成功响应，<c>false</c> 表示业务失败或异常。</para>
	/// </summary>
	bool Success { get; set; }
	/// <summary>
	/// 业务码。
	/// <para>约定成功使用 <c>"0000"</c>，失败使用业务方自定义错误码。</para>
	/// </summary>
	string Code { get; set; }
	/// <summary>
	/// 面向调用方的描述性消息。
	/// <para>失败时通常填写错误说明；成功时可为空串或"Ok"。</para>
	/// </summary>
	string Message { get; set; }
}
