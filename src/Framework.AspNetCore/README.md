# YLFramework.AspNetCore

`YLFramework.AspNetCore` 为 ASP.NET Core 应用提供一组跨场景的基础设施：统一的 API 响应模型、连接管道扩展、异常处理中间件、模块化路由注册。命名空间与本项目同名 `Framework.AspNetCore`（以及 `Application` / `Mvc` / `Routing` / `Microsoft.AspNetCore.Connections` 等子命名空间）。

## 功能概览

| 命名空间 | 关键类型 | 作用 |
|---------|---------|------|
| `Framework.AspNetCore` | `IKestrelMiddleware` | Kestrel 中间件标记接口与基础约定 |
| `Framework.AspNetCore.Application` | `ApplicationContext` / `ApplicationBuilder` / `ApplicationDelegate` / `IApplicationMiddleware` | 自定义"应用中间件"管道，独立于 ASP.NET Core 自身的 `RequestDelegate` 链，可承载横切逻辑 |
| `Framework.AspNetCore.Middlewares` | `ApiExceptionMiddleware` | 捕获下游异常，统一转换为 `IApiResponse` 响应并写入 `ILogger` |
| `Framework.AspNetCore.Mvc` | `ApiResponse<T>` / `ApiResult` / `IApiResponse` / `ApiException` | 统一前后端交互格式，封装 `success` / `code` / `message` / `data` / `exception` 字段 |
| `Framework.AspNetCore.Routing` | `IBaseRouter` / `RouterExtensions` | 通过反射扫描实现 `IBaseRouter` 的模块类并自动注册 MVC 路由，配合"模块化 API"模式 |
| `Framework.AspNetCore.Microsoft.AspNetCore.Connections` | `ConnectionBuilderExtensions` / `ConnectionFactoryTypeUtil` / `ServiceCollectionExtensions` | 扩展 `ConnectionBuilder` 以便在 Kestrel 连接级管道注册自定义中间件工厂 |
| `Framework.AspNetCore.System.Net` | `HttpContextExtensions` / `HttpRequestExtensions` | 常用 `HttpContext` / `HttpRequest` 工具方法（如 `IsMobileBrowser`） |

## 快速上手

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1) 注册统一 API 响应 + 模块化路由扫描
builder.Services.AddControllers();
builder.Services.AddFrameworkAspNetCore(builder.Configuration);

// 2) 构建应用
var app = builder.Build();

// 3) 使用统一异常中间件
app.UseApiExceptionHandler();

// 4) 注册业务模块（实现 IBaseRouter 的类自动被发现）
app.UseFrameworkRouters();
```

## 模块化路由

实现 `IBaseRouter`：

```csharp
public class UserRouter : IBaseRouter
{
    public void AddModuleRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/users", () => Results.Ok(new { id = 1, name = "alice" }));
    }
}
```

调用 `AddIoC<TAssembly>()` 把目标程序集加入模块列表后，`AddRouters` 会逐个实例化模块并注册其路由。

## 统一响应

控制器与服务可直接返回 `ApiResult.Ok(data)` / `ApiResult.Fail(code, msg)`，中间件会负责序列化与异常兜底。

## 版本

`1.0.x`，遵循 GitVersion（仓库根 `GitVersion.yml`）。
