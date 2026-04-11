using Spectre.Console;
using Spectre.Console.Rendering;

namespace Framework.Cli;

public static partial class LAnsiConsole
{
    public static void Write(string message)
    {
        Console.Write(message);
    }

    public static void WriteLine(string message = "")
    {
        Console.WriteLine(message);
    }

    public static void Write(IRenderable renderable)
    {
        Spectre.Console.AnsiConsole.Write(renderable);
    }

    public static void WriteLine(IRenderable renderable)
    {
        Spectre.Console.AnsiConsole.Write(renderable);
        Spectre.Console.AnsiConsole.WriteLine();
    }

    public static void LWrite(string message, string filePath, bool writeToFile = false, string? prefix = null, string? suffix = null)
    {
        LAnsiConsoleWriter.LWrite(message, filePath, writeToFile, prefix, suffix);
    }

    public static void LWriteLine(string message = "", string filePath = "", bool writeToFile = false, string? prefix = null, string? suffix = null)
    {
        LAnsiConsoleWriter.LWriteLine(message, filePath, writeToFile, prefix, suffix);
    }

    public static void LWrite(string message, bool writeToFile = false)
    {
        if (Console is LAnsiConsoleWriter writer)
        {
            writer.LWrite(message, writeToFile);
        }
    }

    public static void LWriteLine(string message = "", bool writeToFile = false)
    {
        if (Console is LAnsiConsoleWriter writer)
        {
            writer.LWriteLine(message, writeToFile);
        }
    }
}