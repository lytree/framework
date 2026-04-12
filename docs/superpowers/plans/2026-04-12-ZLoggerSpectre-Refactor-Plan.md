# ZLoggerSpectre 重构实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 重构 Framework.ZLogging 的 SpectreConsole 输出，提供更灵活的格式化配置和颜色显示

**Architecture:** 使用 Spectre.Console 的 AnsiConsole 进行输出，通过 PlainTextFormatterConfig 提供可配置的格式化方法

**Tech Stack:** ZLogger, Spectre.Console, Microsoft.Extensions.Logging

---

### Task 1: 修改 ZLoggerSpectreConsoleOptions.cs - 添加颜色配置和修改 PlainTextFormatterConfig

**Files:**
- Modify: `src/Framework.ZLogging/ZLoggerSpectreConsoleOptions.cs`

- [ ] **Step 1: 修改 PlainTextFormatterConfig 支持 Func 类型的 formatter**

```csharp
public class PlainTextFormatterConfig
{
    private readonly ZLoggerSpectreConsoleOptions _options;
    private Func<MessageTemplate, LogInfo, string>? _prefixFormatter;
    private Func<MessageTemplate, LogInfo, string>? _suffixFormatter;
    private Action<IAnsiConsole, Exception>? _exceptionFormatter;

    internal PlainTextFormatterConfig(ZLoggerSpectreConsoleOptions options)
    {
        _options = options;
    }

    public void SetPrefixFormatter(string format, Func<MessageTemplate, LogInfo, string> formatter)
    {
        _options.PrefixFormat = format;
        _prefixFormatter = formatter;
    }

    public void SetSuffixFormatter(string format, Func<MessageTemplate, LogInfo, string> formatter)
    {
        _options.SuffixFormat = format;
        _suffixFormatter = formatter;
    }

    public void SetExceptionFormatter(Action<IAnsiConsole, Exception> formatter)
    {
        _options.ExceptionFormat = "{0}";
        _exceptionFormatter = formatter;
    }

    public void SetTimeFormatter(string format)
    {
        _options.TimeFormat = format;
    }

    public void SetLogLevelColor(LogLevel level, string color)
    {
        switch (level)
        {
            case LogLevel.Trace: _options.TraceColor = color; break;
            case LogLevel.Debug: _options.DebugColor = color; break;
            case LogLevel.Information: _options.InformationColor = color; break;
            case LogLevel.Warning: _options.WarningColor = color; break;
            case LogLevel.Error: _options.ErrorColor = color; break;
            case LogLevel.Critical: _options.CriticalColor = color; break;
        }
    }
}
```

- [ ] **Step 2: 添加 TimeOnlyColor 配置**

```csharp
public string TimeOnlyColor06 { get; set; } = "green";
public string TimeOnlyColor12 { get; set; } = "blue";
public string TimeOnlyColor18 { get; set; } = "purple";
public string TimeOnlyColor00 { get; set; } = "grey";

public string CategoryColor { get; set; } = "cyan";
```

- [ ] **Step 3: 保存 formatter 到 options**

```csharp
// 在 ZLoggerSpectreConsoleOptions 中添加
public Func<MessageTemplate, LogInfo, string>? PrefixFormatter { get; set; }
public Func<MessageTemplate, LogInfo, string>? SuffixFormatter { get; set; }
public Action<IAnsiConsole, Exception>? ExceptionFormatter { get; set; }
```

---

### Task 2: 修改 ZLoggerSpectreConsoleLoggerProvider.cs - 实现颜色输出

**Files:**
- Modify: `src/Framework.ZLogging/ZLoggerSpectreConsoleLoggerProvider.cs`

- [ ] **Step 1: 修改 SpectreConsoleLogProcessor 实现颜色输出**

