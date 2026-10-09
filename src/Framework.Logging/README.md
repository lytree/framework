# YLFramework.Logging

基于 [NLog](https://nlog-project.org/) 的 ASP.NET Core 通用日志接入包。提供开箱即用的 NLog 配置（`NLog.config`）与对 `ILoggingBuilder` 的扩展方法。

## 功能概览

| 文件 | 作用 |
|------|------|
| `Framework.Logging.csproj` | 引用 NLog / NLog.Extensions.Logging / NLog.Web.AspNetCore |
| `NLog.config` | 默认 NLog 配置：彩色控制台 + 按天滚动文件 + 自动布局 |

## 快速上手

```csharp
var builder = WebApplication.CreateBuilder(args);

// 一行启用 NLog
builder.Logging.AddNLogWeb();
```

或通过 `Framework.Logging` 提供的统一入口（视项目实际扩展而定）：

```csharp
builder.Logging.AddFrameworkNLog();
```

## 默认 NLog 行为

`NLog.config` 默认：

- **控制台目标**：`${longdate}|${level:uppercase=true}|${logger}|${message}` 彩色输出
- **文件目标**：`logs/${shortdate}.log`，按天滚动，单文件 50MB，保留 30 天
- **布局**：`${longdate} ${level} ${logger} ${message} ${exception:format=tostring}`
- **自动 flush**：高吞吐场景下使用 `BufferingTargetWrapper`

如需自定义，复制 `NLog.config` 到项目根目录并修改即可，NLog 会优先使用项目级配置。

## 与 `Framework.ZLogging` 的区别

| 维度 | YLFramework.Logging | YLFramework.ZLogging |
|------|--------------------|----------------------|
| 后端 | NLog | ZLogger（零分配） |
| 输出风格 | 模板化字符串、彩色控制台 | Spectre.Console 强类型 IRenderable |
| 文件输出 | 内置滚动、压缩、归档 | 简易追加写 |
| 结构化日志 | 支持（`${event-properties:item=...}`） | 弱（主要靠 format message） |
| 适用场景 | 传统 ASP.NET Core 业务服务 | CLI / 高吞吐 / 强类型输出 |

## 依赖

- `NLog`
- `NLog.Extensions.Logging`
- `NLog.Web.AspNetCore`

## 版本

`1.0.0`，遵循 GitVersion。
