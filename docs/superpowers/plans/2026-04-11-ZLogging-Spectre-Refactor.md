# ZLogging Spectre Console Provider 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 创建 ZLoggerSpectreConsoleLoggerProvider 替代 SpectreConsoleProcessor，并添加扩展方法

**Architecture:** 创建符合 ZLogger Provider 模式的 ZLoggerSpectreConsoleLoggerProvider，实现 ILoggerProvider 接口，同时保留现有 SpectreConsoleProcessor 并标记为 Obsolete

**Tech Stack:** C# .NET, Spectre.Console, ZLogger, Microsoft.Extensions.Logging

---

## 任务 0: 准备工作

**Files:**
- Read: `src/Framework.ZLogging/SpectreConsoleLogProcessor.cs`
- Read: `src/Framework.ZLogging/ZLoggerSpectreExtensions.cs`
- Read: `src/Framework.ZLogging/Framework.ZLogging.csproj`

- [ ] 创建 git worktree (如果需要独立分支): `git worktree add ../framework-zlogging-refactor -b refactor/zlogging-spectre`

---

## 任务 1: 创建 ZLoggerSpectreConsoleOptions 配置类

**Files:**
- Create: `src/Framework.ZLogging/ZLoggerSpectreConsoleOptions.cs`

- [ ] **Step 1: 创建 ZLoggerSpectreConsoleOptions.cs**

```csharp
using System;
using ZLogger;

namespace Framework.ZLogging;

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

---

## 任务 2: 创建 ZLoggerSpectreConsoleLogger

**Files:**
- Create: `src/Framework.ZLogging/ZLoggerSpectreConsoleLogger.cs`

- [ ] **Step 1: 创建 ZLoggerSpectreConsoleLogger.cs**

```csharp
using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Framework.ZLogging;

public class ZLoggerSpectreConsoleLogger : ILogger
{
    private readonly string _categoryName;
    private readonly ZLoggerSpectreConsoleOptions _options;
    private readonly IAsyncLogProcessor _processor;
    private readonly ConcurrentDictionary<string, object?> _scopes = new();

    public ZLoggerSpectreConsoleLogger(
        string categoryName,
        ZLoggerSpectreConsoleOptions options,
        IAsyncLogProcessor processor)
    {
        _categoryName = categoryName;
        _options = options;
        _processor = processor;
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public IDisposable? BeginScope<T>(T state) where T : notnull
    {
        var scopeId = Guid.NewGuid().ToString();
        _scopes[scopeId] = state;
        return new ScopeDisposable(() => _scopes.TryRemove(scopeId, out _));
    }

    public void Log<T>(LogLevel logLevel, EventId eventId, T state, Exception? exception, Func<T, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        var entry = ZLoggerEntry.Create(logLevel, eventId, _categoryName, message, exception);
        _processor.Post(entry);
    }

    private class ScopeDisposable : IDisposable
    {
        private readonly Action _dispose;
        public ScopeDisposable(Action dispose) => _dispose = dispose;
        public void Dispose() => _dispose();
    }
}
```

---

## 任务 3: 创建 ZLoggerSpectreConsoleLoggerProvider

**Files:**
- Create: `src/Framework.ZLogging/ZLoggerSpectreConsoleLoggerProvider.cs`

- [ ] **Step 1: 创建 ZLoggerSpectreConsoleLoggerProvider.cs**

```csharp
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using ZLogger;

namespace Framework.ZLogging;

[ProviderAlias("ZLoggerSpectreConsole")]
public class ZLoggerSpectreConsoleLoggerProvider : ILoggerProvider, IAsyncDisposable
{
    private readonly ZLoggerSpectreConsoleOptions _options;
    private readonly ConcurrentDictionary<string, ZLoggerSpectreConsoleLogger> _loggers = new();
    private readonly SpectreConsoleProcessor _processor;

    public ZLoggerSpectreConsoleLoggerProvider(ZLoggerSpectreConsoleOptions options)
    {
        _options = options;
        _processor = new SpectreConsoleProcessor(options);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, 
            new ZLoggerSpectreConsoleLogger(categoryName, _options, _processor));
    }

    public void Dispose()
    {
        _processor.DisposeAsync().AsTask().Wait();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var logger in _loggers)
        {
            if (logger.Value is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }
        }
        _loggers.Clear();
        await _processor.DisposeAsync();
    }
}

internal class SpectreConsoleProcessor : IAsyncLogProcessor
{
    private readonly ZLoggerSpectreConsoleOptions _options;
    private readonly bool _stripMarkup;

    public SpectreConsoleProcessor(ZLoggerSpectreConsoleOptions options)
    {
        _options = options;
        _stripMarkup = !Console.IsOutputRedirected && !Console.IsErrorRedirected;
    }

    public void Post(IZLoggerEntry log)
    {
        var message = log.ToString();
        if (string.IsNullOrEmpty(message))
        {
            log.Return();
            return;
        }

        var output = BuildOutput(message);
        Write(output);
        log.Return();
    }

    private string BuildOutput(string message)
    {
        if (!_options.EnableAnsi)
        {
            return message;
        }

        var timestamp = DateTime.Now.ToString(_options.TimeFormat);
        var logLevel = GetLogLevel(message);
        var levelColor = GetLogLevelAnsiColor(message);
        var levelStr = $"{levelColor}[{logLevel}][/]";
        var categoryColor = "magenta";
        var category = GetLogCategory(message);
        var categoryStr = $"{categoryColor}[{category}][/]";
        return $"{timestamp} {levelStr} {categoryStr} {message}";
    }

