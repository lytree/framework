using Spectre.Console;

namespace Framework.Cli;

public static partial class LAnsiConsole
{
    private static LAnsiConsoleWriter? _writer;
    private static Lazy<IAnsiConsole> _console = new Lazy<IAnsiConsole>(
        () => AnsiConsole.Console);

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


    public static IAnsiConsole Create(AnsiConsoleSettings settings)
    {

        return AnsiConsole.Create(settings);


    }

    public static void Clear()
    {
        Console.Clear();
    }

}