using System.Text.RegularExpressions;
namespace Telegram.Core.App;

public static class UrlParser
{
    const string pattern = @"^(?:(?<scheme>[a-z][a-z0-9+.-]*)://)?(?:(?<user>[^:@/?#]+)(?::(?<pass>[^@/?#]*))?@)?(?<host>\[[0-9a-fA-F:]+\]|localhost|(?:[a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}|\d{1,3}(?:\.\d{1,3}){3})(?::(?<port>\d{1,5}))?(?<path>/[^?#]*)?(?:\?(?<query>[^#]*))?(?:#(?<fragment>.*))?$";

    // 增强型正则表达式，专门针对代理配置和 SOCKS5 格式：
    // (?:(?<scheme>[a-z]+)://)?  -> 协议头 (e.g., socks5)
    // (?:(?<user>[^:]+)(?::(?<pass>[^@]+))?@)? -> 可选的 user:pass@
    // (?<host>[^:]+)           -> 主机名 (e.g., 192.168.1.10)
    // (?::(?<port>\d+))?       -> 可选的 :port
    private static readonly Regex ProxyRegex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static ParsedUrl Parse(string urlString)
    {
        if (string.IsNullOrWhiteSpace(urlString))
        {
            return new ParsedUrl { IsValid = false };
        }

        // --- 尝试使用 System.Uri (适用于 http, https, ftp, file 等标准协议) ---
        // 只有带有明确协议头的，才优先交给 Uri 处理。
        if (urlString.Contains("://") && Uri.TryCreate(urlString, UriKind.Absolute, out Uri uri))
        {
            // 如果 URI 成功创建且是绝对 URI
            if (uri.IsAbsoluteUri)
            {
                // 如果 Uri 识别的 Scheme 是标准 Web 协议，则使用 Uri 解析结果
                if (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                    uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                    uri.Scheme.Equals(Uri.UriSchemeFtp, StringComparison.OrdinalIgnoreCase))
                {
                    return new ParsedUrl
                    {
                        IsValid = true,
                        Scheme = uri.Scheme,
                        Host = uri.Host,
                        Port = uri.Port > 0 ? uri.Port : uri.IsDefaultPort ? -1 : 80,
                        Path = uri.AbsolutePath,
                        Query = uri.Query,
                        Username = string.IsNullOrEmpty(uri.UserInfo) ? null : uri.UserInfo.Split(':')[0],
                        Password = string.IsNullOrEmpty(uri.UserInfo) ? null : (uri.UserInfo.Contains(':') ? uri.UserInfo.Split(':')[1] : null)
                    };
                }
            }
        }

        // --- 使用正则表达式处理 SOCKS5、纯 host:port 或未被 Uri 识别的带协议头格式 ---
        var match = ProxyRegex.Match(urlString);

        if (match.Success)
        {
            string portString = match.Groups["port"].Value;
            int port = string.IsNullOrEmpty(portString) ? -1 : int.Parse(portString);

            // 如果没有端口，并且没有协议头，我们可能需要判断它是否是一个有效的地址，这里我们假设它是有效的
            if (port == -1 && !match.Groups["scheme"].Success)
            {
                // 如果既无协议也无端口，可能是纯主机名，我们返回解析失败或根据业务逻辑返回默认端口
                // 这里我们保持解析失败的严格性，除非它是有效的SOCKS5格式
            }

            return new ParsedUrl
            {
                IsValid = true,
                Scheme = match.Groups["scheme"].Success ? match.Groups["scheme"].Value : "unknown",
                Host = match.Groups["host"].Value,
                Port = port,
                Username = match.Groups["user"].Value,
                Password = match.Groups["pass"].Value,
                Path = "",
                Query = ""
            };
        }

        // 如果所有尝试都失败
        return new ParsedUrl { IsValid = false };
    }
}
/// <summary>
/// 存储通用 URL 解析结果的结构体。
/// </summary>
public struct ParsedUrl
{
    // --- 核心状态 ---
    public bool IsValid { get; set; }  // 解析是否成功

    // --- 地址组件 ---
    public string Scheme { get; set; } // 协议 (e.g., http, https, socks5, unknown)
    public string Host { get; set; }   // 主机名或 IP 地址
    public int Port { get; set; }      // 端口号 (-1 表示未指定或使用默认端口)

    // --- 认证信息 (通常用于代理或 FTP) ---
    public string Username { get; set; } // 用户名
    public string Password { get; set; } // 密码

    // --- 标准 URL 路径和查询信息 (主要用于 HTTP/HTTPS) ---
    public string Path { get; set; }     // 路径 (e.g., /api/resource)
    public string Query { get; set; }    // 查询字符串 (e.g., ?key=value)

    /// <summary>
    /// 重写 ToString() 方法，用于调试和输出简洁的解析结果。
    /// </summary>
    public override string ToString()
    {
        if (!IsValid) return "解析失败：格式无效";

        string userInfo = string.IsNullOrEmpty(Username)
            ? ""
            : $"{Username}{(string.IsNullOrEmpty(Password) ? "" : ":***")}@"; // 密码用***隐藏

        // 格式化输出: [scheme] user:***@host:port/path?query
        return $"{Scheme}://{userInfo}{Host}{(Port > 0 ? $":{Port}" : "")}{Path}{Query}";
    }
}