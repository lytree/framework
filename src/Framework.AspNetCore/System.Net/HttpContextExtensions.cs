using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Framework.AspNetCore.System.Net;

public static class HttpContextExtensions
{
    /// <summary>
    /// 获取IP地址
    /// <para>https://gist.github.com/jjxtra/3b240b31a1ed3ad783a7dcdb6df12c36</para>
    /// </summary>
    /// <param name="context"></param>
    /// <param name="allowForwarded"></param>
    /// <returns></returns>
    public static IPAddress? GetRemoteIPAddress(this HttpContext context, bool allowForwarded = true)
    {
        if (allowForwarded)
        {
            var header = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault()
                         ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (IPAddress.TryParse(header, out var ip))
            {
                return ip;
            }
        }

        return context.Connection.RemoteIpAddress;
    }

    /// <summary>
    /// 获取服务器主机URL地址，格式：http(s)://localhost。
    /// </summary>
    /// <param name="http">Http上下文对象。</param>
    /// <returns></returns>
    public static string GetHostUrl(this HttpContext http)
    {
        return http.Request.Scheme + "://" + http.Request.Host;
    }

    /// <summary>
    /// 判断当前请求是否是移动浏览器请求。
    /// </summary>
    /// <param name="http">Http上下文对象。</param>
    /// <returns></returns>
    /// <exception cref="Exception">请求对象若不存在，确认服务器是否开启 WebSocket。</exception>
    public static bool CheckMobile(this HttpContext http)
    {
        if (http == null || http.Request == null)
            throw new Exception("Server WebSocket not enabled!");

        var agent = http.Request.Headers["User-Agent"].ToString();
        if (string.IsNullOrWhiteSpace(agent))
            agent = http.Request.Headers["X-Forwarded-For"].ToString();
        return CheckMobile(agent);
    }
    /// <summary>
    /// 根据浏览器代理字符串判断是否是移动端请求。
    /// </summary>
    /// <param name="agent">浏览器代理字符串。</param>
    /// <returns></returns>
    private static bool CheckMobile(string agent)
    {
        if (agent.Contains("Windows NT") || agent.Contains("Macintosh"))
            return false;

        bool flag = false;
        string[] keywords = ["Android", "iPhone", "iPod", "Windows Phone", "MQQBrowser"];

        foreach (string item in keywords)
        {
            if (agent.Contains(item))
            {
                flag = true;
                break;
            }
        }

        return flag;
    }
}