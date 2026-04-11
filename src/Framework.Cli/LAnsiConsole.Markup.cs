using Spectre.Console;

namespace Framework.Cli;

public static partial class LAnsiConsole
{
    public static void Markup(string value)
    {
        Spectre.Console.AnsiConsole.Markup(value);
    }

    public static void Markup(string format, params object[] args)
    {
        Spectre.Console.AnsiConsole.Markup(format, args);
    }

    public static void MarkupLine(string value)
    {
        Spectre.Console.AnsiConsole.MarkupLine(value);
    }

    public static void MarkupLine(string format, params object[] args)
    {
        Spectre.Console.AnsiConsole.MarkupLine(format, args);
    }

    public static void MarkupInterpolated(FormattableString value)
    {
        Spectre.Console.AnsiConsole.MarkupInterpolated(value);
    }

    public static void MarkupLineInterpolated(FormattableString value)
    {
        Spectre.Console.AnsiConsole.MarkupLineInterpolated(value);
    }

    public static void LMarkup(string markup, string filePath, bool writeToFile = false, string? prefix = null, string? suffix = null)
    {
        LAnsiConsoleWriter.LMarkup(markup, filePath, writeToFile, prefix, suffix);
    }

    public static void LMarkupLine(string markup, string filePath, bool writeToFile = false, string? prefix = null, string? suffix = null)
    {
        LAnsiConsoleWriter.LMarkupLine(markup, filePath, writeToFile, prefix, suffix);
    }

    public static void LMarkup(string markup, bool writeToFile = false)
    {
        if (Console is LAnsiConsoleWriter writer)
        {
            writer.LMarkup(markup, writeToFile);
        }
    }

    public static void LMarkupLine(string markup, bool writeToFile = false)
    {
        if (Console is LAnsiConsoleWriter writer)
        {
            writer.LMarkupLine(markup, writeToFile);
        }
    }
}