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

    public static LAnsiConsoleWriter Create(string filePath = "./info.log", string? prefix = null, string? suffix = null)
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

    public static LAnsiConsoleWriter Create(IAnsiConsole console, string filePath = "./info.log", string? prefix = null, string? suffix = null)
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

    public void Write(string message, bool writeToFile = false)
    {
        _console.Write(message);
        if (writeToFile)
        {
            WriteToFile(message);
        }
    }

    public void WriteLine(string message = "", bool writeToFile = false)
    {
        _console.WriteLine(message);
        if (writeToFile)
        {
            WriteToFile(message);
        }
    }

    public void Write(IRenderable renderable, bool writeToFile = false)
    {
        _console.Write(renderable);
        if (writeToFile)
        {
            var text = renderable.ToString();
            WriteToFile(text ?? string.Empty);
        }
    }

    public void WriteLine(IRenderable renderable, bool writeToFile = false)
    {
        _console.Write(renderable);
        _console.WriteLine();
        if (writeToFile)
        {
            var text = renderable.ToString();
            WriteToFile(text ?? string.Empty);
        }
    }

    public void Markup(string markup, bool writeToFile = false)
    {
        _console.Markup(markup);
        if (writeToFile)
        {
            WriteToFile(Spectre.Console.Markup.Remove(markup));
        }
    }

    public void Markup(string format, params object[] args)
    {
        _console.Markup(format, args);
    }

    public void MarkupLine(string markup, bool writeToFile = false)
    {
        _console.MarkupLine(markup);
        if (writeToFile)
        {
            WriteToFile(Spectre.Console.Markup.Remove(markup));
        }
    }

    public void Markup(string format, bool writeToFile, params object[] args)
    {
        _console.Markup(format, args);
        if (writeToFile)
        {
            WriteToFile(string.Format(format, args));
        }
    }

    public void MarkupLine(string format, bool writeToFile, params object[] args)
    {
        _console.MarkupLine(format, args);
        if (writeToFile)
        {
            WriteToFile(string.Format(format, args));
        }
    }

    public void MarkupInterpolated(FormattableString value, bool writeToFile = false)
    {
        _console.MarkupInterpolated(value);
        if (writeToFile)
        {
            WriteToFile(value.ToString());
        }
    }

    public void MarkupLineInterpolated(FormattableString value, bool writeToFile = false)
    {
        _console.MarkupLineInterpolated(value);
        if (writeToFile)
        {
            WriteToFile(value.ToString());
        }
    }

    public void WriteException(Exception exception, Spectre.Console.ExceptionFormats format = Spectre.Console.ExceptionFormats.Default, bool writeToFile = false)
    {
        _console.WriteException(exception, format);
        if (writeToFile)
        {
            WriteToFile(exception.ToString() ?? string.Empty);
        }
    }

    public void WriteException(Exception exception, Spectre.Console.ExceptionSettings settings, bool writeToFile = false)
    {
        _console.WriteException(exception, settings);
        if (writeToFile)
        {
            WriteToFile(exception.ToString() ?? string.Empty);
        }
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
            WriteToFile(Spectre.Console.Markup.Remove(markup));
        }
    }

    public void LMarkupLine(string markup, bool writeToFile = false)
    {
        _console.MarkupLine(markup);
        if (writeToFile)
        {
            WriteToFile(Spectre.Console.Markup.Remove(markup));
        }
    }
}