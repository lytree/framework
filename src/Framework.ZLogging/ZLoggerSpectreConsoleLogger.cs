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
    }

    private class ScopeDisposable : IDisposable
    {
        private readonly Action _dispose;
        public ScopeDisposable(Action dispose) => _dispose = dispose;
        public void Dispose() => _dispose();
    }
}