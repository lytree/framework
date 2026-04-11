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
        _options = options;
        _processor = new SpectreConsoleLogProcessor(options);
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
        var timestamp = _options.UseTime ? logInfo.Timestamp.ToString() : "";
        var logLevel = logInfo.LogLevel.ToString();
        var category = logInfo.Category.ToString();
        var output = BuildOutput(timestamp, logLevel, category, message);
        Write(output);
        log.Return();
    }

    private string BuildOutput(string timestamp, string logLevel, string category, string message)
    {
        var levelColor = GetLogLevelAnsiColor(logLevel);

        if (!_options.EnableAnsi)
        {
            var prefix = string.Format(_options.PrefixFormat, timestamp, logLevel);
            var suffix = string.Format(_options.SuffixFormat, category);
            return $"{prefix} {message} {suffix}";
        }

        var levelStr = $"[{levelColor}]{logLevel}[/]";
        var categoryStr = $"[magenta]{category}[/]";
        return $"{timestamp}|{levelStr}| {message} {categoryStr}";
    }

    private string GetLogLevelAnsiColor(string logLevel)
    {
        return logLevel switch
        {
            "Trace" => _options.TraceColor,
            "Debug" => _options.DebugColor,
            "Warning" => _options.WarningColor,
            "Error" => _options.ErrorColor,
            "Critical" => _options.CriticalColor,
            _ => _options.InformationColor
        };
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