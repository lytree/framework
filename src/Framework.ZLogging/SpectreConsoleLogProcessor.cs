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

        if (_options.UseTime)
        {
            var timestamp = $"[{_options.TraceColor}]{DateTime.Now.ToString(_options.TimeFormat)}[/]";
            return $"{timestamp} {message}";
        }

        return message;
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