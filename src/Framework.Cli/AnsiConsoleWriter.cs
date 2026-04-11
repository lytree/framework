using Spectre.Console;
using Spectre.Console.Rendering;

namespace Framework.Cli;

public sealed class AnsiConsoleWriter : IAnsiConsole
{
    private readonly IAnsiConsole _console;
    private readonly string _filePath;
    private readonly string _prefix;
    private readonly string _suffix;
    private readonly object _lock = new();

    private AnsiConsoleWriter(IAnsiConsole console, string filePath, string? prefix, string? suffix)
    {
        _console = console;
        _filePath = filePath;
        _prefix = prefix ?? string.Empty;
        _suffix = suffix ?? string.Empty;
    }

    public static AnsiConsoleWriter Create(IAnsiConsole console, string filePath, string? prefix = null, string? suffix = null)
    {
        return new AnsiConsoleWriter(console, filePath, prefix, suffix);
    }

    private void WriteToFile(string message)
    {
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

    public void WriteLine()
    {
        _console.WriteLine();
    }

    public void Markup(string markup, bool writeToFile = false)
    {
        _console.Markup(markup);
        if (writeToFile)
        {
            WriteToFile(markup);
        }
    }

    public void MarkupLine(string markup, bool writeToFile = false)
    {
        _console.MarkupLine(markup);
        if (writeToFile)
        {
            WriteToFile(markup);
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

    public void Write(IRenderable renderable, bool writeToFile = false)
    {
        ((IAnsiConsole)this).Write(renderable);
        if (writeToFile)
        {
            WriteToFile(renderable.ToString() ?? string.Empty);
        }
    }

    public void WriteLine(IRenderable renderable, bool writeToFile = false)
    {
        ((IAnsiConsole)this).Write(renderable);
        ((IAnsiConsole)this).WriteLine();
        if (writeToFile)
        {
            WriteToFile(renderable.ToString() ?? string.Empty);
        }
    }
}