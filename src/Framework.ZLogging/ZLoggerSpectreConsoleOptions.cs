using Microsoft.Extensions.Logging;
using Spectre.Console;
using ZLogger;
using ZLogger.Formatters;

namespace Framework.ZLogging;

public class ZLoggerSpectreConsoleOptions
{
    public string TimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

    public Dictionary<LogLevel, string> LogLevelColors { get; set; } = new()
    {
        { LogLevel.Trace, "grey" },
        { LogLevel.Debug, "grey" },
        { LogLevel.Information, "green" },
        { LogLevel.Warning, "yellow" },
        { LogLevel.Error, "red" },
        { LogLevel.Critical, "red bold" }
    };
    private Func<IZLoggerFormatter> formatterFactory = DefaultFormatterFactory;
    public string CategoryColor { get; set; } = "cyan";
    public int BoundedChannelSize { get; set; } = 1024;
    public bool IncludeScopes { get; set; } = false;
    public TimeProvider? TimeProvider { get; set; }
    public Action<Exception>? InternalErrorLogger { get; set; }
    public Action<IAnsiConsole, Exception>? ExceptionFormatter { get; set; }

    
    public void SetExceptionFormatter(Action<IAnsiConsole, Exception> formatter)
    {
        ExceptionFormatter = formatter;
    }
    public IZLoggerFormatter CreateFormatter()
    {
        return formatterFactory();
    }

    public ZLoggerSpectreConsoleOptions UseFormatter(Func<IZLoggerFormatter> formatterFactory)
    {
        this.formatterFactory = formatterFactory;
        return this;
    }

    public ZLoggerSpectreConsoleOptions UsePlainTextFormatter(Action<PlainTextZLoggerFormatter>? configure = null)
    {
        UseFormatter(delegate
        {
            PlainTextZLoggerFormatter plainTextZLoggerFormatter = new();
            configure?.Invoke(plainTextZLoggerFormatter);
            return plainTextZLoggerFormatter;
        });
        return this;
    }

    private static IZLoggerFormatter DefaultFormatterFactory()
    {
        return new PlainTextZLoggerFormatter();
    }
}