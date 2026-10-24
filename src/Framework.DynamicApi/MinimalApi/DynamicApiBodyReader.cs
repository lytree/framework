using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Framework.DynamicApi.MinimalApi;

/// <summary>
/// 生成代码使用的请求体读取器。
/// 通过 <see cref="JsonSerializerOptions.GetTypeInfo(System.Type)"/> 获取类型信息，
/// 不使用 <c>ReadFromJsonAsync&lt;T&gt;(HttpRequest)</c> 的反射路径，可在 AOT 下工作；
/// 消费方需要注册 <see cref="System.Text.Json.Serialization.JsonSerializerContext"/> 源生成上下文。
/// </summary>
public static class DynamicApiBodyReader
{
	/// <summary>
	/// 读取并反序列化 JSON 请求体；反序列化失败或请求体为空时返回 <c>null</c>。
	/// </summary>
	/// <typeparam name="TValue">请求体类型。</typeparam>
	/// <param name="context">当前请求上下文。</param>
	/// <returns>反序列化结果；失败时为 <c>null</c>。</returns>
	public static async Task<TValue?> ReadAsync<TValue>(HttpContext context)
	{
		var options = context.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;

		if (options.GetTypeInfo(typeof(TValue)) is not JsonTypeInfo<TValue> typeInfo)
			return default;

		try
		{
			return await context.Request.ReadFromJsonAsync(typeInfo, context.RequestAborted);
		}
		catch (JsonException)
		{
			return default;
		}
		catch (BadHttpRequestException)
		{
			return default;
		}
	}
}
