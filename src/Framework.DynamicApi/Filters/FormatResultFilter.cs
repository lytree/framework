using System.Threading.Tasks;
using Framework.DynamicApi.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Framework.DynamicApi.Filters;

/// <summary>
/// 把 Minimal API handler 的返回值包装为统一响应结构 <see cref="ResponseResul{T}"/>。
/// </summary>
/// <remarks>
/// 以实例方式注册（<c>.AddEndpointFilter(new FormatResultFilter&lt;T&gt;())</c>），
/// 避免 <c>ActivatorUtilities</c> 反射激活与开放泛型 <c>MakeGenericType</c>，可在 AOT 下使用。
/// 已经是 <see cref="IResult"/> / <see cref="IActionResult"/> / <see cref="ResponseResul{T}"/> 的返回值原样透传。
/// </remarks>
/// <typeparam name="T">业务负载类型。</typeparam>
public sealed class FormatResultFilter<T> : IEndpointFilter
{
	/// <inheritdoc />
	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		var result = await next(context);

		return Format(result);
	}

	private static object? Format(object? result)
	{
		if (result is null)
		{
			return Results.Ok(new ResponseResul<T>(default!));
		}

		if (result is IResult or IActionResult or ResponseResul<T>)
		{
			return result;
		}

		return result is T value ? Results.Ok(new ResponseResul<T>(value)) : result;
	}
}
