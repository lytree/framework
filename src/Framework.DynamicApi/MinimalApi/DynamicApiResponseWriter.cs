using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Framework.DynamicApi.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Framework.DynamicApi.MinimalApi;

/// <summary>
/// 生成代码使用的响应写入器。
/// 通过 <see cref="JsonSerializerOptions.GetTypeInfo(System.Type)"/> 取得类型信息后写入 JSON，
/// 避免 <c>JsonSerializer</c> 的反射路径，可在 AOT 下工作。
/// </summary>
public static class DynamicApiResponseWriter
{
	/// <summary>
	/// 以 200 状态码写入原始负载。
	/// </summary>
	public static async Task WriteAsync<TValue>(HttpContext context, TValue value)
	{
		var typeInfo = (JsonTypeInfo<TValue>)ResolveOptions(context).GetTypeInfo(typeof(TValue));

		context.Response.StatusCode = StatusCodes.Status200OK;
		await context.Response.WriteAsJsonAsync(value, typeInfo, "application/json; charset=utf-8", context.RequestAborted);
	}

	/// <summary>
	/// 以 200 状态码写入被 <see cref="ResponseResul{T}"/> 包装后的负载。
	/// </summary>
	public static async Task WriteWrappedAsync<TValue>(HttpContext context, TValue value)
	{
		var typeInfo = (JsonTypeInfo<ResponseResul<TValue>>)ResolveOptions(context).GetTypeInfo(typeof(ResponseResul<TValue>));

		context.Response.StatusCode = StatusCodes.Status200OK;
		await context.Response.WriteAsJsonAsync(new ResponseResul<TValue>(value), typeInfo, "application/json; charset=utf-8", context.RequestAborted);
	}

	/// <summary>
	/// 写入不带负载的 200 响应。
	/// </summary>
	public static void WriteEmpty(HttpContext context)
	{
		context.Response.StatusCode = StatusCodes.Status200OK;
	}

	/// <summary>
	/// 写入 400 响应，说明哪个参数缺失或格式非法。
	/// </summary>
	public static Task WriteBadRequestAsync(HttpContext context, string parameterName)
	{
		context.Response.StatusCode = StatusCodes.Status400BadRequest;

		return context.Response.WriteAsync($"Required parameter '{parameterName}' was not provided or is invalid.");
	}

	private static JsonSerializerOptions ResolveOptions(HttpContext context)
	{
		return context.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
	}
}
