using Spectre.Console;
using Spectre.Console.Rendering;

namespace Framework.Cli;

public sealed class LAnsiConsoleWriter : IAnsiConsole
{
    private readonly IAnsiConsole _console;
    private readonly string _filePath;
    private readonly string _prefix;
    private readonly string _suffix;
    private readonly object _lock = new();

    private static LAnsiConsoleWriter? _instance;
    private static string? _instanceFilePath;
    private static string? _instancePrefix;
    private static string? _instanceSuffix;

    private LAnsiConsoleWriter(IAnsiConsole console, string filePath, string? prefix, string? suffix)
    {
        _console = console;
        _filePath = filePath;
        _prefix = prefix ?? string.Empty;
        _suffix = suffix ?? string.Empty;
    }

    public static LAnsiConsoleWriter Create(string filePath, string? prefix = null, string? suffix = null)
    {
        if (_instance != null && 
            _instanceFilePath == filePath && 
            _instancePrefix == prefix && 
            _instanceSuffix == suffix)
        {
            return _instance;
        }

        _instance = new LAnsiConsoleWriter(Spectre.Console.AnsiConsole.Console, filePath, prefix, suffix);
        _instanceFilePath = filePath;
        _instancePrefix = prefix;
        _instanceSuffix = suffix;
        return _instance;
    }

    public static LAnsiConsoleWriter Create(IAnsiConsole console, string filePath, string? prefix = null, string? suffix = null)
    {
        return new LAnsiConsoleWriter(console, filePath, prefix, suffix);
    }

    public static LAnsiConsoleWriter? GetInstance() => _instance;

    private void WriteToFile(string message)
    {
        if (string.IsNullOrEmpty(_filePath))
            return;

        lock (_lock)
        {
            using var stream = new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream);
            writer.Write(_prefix);
            writer.Write(message);
            writer.WriteLine(_suffix);
            writer.Flush();
        }
    }

    public void LWrite(string message, bool writeToFile = false)
    {
        _console.Write(message);
        if (writeToFile)
        {
            WriteToFile(message);
        }
    }

    public void LWriteLine(string message = "", bool writeToFile = false)
    {
        _console.WriteLine(message);
        if (writeToFile)
        {
            WriteToFile(message);
        }
    }

    public void LMarkup(string markup, bool writeToFile = false)
    {
        _console.Markup(markup);
        if (writeToFile)
        {
            WriteToFile(markup);
        }
    }

    public void LMarkupLine(string markup, bool writeToFile = false)
    {
        _console.MarkupLine(markup);
        if (writeToFile)
        {
            WriteToFile(markup);
        }
    }

    public static void LWrite(string message, string filePath, bool writeToFile = false, string? prefix = null, string? suffix = null)
    {
        var writer = Create(filePath, prefix, suffix);
        writer.LWrite(message, writeToFile);
    }

    public static void LWriteLine(string message = "", string filePath = "", bool writeToFile = false, string? prefix = null, string? suffix = null)
    {
        var writer = Create(filePath, prefix, suffix);
        writer.LWriteLine(message, writeToFile);
    }

    public static void LMarkup(string markup, string filePath, bool writeToFile = false, string? prefix = null, string? suffix = null)
    {
        var writer = Create(filePath, prefix, suffix);
        writer.LMarkup(markup, writeToFile);
    }

    public static void LMarkupLine(string markup, string filePath, bool writeToFile = false, string? prefix = null, string? suffix = null)
    {
        var writer = Create(filePath, prefix, suffix);
        writer.LMarkupLine(markup, writeToFile);
    }

    Profile IAnsiConsole.Profile => _console.Profile;
    IAnsiConsoleCursor IAnsiConsole.Cursor => _console.Cursor;
    IAnsiConsoleInput IAnsiConsole.Input => _console.Input;
    IExclusivityMode IAnsiConsole.ExclusivityMode => _console.ExclusivityMode;
    RenderPipeline IAnsiConsole.Pipeline => _console.Pipeline;

    void IAnsiConsole.Clear(bool home) => _console.Clear(home);

    void IAnsiConsole.Write(IRenderable renderable)
    {
        _console.Write(renderable);
    }
}