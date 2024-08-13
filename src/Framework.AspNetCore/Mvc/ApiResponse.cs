using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json.Serialization;

namespace Framework.AspNetCore.AspNetCore.Mvc;

public class ApiResponse<T> : IApiResponse<T>
{
    public int StatusCode { get; set; } = 200;

    public bool Success { get; set; } = true;
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public T? Data { get; set; } = default(T);


    [JsonIgnore]
    public Dictionary<string, object> ErrorData { get; set; } = new Dictionary<string, object>();

    public Dictionary<string, object> AddErrorData(string key, object value)
    {
        ErrorData.Add(key, value);
        return ErrorData;
    }

}
public static partial class ApiResponse
{
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
    public static ApiResponse<object> Fail(string code="9999", string message = "Fail")
    {
        return new ApiResponse<object>
        {
            StatusCode = 200,
            Success = true,
            Code = code,
            Message = message
        };
    }
    public static ApiResponse<object> Unauthorized(string message = "Unauthorized")
    {
        return new ApiResponse<object>
        {
            StatusCode = 401,
            Success = false,
            Message = message
        };
    }

    public static ApiResponse<object> NotFound(string message = "NotFound")
    {
        return new ApiResponse<object>
        {
            StatusCode = 404,
            Success = false,
            Message = message
        };
    }

    public static ApiResponse<object> BadRequest( string message = "BadRequest")
    {
        return new ApiResponse<object>
        {
            StatusCode = 400,
            Success = false,
            Message = message
        };
    }

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

    public static ApiResponse<object> Forbid(string message = "Forbid")
    {
        return new ApiResponse<object>
        {
            StatusCode = 403,
            Success = false,
            Message = message
        };
    }

    public static ApiResponse<object> Error(string code, string message = "Error", Exception? exception = null)
    {
        object? obj = null;
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