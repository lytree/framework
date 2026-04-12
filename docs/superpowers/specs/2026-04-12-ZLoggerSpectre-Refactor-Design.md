# ZLoggerSpectre 重构设计方案

## 目标

重构 Framework.ZLogging 的 SpectreConsole 输出，提供更灵活的颜色配置。

## 1. ZLoggerSpectreConsoleLoggerProvider

保持现有结构:
- 继承 `ILoggerProvider`, `IAsyncDisposable`
- 使用 `SpectreConsoleLogProcessor` 进行异步日志处理
- 通过 `AnsiConsole` 输出

### PlainTextFormatterConfig 配置方法

```csharp
public void SetPrefixFormatter(string format, Func<MessageTemplate, LogInfo, string> formatter)
public void SetSuffixFormatter(string format, Func<MessageTemplate, LogInfo, string> formatter)
public void SetExceptionFormatter(Action<IAnsiConsole, Exception> formatter)
```

## 2. ZLoggerSpectreConsoleOptions 颜色配置

### 时间颜色 (TimeOnlyLocal)
- 06:00-12:00: 绿色 `#green`
- 12:00-18:00: 蓝色 `#blue`
- 18:00-24:00: 紫色 `#purple`
- 00:00-06:00: 灰色 `#grey`

### LogLevel 颜色
- Trace: `grey`
- Debug: `grey`
- Information: `green`
- Warning: `yellow`
- Error: `red`
- Critical: `red bold`

### Category 颜色
- 默认: `cyan`

### 时间格式
- 默认: `yyyy-MM-dd HH:mm:ss`

## 3. ZLoggerSpectreExtensions

默认配置 `UsePlainTextFormatter`:
- 时间格式: `yyyy-MM-dd HH:mm:ss`
- 时间颜色使用 `TimeOnlyLocal`
- LogLevel 颜色使用默认配置
- Category 颜色为青色

## 4. 实现文件

### ZLoggerSpectreConsoleOptions.cs
- 添加 `TimeOnlyColor` 配置
- 添加 `CategoryColor` 配置
- 修改 `PlainTextFormatterConfig` 支持 Func

### ZLoggerSpectreConsoleLoggerProvider.cs
- 使用 AnsiConsole 输出
- 调用 `PlainTextFormatterConfig` 配置的 formatter

### ZLoggerSpectreExtensions.cs
- 默认启用完整颜色配置

## 5. 示例用法

```csharp
// 默认用法
logging.AddZLoggerSpectreConsole();

// 自定义用法
logging.AddZLoggerSpectreConsole(options =>
{
    options.UsePlainTextFormatter(formatter =>
    {
        formatter.SetPrefixFormatter("{0}|{1}|", (template, info) =>
            $"{info.Timestamp:HH:mm:ss}|{info.LogLevel}|");
        formatter.SetSuffixFormatter(" ({0})", (template, info) =>
            $"{info.Category}");
    });
});
```