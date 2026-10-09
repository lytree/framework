# YLFramework.AspNetCore.Middleware

面向 Kestrel 连接管道的可复用中间件集合。所有中间件都通过 `ListenOptionsExtensions` 暴露 `Use*` 风格扩展方法，一行接入。

## 中间件清单

| 子模块 | 扩展方法 | 作用 |
|--------|---------|------|
| **Echo** | `UseEcho()` | TCP 回显：把客户端写入的全部字节原样写回。调试 / 探活用 |
| **FlowAnalyze** | `UseFlowAnalyze(IFlowAnalyzer)` | 流量统计：按 `FlowType`（上行 / 下行 / 总和）累计字节数，可注册自定义分析器把指标推送到 Prometheus / 日志等 |
| **FlowXor** | `UseFlowXor(byte[] key)` | 字节级 XOR 混淆：常用于代理场景下对透明 TCP 流量做轻度加密，绕过旁路检测 |
| **HttpProxy** | `UseHttpProxy(...)` | HTTP CONNECT 隧道代理：支持认证（`IHttpProxyAuthenticationHandler`）+ PROXY 协议解析（`ProxyMiddleware`） |
| **TunnelProxy** | `UseTunnelProxy()` | 透传式 TCP 隧道：把 Kestrel 端点变为可转发的中转节点 |
| **Telnet** | `UseTelnet()` + 三个嵌套中间件 `UseBye()` / `UseEcho()` / `UseEmpty()` | Telnet 协议服务端：根据命令返回 BYE / 回显 / 空行 |
| **TelnetProxy** | `UseXorTelnetProxy(byte[] key)` | Telnet 协议上的 XOR 代理：把 Telnet 客户端通过 XOR 流转发到上游 |
| **TlsDetection** | `UseTlsDetection(...)` | TLS 特征识别：通过 `FakeTlsConnectionFeature` 模拟 TLS 记录，让上游把裸 TCP 误判为 TLS 流量（用于协议伪装） |

## 用法示例

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, listen =>
    {
        listen.UseFlowAnalyze(new ConsoleFlowAnalyzer());
        listen.UseFlowXor(Encoding.ASCII.GetBytes("secret"));
        listen.UseEcho();
    });
});
```

## 服务注册

部分中间件需要把分析器 / 认证处理器等注入 DI：

```csharp
builder.Services.AddFlowAnalyze();
builder.Services.AddHttpProxyAuthentication<MyAuthHandler>();
```

DI 注册方法在对应子模块的 `ServiceCollectionExtensions` 中。

## 设计要点

- 每个中间件都是 `DelegatingDuplexPipe` 或 `DelegatingStream` 包装，对性能影响极小
- `TlsDetection` 通过临时替换 `ConnectionContext.Features` 实现协议层降级，连接关闭后自动恢复
- `HttpProxy` 支持 PROXY 协议 v1 / v2 解析（`ProxyProtocol`）以及用户自定义认证

## 版本

`1.0.x`，遵循 GitVersion。
