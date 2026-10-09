# YLFramework.ZLogging

基于 [ZLogger](https://github.com/Cysharp/ZLogger) + [Spectre.Console](https://spectreconsole.net/) 的彩色控制台日志扩展，支持同步写控制台 + 异步追加写文件。

## 功能概览

| 文件 | 类型 | 作用 |
|------|------|------|
| `ZLoggerSpectreConsoleOptions.cs` | `ZLoggerSpectreConsoleOptions` | 配置项：时间格式、LogLevel 颜色映射、Channel 大小、是否包含 Scope、文件输出、异常格式化器 |
| `ZLoggerSpectreConsoleLoggerProvider.cs` | `ZLoggerSpectreConsoleLoggerProvider` | 实现 `ILoggerProvider` + `ISupportExternalScope`；管理 Spectre.Console 与文件写入的双输出生命周期 |
| `ZLoggerSpectreExtensions.cs` | `ZLoggerSpectreConsoleLoggerFactoryExtensions` / `ZLoggerSpectreOutputExtensions` | 注册扩展 + 强类型 `Write` / `WriteLine` / `WriteJson` 输出方法（支持 `IRenderable` 渲染） |

## 快速上手

```csharp
// 1) 仅控制台
builder.Logging.AddZLoggerSpectreConsole(options =>
{
    options.TimeFormat = "HH:mm:ss.fff";
    options.IncludeScopes = true;
    options.CategoryColor = "cyan";
});

// 2) 同时输出到文件（追加写，UTF-8）
builder.Logging.AddZLoggerSpectreConsoleAndFile(options =>
{
    options.FilePath = "logs/app-.log";
    options.FileAppend = true;
    options.UsePlainTextFormatter();
});
```

## 颜色映射

`ZLoggerSpectreConsoleOptions.LogLevelColors` 是一个 `FrozenDictionary<LogLevel, string>`，把每个日志级别映射到 Spectre.Console 颜色字符串（`"red"`、`"green"`、`"yellow bold"` 等），开箱即用并可整体替换。

## 异常渲染

```csharp
options.SetExceptionFormatter((console, ex) =>
{
    console.WriteException(ex, ExceptionFormats.Spectre);
});
```

默认会把异常渲染为 Spectre 风格红字输出，并对 Markup 特殊字符做转义以避免格式串冲突。

## 异步文件输出

文件输出在独立的 `Task` 中通过 `BoundedChannel` 写入（默认容量 1024）。当队列满时丢弃策略由 ZLogger 决定。建议生产环境同时设置 `IInternalErrorLogger` 监控异步循环异常。

## 适用场景

- CLI 工具 / 服务端进程 的彩色日志
- 需要把控制台输出"按原样"回放为文件日志的场景
- 与 `Spectre.Console` 配合：在交互式 CLI 中嵌入表格、进度条、树形结构

## 版本

`1.0.x`，遵循 GitVersion。
