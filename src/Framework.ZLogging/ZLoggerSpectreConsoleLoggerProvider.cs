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

        var timestamp = _options.UseTime ? DateTime.Now.ToString(_options.TimeFormat) + " " : "";
        var logLevel = GetLogLevel(message);
        var levelColor = GetLogLevelAnsiColor(message);
        var levelStr = $"{levelColor}[{logLevel}][/]";
        var categoryColor = "magenta";
        var category = GetLogCategory(message);
        var categoryStr = $"{categoryColor}[{category}][/]";
        return $"{timestamp}{levelStr} {categoryStr} {message}";
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