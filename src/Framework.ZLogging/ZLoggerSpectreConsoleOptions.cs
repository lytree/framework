using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Collections.Frozen;
using System.Text;
using ZLogger;
using ZLogger.Formatters;

namespace Framework.ZLogging;

public sealed class ZLoggerSpectreConsoleOptions
{
    public string TimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

    private FrozenDictionary<LogLevel, string> _logLevelColors = CreateDefaultLogLevelColors();

    /// <summary>
    /// 各 <see cref="LogLevel"/> 对应的 Spectre.Console 颜色标记。
    /// 使用 <see cref="FrozenDictionary"/> 以获得更快的查找性能并防止运行时误修改。
    /// 设置时会自动冻结输入字典。
    /// </summary>
    public FrozenDictionary<LogLevel, string> LogLevelColors
    {
        get => _logLevelColors;
        set => _logLevelColors = value ?? CreateDefaultLogLevelColors();
    }

    public string CategoryColor { get; set; } = "cyan";
    public int BoundedChannelSize { get; set; } = 1024;
    public bool IncludeScopes { get; set; } = false;
    public TimeProvider? TimeProvider { get; set; }
    public Action<Exception>? InternalErrorLogger { get; set; }
    public Action<IAnsiConsole, Exception>? ExceptionFormatter { get; set; }

    public string? FilePath { get; set; }
    public Encoding FileEncoding { get; set; } = Encoding.UTF8;
    public bool FileAppend { get; set; } = true;

    private Func<IZLoggerFormatter> formatterFactory = static () => new PlainTextZLoggerFormatter();

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
        UseFormatter(() =>
        {
            var f = new PlainTextZLoggerFormatter();
            configure?.Invoke(f);
            return f;
        });
        return this;
    }

    private static FrozenDictionary<LogLevel, string> CreateDefaultLogLevelColors()
        => new Dictionary<LogLevel, string>
        {
            { LogLevel.Trace, "grey" },
            { LogLevel.Debug, "grey" },
            { LogLevel.Information, "green" },
            { LogLevel.Warning, "yellow" },
            { LogLevel.Error, "red" },
            { LogLevel.Critical, "red bold" }
        }.ToFrozenDictionary();
}
