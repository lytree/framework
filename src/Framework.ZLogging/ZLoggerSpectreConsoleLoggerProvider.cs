using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Framework.ZLogging;

[ProviderAlias("ZLoggerSpectreConsole")]
public class ZLoggerSpectreConsoleLoggerProvider : ILoggerProvider, IAsyncDisposable
{
    private readonly ZLoggerSpectreConsoleOptions _options;
    private readonly ConcurrentDictionary<string, ZLoggerSpectreConsoleLogger> _loggers = new();
    private readonly IAsyncLogProcessor _processor;

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