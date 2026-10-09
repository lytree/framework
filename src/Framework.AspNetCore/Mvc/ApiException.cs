using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Framework.AspNetCore.Mvc;

/// <summary>
/// 表示一个携带 API 业务码与 HTTP 状态码的异常。
/// <para>用于在控制器或服务层抛出后，由统一异常过滤器捕获并转换为 <see cref="ApiResponse{T}"/> 返回给客户端。</para>
/// </summary>
public sealed class ApiException : Exception
{

	/// <summary>
	/// 面向客户端的友好错误消息。
	/// <para>会随响应体一起返回给调用方；不同于 <see cref="Exception.Message"/>，该字段应避免暴露内部堆栈信息。</para>
	/// </summary>
	public string ApiMessage { get; set; }
	/// <summary>
	/// 业务错误码。
	/// <para>用于客户端按业务码做分支处理（如重新登录、权限不足等），默认值为 <c>null</c>。</para>
	/// </summary>
	public string ApiCode { get; set; }
	/// <summary>
	/// 对应的 HTTP 状态码。
	/// <para>默认值为 <c>200 OK</c>，调用方可根据需要设置为 <c>4xx</c>/<c>5xx</c> 等值。</para>
	/// </summary>
	public int StatusCode { get; set; } = (int)HttpStatusCode.OK;

	/// <summary>
	/// 初始化 <see cref="ApiException"/> 的新实例，所有业务字段均为默认值。
	/// </summary>
	public ApiException()
	{
	}


	/// <summary>
	/// 使用指定的面向客户端消息初始化 <see cref="ApiException"/> 的新实例。
	/// </summary>
	/// <param name="message">同时作为 <see cref="Exception.Message"/> 与 <see cref="ApiMessage"/> 的消息文本。</param>
	public ApiException(string message)
		: base(message)
	{
		ApiMessage = message;
	}

	/// <summary>
	/// 使用指定的面向客户端消息和业务码初始化 <see cref="ApiException"/> 的新实例。
	/// </summary>
	/// <param name="message">同时作为 <see cref="Exception.Message"/> 与 <see cref="ApiMessage"/> 的消息文本。</param>
	/// <param name="code">业务错误码，将写入 <see cref="ApiCode"/>。</param>
	public ApiException(string message, string code)
		: base(message)
	{
		ApiMessage = message;
		ApiCode = code;
	}

	/// <summary>
	/// 使用指定的面向客户端消息、业务码和 HTTP 状态码初始化 <see cref="ApiException"/> 的新实例。
	/// </summary>
	/// <param name="message">同时作为 <see cref="Exception.Message"/> 与 <see cref="ApiMessage"/> 的消息文本。</param>
	/// <param name="code">业务错误码，将写入 <see cref="ApiCode"/>。</param>
	/// <param name="statusCode">HTTP 状态码（如 200、400、500 等），将写入 <see cref="StatusCode"/>。</param>
	public ApiException(string message, string code, int statusCode)
		: base(message)
	{
		ApiMessage = message;
		ApiCode = code;
		StatusCode = statusCode;
	}


	/// <summary>
	/// 使用指定的面向客户端消息和内部异常初始化 <see cref="ApiException"/> 的新实例。
	/// </summary>
	/// <param name="message">同时作为 <see cref="Exception.Message"/> 与 <see cref="ApiMessage"/> 的消息文本。</param>
	/// <param name="innerException">导致当前异常的内部异常，通过 <see cref="Exception.InnerException"/> 暴露。</param>
	public ApiException(string message, Exception innerException)
		: base(message, innerException)
	{
		ApiMessage = message;
	}

	/// <summary>
	/// 使用指定的面向客户端消息、业务码和内部异常初始化 <see cref="ApiException"/> 的新实例。
	/// </summary>
	/// <param name="message">同时作为 <see cref="Exception.Message"/> 与 <see cref="ApiMessage"/> 的消息文本。</param>
	/// <param name="code">业务错误码，将写入 <see cref="ApiCode"/>。</param>
	/// <param name="innerException">导致当前异常的内部异常，通过 <see cref="Exception.InnerException"/> 暴露。</param>
	public ApiException(string message, string code, Exception innerException)
		: base(message, innerException)
	{
		ApiMessage = message;
		ApiCode = code;
	}

	/// <summary>
	/// 使用指定的面向客户端消息、业务码、HTTP 状态码和内部异常初始化 <see cref="ApiException"/> 的新实例。
	/// </summary>
	/// <param name="message">同时作为 <see cref="Exception.Message"/> 与 <see cref="ApiMessage"/> 的消息文本。</param>
	/// <param name="code">业务错误码，将写入 <see cref="ApiCode"/>。</param>
	/// <param name="statusCode">HTTP 状态码（如 200、400、500 等），将写入 <see cref="StatusCode"/>。</param>
	/// <param name="innerException">导致当前异常的内部异常，通过 <see cref="Exception.InnerException"/> 暴露。</param>
	public ApiException(string message, string code, int statusCode, Exception innerException)
		: base(message, innerException)
	{
		ApiMessage = message;
		ApiCode = code;
		StatusCode = statusCode;
	}
}