    private static string GetLogLevel(string message)
    {
        if (message.Contains("Trace")) return "Trace";
        if (message.Contains("Debug")) return "Debug";
        if (message.Contains("Warning") || message.Contains("Warn")) return "Warning";
        if (message.Contains("Error") || message.Contains("Fail")) return "Error";
        if (message.Contains("Critical") || message.Contains("Fatal")) return "Critical";
        return "Information";
    }

    private static string GetLogCategory(string message)
    {
        var start = message.LastIndexOf('[');
        var end = message.LastIndexOf(']');
        if (start >= 0 && end > start)
        {
            return message[(start + 1)..end];
        }
        return "";
    }

    private string GetLogLevelAnsiColor(string message)
    {
        if (message.Contains("Trace")) return _options.TraceColor;
        if (message.Contains("Debug")) return _options.DebugColor;
        if (message.Contains("Warning") || message.Contains("Warn")) return _options.WarningColor;
        if (message.Contains("Error") || message.Contains("Fail") || message.Contains("Critical") || message.Contains("Fatal")) return _options.ErrorColor;
        return _options.InformationColor;
    }

    private void Write(string message)
    {
        if (_options.EnableAnsi)
        {
            AnsiConsole.MarkupLine(message);
        }
        else
        {
            Console.WriteLine(Markup.Remove(message));
        }
    }

    public ValueTask DisposeAsync() => default;
}
```

- [ ] **Step 2: 添加 ZLoggerLogger import 修复**

检查并修复 ZLoggerEntry 引用是否正确。如果 Build 失败，修复 import。

---

## 任务 4: 更新 ZLoggerSpectreExtensions 扩展方法

**Files:**
- Modify: `src/Framework.ZLogging/ZLoggerSpectreExtensions.cs`

- [ ] **Step 1: 添加 AddZLoggerSpectreConsole 方法**

在 ZLoggerSpectreExtensions 类中添加新方法:

```csharp
public static ILoggingBuilder AddZLoggerSpectreConsole(
    this ILoggingBuilder builder,
    Action<ZLoggerSpectreConsoleOptions>? configure = null)
{
    var options = new ZLoggerSpectreConsoleOptions();
    options.UsePlainTextFormatter(formatter =>
    {
        formatter.SetPrefixFormatter($"{0}|{1}|", (in MessageTemplate template, in LogInfo info) => template.Format(info.Timestamp, info.LogLevel));
        formatter.SetSuffixFormatter($" ({0})", (in MessageTemplate template, in LogInfo info) => template.Format(info.Category));
        formatter.SetExceptionFormatter((writer, ex) => Utf8String.Format(writer, $"{ex.Message}"));
    });
    configure?.Invoke(options);

    builder.AddZLogger(options);
    builder.AddProvider(new ZLoggerSpectreConsoleLoggerProvider(options));

    return builder;
}
```

- [ ] **Step 2: 修改文件并 Build**

使用 Edit 工具添加到 ZLoggerSpectreExtensions.cs 文件，然后运行 Build 验证。

---

## 任务 5: 标记旧类为 Obsolete

**Files:**
- Modify: `src/Framework.ZLogging/SpectreConsoleLogProcessor.cs`

- [ ] **Step 1: 添加 Obsolete 属性**

在 SpectreConsoleProcessor 和 SpectreConsoleLogProcessorOptions 类上添加:

```csharp
[Obsolete("Use ZLoggerSpectreConsoleLoggerProvider instead")]
public class SpectreConsoleLogProcessorOptions
{
    // ... existing code
}

[Obsolete("Use ZLoggerSpectreConsoleLoggerProvider instead")]
public class SpectreConsoleProcessor : IAsyncLogProcessor
{
    // ... existing code
}
```

---

## 任务 6: 验证和测试

**Files:**
- Run: `dotnet build src/Framework.ZLogging/Framework.ZLogging.csproj`

- [ ] **Step 1: Build 项目**

运行: `dotnet build src/Framework.ZLogging/Framework.ZLogging.csproj`
预期: 成功编译

- [ ] **Step 2: 运行已有测试**

如果有测试项目，运行: `dotnet test`
预期: 所有测试通过

---

## 任务 7: 提交

**Files:**
- Commit all changes

- [ ] **Step 1: 提交更改**

```bash
git add src/Framework.ZLogging/
git commit -m "refactor(zlogging): add ZLoggerSpectreConsoleLoggerProvider

- Add ZLoggerSpectreConsoleOptions for configuration
- Add ZLoggerSpectreConsoleLogger for individual logger instances
- Add ZLoggerSpectreConsoleLoggerProvider as main provider
- Update extensions with AddZLoggerSpectreConsole method
- Mark SpectreConsoleProcessor as obsolete
- Add Spectre Console output extensions"
```

---

## 执行方式

**计划完成并保存到 `docs/superpowers/plans/2026-04-11-ZLogging-Spectre-Refactor.md`。两种执行方式:**

**1. Subagent-Driven (推荐)** - 每个任务分配一个子代理，任务间 review，快速迭代

**2. Inline Execution** - 在当前会话中执行任务，使用 batch 执行与 checkpoints

**选择哪种方式?**