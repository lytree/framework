using Spectre.Console;
using Spectre.Console.Rendering;

namespace Framework.Cli;

public static partial class LAnsiConsole
{
    private static LAnsiConsoleWriter? _writer;

    public static void Configure(string filePath, string? prefix = null, string? suffix = null)
    {
        _writer = LAnsiConsoleWriter.Create(Spectre.Console.AnsiConsole.Console, filePath, prefix, suffix);
    }

    public static IAnsiConsole Console => _writer ?? Spectre.Console.AnsiConsole.Console;

    public static IAnsiConsoleCursor Cursor => Console.Cursor;
    public static Profile Profile => Console.Profile;

    public static IAnsiConsole Create(Spectre.Console.AnsiConsoleSettings settings)
    {
        return Spectre.Console.AnsiConsole.Create(settings);
    }

    public static void Clear()
    {
        Console.Clear();
    }
}