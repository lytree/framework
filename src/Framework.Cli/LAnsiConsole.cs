using Spectre.Console;

namespace Framework.Cli;

public static partial class LAnsiConsole
{
    private static LAnsiConsoleWriter? _writer;
    private static Lazy<IAnsiConsole> _console = new Lazy<IAnsiConsole>(
        () => Spectre.Console.AnsiConsole.Console);

    public static IAnsiConsole Console
    {
        get
        {
            return _writer ?? _console.Value;
        }
        set
        {
            _console = new Lazy<IAnsiConsole>(() => value);
        }
    }

    public static IAnsiConsoleCursor Cursor => _console.Value.Cursor;

    public static Profile Profile => Console.Profile;

    public static IAnsiConsole Create(Spectre.Console.AnsiConsoleSettings settings)
    {
        return Spectre.Console.AnsiConsole.Create(settings);
    }

    public static void Clear()
    {
        Console.Clear();
    }

    public static void Configure(string filePath, string? prefix = null, string? suffix = null)
    {
        _writer = LAnsiConsoleWriter.Create(Spectre.Console.AnsiConsole.Console, filePath, prefix, suffix);
    }
}