using Spectre.Console;

namespace Framework.Cli;

public static partial class LAnsiConsole
{
    public static void LWrite(string message, string filePath, bool writeToFile = false, string? prefix = null, string? suffix = null)
    {
        LAnsiConsoleWriter.LWrite(message, filePath, writeToFile, prefix, suffix);
    }

    public static void LWriteLine(string message = "", string filePath = "", bool writeToFile = false, string? prefix = null, string? suffix = null)
    {
        LAnsiConsoleWriter.LWriteLine(message, filePath, writeToFile, prefix, suffix);
    }

    public static void LMarkup(string markup, string filePath, bool writeToFile = false, string? prefix = null, string? suffix = null)
    {
        LAnsiConsoleWriter.LMarkup(markup, filePath, writeToFile, prefix, suffix);
    }

    public static void LMarkupLine(string markup, string filePath, bool writeToFile = false, string? prefix = null, string? suffix = null)
    {
        LAnsiConsoleWriter.LMarkupLine(markup, filePath, writeToFile, prefix, suffix);
    }
}