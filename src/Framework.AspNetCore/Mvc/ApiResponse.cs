using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json.Serialization;

namespace Framework.AspNetCore.Mvc;

/// <summary>
/// 统一的 API 响应包装对象。
/// <para>所有控制器方法建议返回该类型，以便前端按统一结构解析 <see cref="StatusCode"/> / <see cref="Success"/> / <see cref="Code"/> / <see cref="Message"/> / <see cref="Data"/> 等字段。</para>
/// </summary>
/// <typeparam name="T">业务数据类型 <see cref="Data"/> 的类型。</typeparam>
public sealed class ApiResponse<T> : IApiResponse<T>
{
    /// <summary>
    /// HTTP 状态码。
    /// <para>默认值为 <c>200</c>；如需返回错误可设置为 400/401/403/404/500 等值。</para>
    /// </summary>
    public int StatusCode { get; set; } = 200;

    /// <summary>
    /// 业务处理是否成功。
    /// <para>默认值为 <c>true</c>；失败响应请显式置为 <c>false</c>。</para>
    /// </summary>
    public bool Success { get; set; } = true;
    /// <summary>
    /// 业务码。
    /// <para>约定成功使用 <c>"0000"</c>，失败使用业务方自定义码，默认值为空串。</para>
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 面向调用方的描述性消息。
    /// <para>默认值为空串；失败时通常填写错误说明。</para>
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 业务数据载荷。
    /// <para>默认为 <typeparamref name="T"/> 的默认值，调用方可按需赋值为任意业务对象。</para>
    /// </summary>
    public T Data { get; set; } = default(T);


    /// <summary>
    /// 扩展错误数据。
    /// <para>用于在响应体中携带模型校验错误详情等附加信息，序列化时会被 <see cref="JsonIgnoreAttribute"/> 忽略（仅在内部流转使用）。</para>
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, object> ErrorData { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// 向 <see cref="ErrorData"/> 中追加一条键值对，并返回当前字典以支持链式调用。
    /// </summary>
    /// <param name="key">错误数据键，不能为 <c>null</c>。</param>
    /// <param name="value">与键关联的错误数据值，可为任意对象。</param>
    /// <returns>追加后的 <see cref="ErrorData"/> 字典本身。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="key"/> 为 <c>null</c> 时由字典实现抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="key"/> 已存在于字典中时由字典实现抛出。</exception>
    public Dictionary<string, object> AddErrorData(string key, object value)
    {
        ErrorData.Add(key, value);
        return ErrorData;
    }

}
/// <summary>
/// 提供一组静态工厂方法，用于快速构造常用场景的 <see cref="ApiResponse{T}"/> 实例。
/// </summary>
public static partial class ApiResponse
{
    /// <summary>
    /// 创建一个表示“无内容”成功响应的 <see cref="ApiResponse{T}"/>，HTTP 状态码为 204。
    /// </summary>
    /// <typeparam name="T">业务数据类型占位（实际响应 <see cref="ApiResponse{T}.Data"/> 始终为默认值）。</typeparam>
    /// <param name="code">业务码，默认值为 <c>"0000"</c>。</param>
    /// <param name="message">描述性消息，默认值为 <c>"NoContent"</c>。</param>
    /// <returns>一个 <see cref="ApiResponse{T}"/>，<see cref="ApiResponse{T}.StatusCode"/> 为 204，<see cref="ApiResponse{T}.Success"/> 为 <c>true</c>。</returns>
    public static ApiResponse<object> NoContent<T>(string code = "0000", string message = "NoContent")
    {
        return new ApiResponse<object>
        {
            StatusCode = 204,
            Success = true,
            Code = code,
            Message = message
        };
    }


    /// <summary>
    /// 创建一个表示成功并携带业务数据的 <see cref="ApiResponse{T}"/>，HTTP 状态码为 200。
    /// </summary>
    /// <typeparam name="T"><paramref name="data"/> 的实际类型。</typeparam>
    /// <param name="data">要回传给调用方的业务数据。</param>
    /// <param name="code">业务码，默认值为 <c>"0000"</c>。</param>
    /// <param name="message">描述性消息，默认值为 <c>"Ok"</c>。</param>
    /// <returns>一个 <see cref="ApiResponse{T}"/>，<see cref="ApiResponse{T}.StatusCode"/> 为 200，<see cref="ApiResponse{T}.Success"/> 为 <c>true</c>。</returns>
    public static ApiResponse<T> Ok<T>(T data, string code="0000", string message = "Ok")
    {
        return new ApiResponse<T>
        {
            StatusCode = 200,
            Success = true,
            Code = code,
            Message = message,
            Data = data
        };
    }
    /// <summary>
    /// 创建一个通用业务失败响应，<see cref="ApiResponse{T}.Success"/> 为 <c>false</c>。
    /// </summary>
    /// <param name="code">业务错误码，默认值为 <c>"9999"</c>。</param>
    /// <param name="message">面向调用方的失败描述，默认值为 <c>"Fail"</c>。</param>
    /// <param name="statusCode">HTTP 状态码，默认值为 <c>500</c>。</param>
    /// <returns>一个 <see cref="ApiResponse{T}"/>，其 <see cref="ApiResponse{T}.Data"/> 字段保持为默认值。</returns>
    public static ApiResponse<object> Fail(string code="9999", string message = "Fail", int statusCode = 500)
    {
        return new ApiResponse<object>
        {
            StatusCode = statusCode,
            Success = false,
            Code = code,
            Message = message
        };
    }
    /// <summary>
    /// 创建一个未授权（401）失败响应。
    /// </summary>
    /// <param name="message">描述性消息，默认值为 <c>"Unauthorized"</c>。</param>
    /// <returns>一个 <see cref="ApiResponse{T}"/>，<see cref="ApiResponse{T}.StatusCode"/> 为 401，<see cref="ApiResponse{T}.Success"/> 为 <c>false</c>，<see cref="ApiResponse{T}.Code"/> 留空。</returns>
    public static ApiResponse<object> Unauthorized(string message = "Unauthorized")
    {
        return new ApiResponse<object>
        {
            StatusCode = 401,
            Success = false,
            Message = message
        };
    }

