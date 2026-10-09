using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Framework.DynamicApi.Attributes;

/// <summary>
/// 标注在动态 API Action 上，用于声明该 Action 的响应会被统一包装为 <see cref="AppConsts.FormatResultType"/>（默认 <c>ResponseResult&lt;T&gt;</c>）。
/// 同时继承自 <see cref="ProducesResponseTypeAttribute"/>，可被 Swagger 等 API 元数据消费者识别为对应的状态码与负载类型。
/// 允许多个特性叠加以便同时声明多个可能的返回（200、400 等）。
/// </summary>
[Serializable]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class FormatResultAttribute : ProducesResponseTypeAttribute
{
	/// <summary>
	/// 仅声明 HTTP 状态码的构造函数，不指定负载类型（用于 void 返回或不需要包装负载模型的场景）。
	/// </summary>
	/// <param name="statusCode">HTTP 响应状态码，例如 200、201。</param>
	public FormatResultAttribute(int statusCode) : base(statusCode)
	{
	}

	/// <summary>
	/// 声明负载类型，默认状态码为 200 OK。
	/// 内部会自动用 <see cref="AppConsts.FormatResultType"/> 把 <paramref name="type"/> 包装为统一响应结构。
	/// </summary>
	/// <param name="type">Action 实际返回的负载类型，可为 <c>null</c> 或 <see cref="void"/> 表示不包装。</param>
	public FormatResultAttribute(Type type) : base(type, StatusCodes.Status200OK)
	{
		FormatType(type);
	}

	/// <summary>
	/// 同时声明负载类型与 HTTP 状态码的构造函数。
	/// 内部会自动用 <see cref="AppConsts.FormatResultType"/> 把 <paramref name="type"/> 包装为统一响应结构。
	/// </summary>
	/// <param name="type">Action 实际返回的负载类型，可为 <c>null</c> 或 <see cref="void"/>。</param>
	/// <param name="statusCode">HTTP 响应状态码。</param>
	public FormatResultAttribute(Type type, int statusCode) : base(type, statusCode)
	{
		FormatType(type);
	}

	// 将原始负载类型包装为统一的 ResponseResult<T>，便于 Swagger 输出统一的响应结构
	private void FormatType(Type type)
	{
		if (type != null && type != typeof(void))
		{
			Type = AppConsts.FormatResultType.MakeGenericType(type);
		}
	}
}