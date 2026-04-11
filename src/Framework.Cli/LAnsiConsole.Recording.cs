using Spectre.Console;

namespace Framework.Cli;

public static partial class LAnsiConsole
{
    public static void Record()
    {
        Spectre.Console.AnsiConsole.Record();
    }

    public static string ExportText()
    {
        return Spectre.Console.AnsiConsole.ExportText();
    }

    public static string ExportHtml()
    {
        return Spectre.Console.AnsiConsole.ExportHtml();
    }
}