```csharp
internal class SpectreConsoleLogProcessor : IAsyncLogProcessor
{
    private readonly ZLoggerSpectreConsoleOptions _options;

    public SpectreConsoleLogProcessor(ZLoggerSpectreConsoleOptions options)
    {
        _options = options;
    }

    public void Post(IZLoggerEntry log)
    {
        var message = log.ToString();
        if (string.IsNullOrEmpty(message))
        {
            log.Return();
            return;
        }

        var logInfo = log.LogInfo;
        var timestamp = logInfo.Timestamp;
        var timeStr = timestamp.ToString(_options.TimeFormat);
        var logLevelStr = logInfo.LogLevel.ToString();
        var categoryStr = logInfo.Category;
        var exception = logInfo.Exception;

        var timeColor = GetTimeColor(timestamp);
        var levelColor = GetLogLevelColor(logInfo.LogLevel);

        WriteOutput(timeStr, timeColor, logLevelStr, levelColor, categoryStr, message, exception);
        log.Return();
    }

    private string GetTimeColor(DateTime timestamp)
    {
        var hour = timestamp.Hour;
        if (hour >= 6 && hour < 12) return _options.TimeOnlyColor06;
        if (hour >= 12 && hour < 18) return _options.TimeOnlyColor12;
        if (hour >= 18 && hour < 24) return _options.TimeOnlyColor18;
        return _options.TimeOnlyColor00;
    }

    private string GetLogLevelColor(LogLevel level) => level switch
    {
        LogLevel.Trace => _options.TraceColor,
        LogLevel.Debug => _options.DebugColor,
        LogLevel.Information => _options.InformationColor,
        LogLevel.Warning => _options.WarningColor,
        LogLevel.Error => _options.ErrorColor,
        LogLevel.Critical => _options.CriticalColor,
        _ => "white"
    };

    private void WriteOutput(string time, string timeColor, string level, string levelColor, 
        string category, string message, Exception? exception)
    {
        var prefix = string.Format(_options.PrefixFormat, time, level);
        var suffix = string.Format(_options.SuffixFormat, category);
        
        var levelMarkup = $"[{levelColor}]{level}[/]";
        var categoryMarkup = $"[{_options.CategoryColor}]{suffix}[/]";
        var messageMarkup = message;
        
        var output = $"{time}|{levelMarkup}|{messageMarkup} {categoryMarkup}";
        
        if (exception != null)
        {
            output += $" [red]{exception.Message}[/]";
        }

        AnsiConsole.MarkupLine(output);
    }

    public ValueTask DisposeAsync() => default;
}
```

---

### Task 3: 修改 ZLoggerSpectreExtensions.cs - 设置默认颜色配置

**Files:**
- Modify: `src/Framework.ZLogging/ZLoggerSpectreExtensions.cs`

- [ ] **Step 1: 修改 AddZLoggerSpectreConsole 默认配置**

```csharp
public static ILoggingBuilder AddZLoggerSpectreConsole(
    this ILoggingBuilder builder)
{
    var options = new ZLoggerSpectreConsoleOptions
    {
        EnableAnsi = true,
        UseTime = true,
        TimeFormat = "yyyy-MM-dd HH:mm:ss"
    };

    options.UsePlainTextFormatter(formatter =>
    {
        formatter.SetPrefixFormatter("{0}|{1}|", (template, info) =>
            $"{info.Timestamp:HH:mm:ss}|{info.LogLevel}|");

        formatter.SetSuffixFormatter(" ({0})", (template, info) =>
            $"{info.Category}");

        formatter.SetExceptionFormatter((console, ex) =>
            console.MarkupLine($"[red]{ex.Message}[/]"));
        
        formatter.SetLogLevelColor(LogLevel.Trace, "grey");
        formatter.SetLogLevelColor(LogLevel.Debug, "grey");
        formatter.SetLogLevelColor(LogLevel.Information, "green");
        formatter.SetLogLevelColor(LogLevel.Warning, "yellow");
        formatter.SetLogLevelColor(LogLevel.Error, "red");
        formatter.SetLogLevelColor(LogLevel.Critical, "red bold");
    });

    builder.AddProvider(new ZLoggerSpectreConsoleLoggerProvider(options));
    return builder;
}
```

---

### Task 4: 测试验证

**Files:**
- Test: 运行现有项目测试

- [ ] **Step 1: 编译项目**

```bash
dotnet build src/Framework.ZLogging/Framework.ZLogging.csproj
```

Expected: 编译成功

- [ ] **Step 2: 运行测试**

```bash
dotnet test
```

Expected: 测试通过

---

### Task 5: 提交

- [ ] **Step 1: 提交更改**

```bash
git add src/Framework.ZLogging/
git commit -m "refactor: add ZLoggerSpectre color configuration"
```