using Framework.AspNetCore.AspNetCore.Mvc;
using Framework.AspNetCore.Mvc;
using Framework.Helper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System.Net;
using Mime = System.Net.Mime;
namespace Framework.AspNetCore.Middlewares;

/// <summary>
/// 异常中间件
/// </summary>
public class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (ApiException ex)
        {
            await HandleApiExceptionAsync(httpContext, ex);
        }
        catch (Exception ex)
        {
            if (ex is ArgumentException || ex is FormatException)
            {
                await HandleArgumentExceptionAsync(httpContext, ex);
            }
            else
            {
                await HandleExceptionAsync(httpContext, ex);
            }
        }
    }

    private static Task HandleApiExceptionAsync(HttpContext context, ApiException appException)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = appException.StatusCode;

        return context.Response.WriteAsync(JsonHelper.Serialize(ApiResponse.Fail(appException.ApiMessage)));
    }
    private Task HandleArgumentExceptionAsync<T>(HttpContext context, T exception) where T : Exception
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

        var authorization = context.Request.Headers.Authorization.FirstOrDefault();
        var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();
        context.Items.TryGetValue("_ActionArguments", out object? actionArguments);
        _logger.LogError(exception, "Error while processing request. \r\nActionArguments: {ActionArguments} \r\nAuthorization: {Authorization} \r\nUserAgent: {UserAgent}",
            actionArguments != null ? JsonHelper.Serialize(actionArguments) : "",
            authorization,
            userAgent);

        return context.Response.WriteAsync(JsonHelper.Serialize(obj: ApiResponse.BadRequest(exception.Message)));
    }
    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var authorization = context.Request.Headers.Authorization.FirstOrDefault();
        var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();
        context.Items.TryGetValue("_ActionArguments", out object? actionArguments);
        _logger.LogError(exception, "Error while processing request. \r\nActionArguments: {ActionArguments} \r\nAuthorization: {Authorization} \r\nUserAgent: {UserAgent}",
        actionArguments != null ? JsonHelper.Serialize(actionArguments) : "",
        authorization,
        userAgent);

        return context.Response.WriteAsync(JsonHelper.Serialize(obj: ApiResponse.Error(exception.Message)));
    }
}