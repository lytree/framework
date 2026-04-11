using System;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Framework.ZLogging;

public class ZLoggerSpectreConsoleOptions
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

    public string PrefixFormat { get; set; } = "{0}|{1}|";
    public string SuffixFormat { get; set; } = " ({0})";
    public string ExceptionFormat { get; set; } = "{0}";

    public void UsePlainTextFormatter(Action<PlainTextFormatterConfig> configure)
    {
        var config = new PlainTextFormatterConfig(this);
        configure?.Invoke(config);
    }

    public class PlainTextFormatterConfig
    {
        private readonly ZLoggerSpectreConsoleOptions _options;

        internal PlainTextFormatterConfig(ZLoggerSpectreConsoleOptions options)
        {
            _options = options;
        }

        public void SetPrefixFormatter(string format, Func<MessageTemplate, LogInfo, string> formatter)
        {
            _options.PrefixFormat = format;
        }

        public void SetSuffixFormatter(string format, Func<MessageTemplate, LogInfo, string> formatter)
        {
            _options.SuffixFormat = format;
        }

        public void SetExceptionFormatter(Action<object, Exception> formatter)
        {
            _options.ExceptionFormat = "{0}";
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
}