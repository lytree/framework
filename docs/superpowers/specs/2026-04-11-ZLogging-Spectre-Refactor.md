# ZLogging Spectre Console Provider 设计文档

## 1. 概述

重构 `Framework.ZLogging` 项目，将控制台日志输出改为使用 `Spectre.Console` 库，并创建类似 ZLogger 的 `ZLoggerSpectreConsoleLoggerProvider` Provider 模式。

## 2. 背景

当前项目已有 `SpectreConsoleProcessor` 实现日志处理，但未遵循 ZLogger 的 Provider 架构模式。需要创建标准化的 Provider 以保持与 ZLogger 生态一致。

## 3. 架构设计

### 3.1 核心组件

```
ZLoggerSpectreConsoleLoggerProvider
├── 实现 ILoggerProvider
├── 实现 IAsyncDisposable
├── 实现 ISupportExternalScope (可选)
└── 创建 ZLoggerSpectreConsoleLogger

ZLoggerSpectreConsoleLogger
├── 实现 ILogger
├── 日志级别过滤
├── 类别名称管理
└── 输出到 Spectre Console
```

### 3.2 类图

```csharp
// ZLoggerSpectreConsoleLoggerProvider - 主 Provider
[ProviderAlias("ZLoggerSpectreConsole")]
public class ZLoggerSpectreConsoleLoggerProvider 
    : ILoggerProvider, IAsyncDisposable, ISupportExternalScope
{
    // 属性
    private readonly ZLoggerSpectreConsoleOptions _options;
    private readonly ConcurrentDictionary<string, ZLoggerSpectreConsoleLogger> _loggers;
    private readonly IAsyncLogProcessor _processor;
    private IExternalScopeProvider? _scopeProvider;

    // 方法
    public ILogger CreateLogger(string categoryName);
    public void Dispose();
    public ValueTask DisposeAsync();
    public void SetScopeProvider(IExternalScopeProvider scopeProvider);
}

// ZLoggerSpectreConsoleLogger - 具体 Logger
public class ZLoggerSpectreConsoleLogger : ILogger
{
    private readonly string _categoryName;
    private readonly ZLoggerSpectreConsoleOptions _options;
    private readonly IAsyncLogProcessor _processor;
    private readonly IExternalScopeProvider? _scopeProvider;

    public bool IsEnabled(LogLevel logLevel);
    public IDisposable? BeginScope<T>(T state);
    public void Log<T>(LogLevel logLevel, EventId eventId, T state, Exception? exception, Func<T, Exception?, string> formatter);
}

// ZLoggerSpectreConsoleOptions - 配置选项
public class ZLoggerSpectreConsoleOptions : ZLoggerOptions
{
    public bool EnableAnsi { get; set; } = true;
    public bool UseTime { get; set; } = true;
    public string TimeFormat { get; set; } = "HH:mm:ss";
    public string TraceColor { get; set; } = "grey";
    public string DebugColor { get; set; } = "grey";
    public string InformationColor { get; set; } = "green";
    public string WarningColor { get; set; } = "yellow";
    public string ErrorColor { get; set; } = "red";
    public string CriticalColor { get; set; } = "red bold";
}
```

### 3.3 扩展方法 (ZLoggerSpectreExtensions)

保留现有实现，扩展方法直接调用 `AnsiConsole`:

```csharp
public static partial class ZLoggerSpectreExtensions
{
    public static ILoggingBuilder AddZLoggerSpectreConsole(
        this ILoggingBuilder builder,
        Action<ZLoggerSpectreConsoleOptions>? configure = null);

    // WriteMarkup 系列
    public static void WriteMarkup(this ILogger logger, string markup);
    public static void WriteMarkupLine(this ILogger logger, string markup);
    public static void WriteMarkups(this ILogger logger, params string[] markups);

    // 输出方法
    public static void WriteTable(this ILogger logger, Table table);
    public static void WriteTree(this ILogger logger, string root, Action<Tree> configure);
    public static void WritePanel(this ILogger logger, string content);
    public static void WritePanel(this ILogger logger, string content, string title);
    public static void WriteStatus(this ILogger logger, string status);

    // 工具方法
    public static void WriteKeyValueTable(this ILogger logger, IEnumerable<(string Key, string Value)> items);
    public static void WriteJson(this ILogger logger, object obj);
    public static void WriteJsonMarkup(this ILogger logger, object obj);
    public static void WriteEmptyLine(this ILogger logger);
}
```

## 4. 与现有代码兼容性

### 4.1 保留的类 (标记为 obsolete)

- `SpectreConsoleProcessor` - 保留但标记 `[Obsolete]`
- `SpectreConsoleLogProcessorOptions` - 保留但标记 `[Obsolete]`

### 4.2 迁移路径

旧代码:
```csharp
builder.AddZLoggerLogProcessor(new SpectreConsoleProcessor(options));
```

新代码:
```csharp
builder.AddZLoggerSpectreConsole();
```

## 5. 实现细节

### 5.1 日志级别颜色映射

| LogLevel | ANSI 颜色 | 默认值 |
|----------|----------|--------|
| Trace | grey | grey |
| Debug | (空) | grey |
| Information | cyan | green |
| Warning | yellow | yellow |
| Error | red | red |
| Critical | red bold | red bold |

### 5.2 日志输出格式

```
{HH:mm:ss} [{LogLevel}] [{Category}] {Message}
```

示例:
```
14:30:25 [Information] [MyApp.Service] Service started successfully
14:30:26 [Warning] [MyApp.Service] Connection retry...
14:30:27 [Error] [MyApp.Service] Connection failed: timeout
```

### 5.3 ANSI 支持

- 检测控制台是否支持 ANSI (自动检测)
- 可通过 `EnableAnsi` 选项强制开关
- 输出重定向时自动禁用 ANSI

## 6. 使用示例

```csharp
// 基础用法
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddZLoggerSpectreConsole();

// 带配置用法
builder.Logging.AddZLoggerSpectreConsole(options =>
{
    options.UseTime = true;
    options.TimeFormat = "yyyy-MM-dd HH:mm:ss";
    options.InformationColor = "cyan";
    options.EnableAnsi = true;
});

// 扩展方法使用
var logger = serviceProvider.GetRequiredService<ILogger<MyService>>();
logger.WriteMarkupLine("[bold]Hello [red]World[/][/]");
logger.WriteTable(myTable);
logger.WriteJson(myObject);
```

## 7. 文件结构

```
src/Framework.ZLogging/
├── Framework.ZLogging.csproj
├── ZLoggerSpectreConsoleLoggerProvider.cs  // 新建
├── ZLoggerSpectreConsoleLogger.cs       // 新建
├── ZLoggerSpectreConsoleOptions.cs     // 新建
├── ZLoggerSpectreExtensions.cs         // 改进现有
├── SpectreConsoleLogProcessor.cs        // delete
├── SpectreConsoleLogProcessorOptions.cs // delete
└── SpectreRollingFileLogger.cs        // 保留
```

## 8. 测试计划

1. 单元测试: Provider 创建和销毁
2. 单元测试: 日志级别过滤
3. 集成测试: 完整日志输出流程
4. 集成测试: 扩展方法功能
5. 兼容性测试: 现有代码迁移

## 9. 验收标准

- [ ] `ZLoggerSpectreConsoleLoggerProvider` 实现完整
- [ ] Provider 可通过 `AddZLoggerSpectreConsole()` 注册
- [ ] 日志输出到 Spectre Console 有颜色和格式
- [ ] 扩展方法保留并可用
- [ ] 现有 `SpectreConsoleProcessor` 标记 obsolete
- [ ] 单元测试通过