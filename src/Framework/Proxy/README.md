# Proxy — 网络代理客户端

支持 HTTP、HTTPS、SOCKS4、SOCKS4a、SOCKS5 代理。

## 文件

| 文件 | 类型 |
|------|------|
| `ArrayBuilder.cs` | 字节数组构造工具 |
| `ArrayUtils.cs`   | 字节 / 数组工具 |
| `Crc16.cs` / `Crc32.cs` / `Lrc.cs` | 校验算法 |
| `UrlParser.cs`    | URL 解析 |
| `Proxy/IProxyClient.cs`         | 代理客户端接口 |
| `Proxy/ProxyClientFactory.cs`   | 代理工厂（按 URL scheme 自动选择） |
| `Proxy/HttpProxyClient.cs`      | HTTP CONNECT 隧道 |
| `Proxy/Socks4ProxyClient.cs`    | SOCKS4 |
| `Proxy/Socks4aProxyClient.cs`   | SOCKS4a |
| `Proxy/Socks5ProxyClient.cs`    | SOCKS5 |
| `Proxy/Utils.cs`                | SOCKS5 地址解析等 |
| `Proxy/EventArgs/CreateConnectionAsyncCompletedEventArgs.cs` | 事件参数 |
| `Proxy/Exceptions/ProxyException.cs` | 异常类型 |

## 外部依赖

- 无（仅 BCL + `System.Threading.Tasks`）
- 部分工具方法依赖 `System.Buffers/ArrayPoolBufferWriter`（来自 `System.Buffers/` 目录）

## 复制最小子集

如果只需要 SOCKS5 客户端：

```
ArrayBuilder.cs
ArrayUtils.cs
UrlParser.cs
Crc16.cs
Crc32.cs
Lrc.cs
Proxy/
  IProxyClient.cs
  ProxyClientFactory.cs
  Socks5ProxyClient.cs
  Utils.cs
  EventArgs/CreateConnectionAsyncCompletedEventArgs.cs
  Exceptions/ProxyException.cs
```

如使用其它协议再加对应 `*ProxyClient.cs`。

## 用法示例

```csharp
var client = ProxyClientFactory.Create("socks5://user:pass@127.0.0.1:1080");
await client.ConnectAsync("target.example.com", 443);
var stream = client.GetStream();
// ... 与目标站点 TLS 握手
```
