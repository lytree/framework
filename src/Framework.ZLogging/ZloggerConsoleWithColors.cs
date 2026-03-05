
using Microsoft.Extensions.Logging;
using ZLogger;
using ZLogger.Formatters;
using ZLogger.Providers;

namespace ZLogger;

public enum LogVerbosity
{
    TimeOnlyLocalLogLevel,
    DataTimeUtcLogLevelCategory,
    LogLevelLineColor
}

public class AddZLoggerConsoleWithColorsOptions
{
    public LogVerbosity LogVerbosity { get; set; } = LogVerbosity.TimeOnlyLocalLogLevel;
}

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddZLoggerConsoleWithColors(this ILoggingBuilder builder,
        Action<AddZLoggerConsoleWithColorsOptions> action)
    {
        var options = new AddZLoggerConsoleWithColorsOptions();
        action.Invoke(options);
        return builder.AddZLoggerConsoleWithColors(options);
    }

    public static ILoggingBuilder AddZLoggerConsoleWithColors(this ILoggingBuilder builder)
    {
        var options = new AddZLoggerConsoleWithColorsOptions();
        return builder.AddZLoggerConsoleWithColors(options);
    }

    public static ILoggingBuilder AddZLoggerConsoleWithColors(this ILoggingBuilder builder,
        AddZLoggerConsoleWithColorsOptions options)
    {
        builder.AddZLoggerConsole(o =>
        {
            o.UsePlainTextFormatter(formatter =>
            {
                switch (options.LogVerbosity)
                {
                    case LogVerbosity.TimeOnlyLocalLogLevel:
                        LocalTimeOnlyLogLevelFormater(formatter);
                        break;
                    case LogVerbosity.DataTimeUtcLogLevelCategory:
                        DataTimeUtcLogLevelCategoryFormater(formatter);
                        break;
                    case LogVerbosity.LogLevelLineColor:
                        LogLevelLineColorFormater(formatter);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        });
        return builder;

        void LocalTimeOnlyLogLevelFormater(PlainTextZLoggerFormatter formatter)
        {
            formatter.SetPrefixFormatter($"{0:local-timeonly} {1}{2:short}{3} ",
                (in MessageTemplate template, in LogInfo i) =>
                {
                    switch (i.LogLevel)
                    {
                        case LogLevel.Trace:
                            template.Format(
                                i.Timestamp,
                                "\u001b[38;2;200;200;200m[", i.LogLevel, "]\u001b[0m");
                            break;
                        case LogLevel.Debug:
                            template.Format(
                                i.Timestamp,
                                "[", i.LogLevel, "]");
                            break;
                        case LogLevel.Information:
                            template.Format(
                                i.Timestamp,
                                "\u001B[36m[", i.LogLevel, "]\u001b[0m");
                            break;
                        case LogLevel.Warning:
                            template.Format(
                                i.Timestamp,
                                "\u001b[33m[", i.LogLevel, "]\u001b[0m");
                            break;
                        case LogLevel.Critical:
                        case LogLevel.Error:
                            template.Format(
                                i.Timestamp,
                                "\u001b[31m[", i.LogLevel, "]\u001b[0m");
                            break;

                        default:
                            template.Format(
                                i.Timestamp,
                                "\u001b[36m[", i.LogLevel, "]\u001b[0m");
                            break;
                    }
                });
        }

        void DataTimeUtcLogLevelCategoryFormater(PlainTextZLoggerFormatter formatter)
        {
            formatter.SetPrefixFormatter($"{0:utc-datetime}|{1}{2:short}{3}|{4}|",
                (in MessageTemplate template, in LogInfo i) =>
                {
                    switch (i.LogLevel)
                    {
                        case LogLevel.Trace:
                            template.Format(
                                i.Timestamp,
                                "\u001b[38;2;200;200;200m", i.LogLevel, "\u001b[0m",
                                i.Category);
                            break;
                        case LogLevel.Debug:
                            template.Format(
                                i.Timestamp,
                                "", i.LogLevel, "",
                                i.Category);
                            break;
                        case LogLevel.Information:
                            template.Format(
                                i.Timestamp,
                                "\u001B[36m", i.LogLevel, "\u001b[0m",
                                i.Category);
                            break;
                        case LogLevel.Warning:
                            template.Format(
                                i.Timestamp,
                                "\u001b[33m", i.LogLevel, "\u001b[0m",
                                i.Category);
                            break;
                        case LogLevel.Critical:
                        case LogLevel.Error:
                            template.Format(
                                i.Timestamp,
                                "\u001b[31m", i.LogLevel, "\u001b[0m",
                                i.Category);
                            break;

                        default:
                            template.Format(
                                i.Timestamp,
                                "\u001b[36m[", i.LogLevel, "]\u001b[0m",
                                i.Category);
                            break;
                    }
                });
        }

        void LogLevelLineColorFormater(PlainTextZLoggerFormatter formatter)
        {
            formatter.SetPrefixFormatter($"{0}{1:short}{2} ",
                (in MessageTemplate template, in LogInfo i) =>
                {
                    switch (i.LogLevel)
                    {
                        case LogLevel.Trace:

                            template.Format(
                                "\u001b[38;2;200;200;200m[", i.LogLevel, "]");
                            break;
                        case LogLevel.Debug:
                            template.Format(
                                "[", i.LogLevel, "]");
                            break;
                        case LogLevel.Information:
                            template.Format(
                                "\u001B[36m[", i.LogLevel, "]");
                            break;
                        case LogLevel.Warning:
                            template.Format(
                                "\u001b[33m[", i.LogLevel, "]");
                            break;
                        case LogLevel.Critical:
                        case LogLevel.Error:
                            template.Format(
                                "\u001b[31m[", i.LogLevel, "]");
                            break;

                        default:
                            template.Format(
                                "\u001b[36m[", i.LogLevel, "]");
                            break;
                    }
                });
            formatter.SetSuffixFormatter($"{0}",
                (in MessageTemplate template, in LogInfo i) =>
                {
                    switch (i.LogLevel)
                    {
                        case LogLevel.Trace:
                            template.Format(
                                "\u001b[0m");
                            break;
                        case LogLevel.Debug:
                            template.Format(
                                "");
                            break;
                        case LogLevel.Information:
                            template.Format(
                                "\u001b[0m");
                            break;
                        case LogLevel.Warning:
                            template.Format(
                                "\u001b[0m");
                            break;
                        case LogLevel.Critical:
                        case LogLevel.Error:
                            template.Format(
                                "\u001b[0m");
                            break;

                        default:
                            template.Format(
                                "\u001b[0m");
                            break;
                    }
                });
        }
    }


}

public class ZLoggerWithColorsProviderOptions : ZLoggerOptions
{
    public bool OutputEncodingToUtf8 { get; set; } = true;
    public bool ConfigureEnableAnsiEscapeCode { get; set; }
    public LogLevel LogToStandardErrorThreshold { get; set; } = LogLevel.None;
    public LogVerbosity LogVerbosity { get; set; } = LogVerbosity.TimeOnlyLocalLogLevel;

    public ZLoggerWithColorsProviderOptions()
    {
        this.UsePlainTextFormatter(formatter =>
        {
            switch (LogVerbosity)
            {
                case LogVerbosity.TimeOnlyLocalLogLevel:
                    LocalTimeOnlyLogLevelFormater(formatter);
                    break;
                case LogVerbosity.DataTimeUtcLogLevelCategory:
                    DataTimeUtcLogLevelCategoryFormater(formatter);
                    break;
                case LogVerbosity.LogLevelLineColor:
                    LogLevelLineColorFormater(formatter);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        });
    }
    private void LocalTimeOnlyLogLevelFormater(PlainTextZLoggerFormatter formatter)
    {
        formatter.SetPrefixFormatter($"{0:local-timeonly} {1}{2:short}{3} ",
            (in MessageTemplate template, in LogInfo i) =>
            {
                switch (i.LogLevel)
                {
                    case LogLevel.Trace:
                        template.Format(
                            i.Timestamp,
                            "\u001b[38;2;200;200;200m[", i.LogLevel, "]\u001b[0m");
                        break;
                    case LogLevel.Debug:
                        template.Format(
                            i.Timestamp,
                            "[", i.LogLevel, "]");
                        break;
                    case LogLevel.Information:
                        template.Format(
                            i.Timestamp,
                            "\u001B[36m[", i.LogLevel, "]\u001b[0m");
                        break;
                    case LogLevel.Warning:
                        template.Format(
                            i.Timestamp,
                            "\u001b[33m[", i.LogLevel, "]\u001b[0m");
                        break;
                    case LogLevel.Critical:
                    case LogLevel.Error:
                        template.Format(
                            i.Timestamp,
                            "\u001b[31m[", i.LogLevel, "]\u001b[0m");
                        break;

                    default:
                        template.Format(
                            i.Timestamp,
                            "\u001b[36m[", i.LogLevel, "]\u001b[0m");
                        break;
                }
            });
    }

    private void DataTimeUtcLogLevelCategoryFormater(PlainTextZLoggerFormatter formatter)
    {
        formatter.SetPrefixFormatter($"{0:utc-datetime}|{1}{2:short}{3}|{4}|",
            (in MessageTemplate template, in LogInfo i) =>
            {
                switch (i.LogLevel)
                {
                    case LogLevel.Trace:
                        template.Format(
                            i.Timestamp,
                            "\u001b[38;2;200;200;200m", i.LogLevel, "\u001b[0m",
                            i.Category);
                        break;
                    case LogLevel.Debug:
                        template.Format(
                            i.Timestamp,
                            "", i.LogLevel, "",
                            i.Category);
                        break;
                    case LogLevel.Information:
                        template.Format(
                            i.Timestamp,
                            "\u001B[36m", i.LogLevel, "\u001b[0m",
                            i.Category);
                        break;
                    case LogLevel.Warning:
                        template.Format(
                            i.Timestamp,
                            "\u001b[33m", i.LogLevel, "\u001b[0m",
                            i.Category);
                        break;
                    case LogLevel.Critical:
                    case LogLevel.Error:
                        template.Format(
                            i.Timestamp,
                            "\u001b[31m", i.LogLevel, "\u001b[0m",
                            i.Category);
                        break;

                    default:
                        template.Format(
                            i.Timestamp,
                            "\u001b[36m[", i.LogLevel, "]\u001b[0m",
                            i.Category);
                        break;
                }
            });
    }

    private void LogLevelLineColorFormater(PlainTextZLoggerFormatter formatter)
    {
        formatter.SetPrefixFormatter($"{0}{1:short}{2} ",
            (in MessageTemplate template, in LogInfo i) =>
            {
                switch (i.LogLevel)
                {
                    case LogLevel.Trace:

                        template.Format(
                            "\u001b[38;2;200;200;200m[", i.LogLevel, "]");
                        break;
                    case LogLevel.Debug:
                        template.Format(
                            "[", i.LogLevel, "]");
                        break;
                    case LogLevel.Information:
                        template.Format(
                            "\u001B[36m[", i.LogLevel, "]");
                        break;
                    case LogLevel.Warning:
                        template.Format(
                            "\u001b[33m[", i.LogLevel, "]");
                        break;
                    case LogLevel.Critical:
                    case LogLevel.Error:
                        template.Format(
                            "\u001b[31m[", i.LogLevel, "]");
                        break;

                    default:
                        template.Format(
                            "\u001b[36m[", i.LogLevel, "]");
                        break;
                }
            });
        formatter.SetSuffixFormatter($"{0}",
            (in MessageTemplate template, in LogInfo i) =>
            {
                switch (i.LogLevel)
                {
                    case LogLevel.Trace:
                        template.Format(
                            "\u001b[0m");
                        break;
                    case LogLevel.Debug:
                        template.Format(
                            "");
                        break;
                    case LogLevel.Information:
                        template.Format(
                            "\u001b[0m");
                        break;
                    case LogLevel.Warning:
                        template.Format(
                            "\u001b[0m");
                        break;
                    case LogLevel.Critical:
                    case LogLevel.Error:
                        template.Format(
                            "\u001b[0m");
                        break;

                    default:
                        template.Format(
                            "\u001b[0m");
                        break;
                }
            });
    }
}

public class ZLoggerWithColorsProvider : ILoggerProvider
{
    private readonly ZLoggerConsoleLoggerProvider _consoleLoggerProvider;

    public ZLoggerWithColorsProvider(ZLoggerWithColorsProviderOptions withColorsProviderOptions)
    {
        var o1 = new ZLoggerConsoleOptions
        {
            OutputEncodingToUtf8 = withColorsProviderOptions.OutputEncodingToUtf8,
            ConfigureEnableAnsiEscapeCode = withColorsProviderOptions.ConfigureEnableAnsiEscapeCode,
            LogToStandardErrorThreshold = withColorsProviderOptions.LogToStandardErrorThreshold
        };


        _consoleLoggerProvider = new ZLoggerConsoleLoggerProvider(o1);
    }

    public void Dispose()
    {
        _consoleLoggerProvider.Dispose();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _consoleLoggerProvider.CreateLogger(categoryName);
    }


}