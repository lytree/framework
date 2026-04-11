using Spectre.Console;
using ZLogger;

namespace Framework.ZLogging;

public class SpectreConsoleLogProcessorOptions
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


public class SpectreConsoleProcessor : IAsyncLogProcessor
{
    private readonly SpectreConsoleLogProcessorOptions _options;
    private readonly TextWriter _writer;
    private readonly bool _stripMarkup;

    public SpectreConsoleProcessor(SpectreConsoleLogProcessorOptions options, TextWriter? writer = null)
    {
        _options = options;
        _writer = writer ?? Console.Out;
        _stripMarkup = writer != null || !Console.IsOutputRedirected && !Console.IsErrorRedirected;
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
        var categoryColor = "\u001b[35m";
        var category = GetLogCategory(message);
        var categoryStr = $"{categoryColor}[{category}][/]";
        return $"{timestamp} {levelStr} {categoryStr} {message}";
    }

    private string GetLogLevel(string message)
    {
        if (message.Contains("Trace")) return "TRACE";
        if (message.Contains("Debug")) return "DEBUG";
        if (message.Contains("Information") || message.Contains("Info")) return "INFO";
        if (message.Contains("Warning") || message.Contains("Warn")) return "WARN";
        if (message.Contains("Error") || message.Contains("Fail")) return "ERROR";
        if (message.Contains("Critical") || message.Contains("Fatal")) return "CRITICAL";
        return "INFO";
    }

    private string GetLogCategory(string message)
    {
        var start = message.IndexOf('[');
        var end = message.IndexOf(']');
        if (start >= 0 && end > start)
        {
            return message.Substring(start + 1, end - start - 1);
        }
        return "";
    }

    private string GetLogLevelAnsiColor(string message)
    {
        if (message.Contains("Trace")) return "\u001b[38;2;200;200;200m";
        if (message.Contains("Debug")) return "";
        if (message.Contains("Warning") || message.Contains("Warn")) return "\u001b[33m";
        if (message.Contains("Error") || message.Contains("Fail") || message.Contains("Critical") || message.Contains("Fatal")) return "\u001b[31m";
        return "\u001b[36m";
    }

    private void Write(string message)
    {
        if (_options.EnableAnsi)
        {
            AnsiConsole.MarkupLine(message);
        }
        else
        {
            _writer.WriteLine(Markup.Remove(message));
        }
    }

    public ValueTask DisposeAsync()
    {
        return default;
    }
}