    /// <summary>
    /// 创建一个资源未找到（404）失败响应。
    /// </summary>
    /// <param name="message">描述性消息，默认值为 <c>"NotFound"</c>。</param>
    /// <returns>一个 <see cref="ApiResponse{T}"/>，<see cref="ApiResponse{T}.StatusCode"/> 为 404，<see cref="ApiResponse{T}.Success"/> 为 <c>false</c>，<see cref="ApiResponse{T}.Code"/> 留空。</returns>
    public static ApiResponse<object> NotFound(string message = "NotFound")
    {
        return new ApiResponse<object>
        {
            StatusCode = 404,
            Success = false,
            Message = message
        };
    }

    /// <summary>
    /// 创建一个请求参数错误（400）失败响应。
    /// </summary>
    /// <param name="message">描述性消息，默认值为 <c>"BadRequest"</c>。</param>
    /// <returns>一个 <see cref="ApiResponse{T}"/>，<see cref="ApiResponse{T}.StatusCode"/> 为 400，<see cref="ApiResponse{T}.Success"/> 为 <c>false</c>，<see cref="ApiResponse{T}.Code"/> 留空。</returns>
    public static ApiResponse<object> BadRequest( string message = "BadRequest")
    {
        return new ApiResponse<object>
        {
            StatusCode = 400,
            Success = false,
            Message = message
        };
    }

    /// <summary>
    /// 根据 <see cref="ModelStateDictionary"/> 创建一个请求参数错误（400）失败响应。
    /// <para>校验错误的详细信息会被包装为 <see cref="SerializableError"/> 写入 <see cref="ApiResponse{T}.ErrorData"/>，便于客户端按字段展示。</para>
    /// </summary>
    /// <param name="modelState">ASP.NET Core 的模型状态字典，包含每个字段的校验结果。</param>
    /// <param name="message">顶层描述性消息，默认值为 <c>"ModelState is not valid."</c>。</param>
    /// <returns>一个 <see cref="ApiResponse{T}"/>，其 <see cref="ApiResponse{T}.ErrorData"/> 字段承载模型校验错误。</returns>
    public static ApiResponse<object> BadRequest(ModelStateDictionary modelState, string message = "ModelState is not valid.")
    {
        return new ApiResponse<object>
        {
            StatusCode = 400,
            Success = false,
            Message = message,
            ErrorData = new SerializableError(modelState)
        };
    }

    /// <summary>
    /// 创建一个权限不足（403）失败响应。
    /// </summary>
    /// <param name="message">描述性消息，默认值为 <c>"Forbid"</c>。</param>
    /// <returns>一个 <see cref="ApiResponse{T}"/>，<see cref="ApiResponse{T}.StatusCode"/> 为 403，<see cref="ApiResponse{T}.Success"/> 为 <c>false</c>，<see cref="ApiResponse{T}.Code"/> 留空。</returns>
    public static ApiResponse<object> Forbid(string message = "Forbid")
    {
        return new ApiResponse<object>
        {
            StatusCode = 403,
            Success = false,
            Message = message
        };
    }

    /// <summary>
    /// 创建一个服务器内部错误（500）失败响应；当传入 <paramref name="exception"/> 时会附带其消息与附加数据。
    /// </summary>
    /// <param name="code">业务错误码，默认值为 <c>"Error"</c>。</param>
    /// <param name="message">顶层描述性消息，默认值为 <c>"Error"</c>。</param>
    /// <param name="exception">可选的内部异常；若不为 <c>null</c>，其 <c>Message</c> 与 <c>Data</c> 会以匿名对象形式写入 <see cref="ApiResponse{T}.Data"/>。</param>
    /// <returns>一个 <see cref="ApiResponse{T}"/>，<see cref="ApiResponse{T}.StatusCode"/> 为 500，<see cref="ApiResponse{T}.Success"/> 为 <c>false</c>。</returns>
    public static ApiResponse<object> Error(string code, string message = "Error", Exception? exception = null)
    {
        object? obj = null;
        // 仅在传入异常时构造一个包含 Message 与 Data 的匿名对象，否则 Data 保持为默认值
        if (exception != null)
        {
            obj = new { exception.Message, exception.Data };
        }
        return new ApiResponse<object>
        {
            StatusCode = 500,
            Success = false,
            Message = message,
            Data = obj
        };
    }
}