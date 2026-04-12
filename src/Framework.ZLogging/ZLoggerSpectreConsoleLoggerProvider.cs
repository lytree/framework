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
    private readonly SpectreConsoleLogProcessor _processor;

    public ZLoggerSpectreConsoleLoggerProvider(ZLoggerSpectreConsoleOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _processor = new SpectreConsoleLogProcessor(options);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName,
            new ZLoggerSpectreConsoleLogger(categoryName, _options, _processor));
    }

    public void Dispose()
    {
        _processor.DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
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
        var timestampStr = logInfo.Timestamp.ToString();
        var timestamp = _options.UseTime ? timestampStr : "";
        var hour = int.Parse(timestampStr.Substring(0, 2));
        var timeColor = GetTimeColor(hour);
        var logLevelStr = logInfo.LogLevel.ToString();
        var categoryStr = logInfo.Category.ToString();
        var exception = logInfo.Exception;
        var levelColor = GetLogLevelColor(logInfo.LogLevel);
        var categoryColor = _options.CategoryColor;
        var output = BuildOutput(timestamp, logLevelStr, categoryStr, message, exception, timeColor, levelColor, categoryColor);
        Write(output);
        log.Return();
    }

    private string GetTimeColor(int hour)
    {
        if (hour >= 6 && hour < 12) return _options.TimeOnlyColor06;
        if (hour >= 12 && hour < 18) return _options.TimeOnlyColor12;
        if (hour >= 18 && hour < 24) return _options.TimeOnlyColor18;
        return _options.TimeOnlyColor00;
    }

    private string GetLogLevelColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => _options.TraceColor,
            LogLevel.Debug => _options.DebugColor,
            LogLevel.Information => _options.InformationColor,
            LogLevel.Warning => _options.WarningColor,
            LogLevel.Error => _options.ErrorColor,
            LogLevel.Critical => _options.CriticalColor,
            _ => _options.InformationColor
        };
    }

    private string BuildOutput(string timestamp, string logLevel, string category, string message, Exception? exception, string timeColor, string levelColor, string categoryColor)
    {
        var prefix = _options.EnableAnsi 
            ? $"{timestamp}|[{timeColor}]{logLevel}[/]|"
            : $"{timestamp}|{logLevel}|";
        var suffix = _options.EnableAnsi 
            ? $" ([{categoryColor}]{category}[/])"
            : $" ({category})";

        if (!_options.EnableAnsi)
        {
            var exceptionText = exception != null ? string.Format(_options.ExceptionFormat, exception.Message) : "";
            return $"{prefix} {message} {suffix} {exceptionText}".Trim();
        }
        var exceptionStr = exception != null ? $" [red]{exception.Message}[/]" : "";
        return $"{prefix} {message} {suffix}{exceptionStr}";
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