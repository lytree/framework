using Spectre.Console;

namespace Framework.Cli;

public static partial class LAnsiConsole
{
    public static void WriteException(Exception exception, Spectre.Console.ExceptionFormats format = Spectre.Console.ExceptionFormats.Default)
    {
        Spectre.Console.AnsiConsole.WriteException(exception, format);
    }

    public static void WriteException(Exception exception, Spectre.Console.ExceptionSettings settings)
    {
        Spectre.Console.AnsiConsole.WriteException(exception, settings);
    }
}