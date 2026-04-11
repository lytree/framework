using Spectre.Console;
using Spectre.Console.Rendering;

namespace Framework.Cli;

public static partial class LAnsiConsole
{
    public static void Write(string message)
    {
        _writer.Write(message);
    }

    public static void WriteLine(string message = "")
    {
        _writer.WriteLine(message);
    }

    public static void Write(IRenderable renderable)
    {
        _writer.Write(renderable);
    }

    public static void WriteLine(IRenderable renderable)
    {
        _writer.Write(renderable);
        _writer.WriteLine();
    }


    public static void Write(string message, bool writeToFile = false)
    {
        if (Console is LAnsiConsoleWriter writer)
        {
            writer.Write(message, writeToFile);
        }
    }

    public static void WriteLine(string message, bool writeToFile = false)
    {
        if (Console is LAnsiConsoleWriter writer)
        {
            writer.WriteLine(message, writeToFile);
        }
    }
}