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


    public static IAnsiConsole Create(LAnsiConsoleSettings settings)
    {
        var console = Spectre.Console.AnsiConsole.Create(new Spectre.Console.AnsiConsoleSettings
        {
            Ansi = settings.Ansi,
            ColorSystem = settings.ColorSystem
        });

        if (!string.IsNullOrEmpty(settings.FilePath))
        {
            _writer = LAnsiConsoleWriter.Create(console, settings.FilePath, settings.Prefix, settings.Suffix);
            return _writer;
        }

        return console;
    }

    public static void Clear()
    {
        Console.Clear();
    }

}