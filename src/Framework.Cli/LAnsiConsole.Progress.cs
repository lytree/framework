using Spectre.Console;

namespace Framework.Cli;

public static partial class LAnsiConsole
{
    public static Progress Progress()
    {
        return Spectre.Console.AnsiConsole.Progress();
    }

    public static Status Status()
    {
        return Spectre.Console.AnsiConsole.Status();
    }
}