using Framework;
using Spectre.Console;
using ZLogger;

namespace Framework.ZLogging;

[Obsolete("Use ZLoggerSpectreConsoleLoggerProvider instead")]
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


[Obsolete("Use ZLoggerSpectreConsoleLoggerProvider instead")]
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
        var logLevel = Helper.GetLogLevel(message);
        var levelColor = GetLogLevelAnsiColor(message);
        var levelStr = $"{levelColor}[{logLevel}][/]";
        var categoryColor = "magenta";
        var category = Helper.GetLogCategory(message);
        var categoryStr = $"{categoryColor}[{category}][/]";
        return $"{timestamp} {levelStr} {categoryStr} {message}";
    }

    private string GetLogLevelAnsiColor(string message)
    {
        if (message.Contains("Trace")) return "grey";
        if (message.Contains("Debug")) return "";
        if (message.Contains("Warning") || message.Contains("Warn")) return "yellow";
        if (message.Contains("Error") || message.Contains("Fail") || message.Contains("Critical") || message.Contains("Fatal")) return "red";
        return "cyan";
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