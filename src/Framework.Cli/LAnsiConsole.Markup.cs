using Spectre.Console;

namespace Framework.Cli;

public static partial class LAnsiConsole
{
    public static void Markup(string value, bool writeToFile = false)
    {
        _writer.Markup(value,writeToFile);
    }

    public static void Markup(string format, bool writeToFile = false, params object[] args)
    {
        _writer.Markup(format,writeToFile, args);
    }

    public static void MarkupLine(string value, bool writeToFile = false)
    {
        _writer.MarkupLine(value,writeToFile);
    }

    public static void MarkupLine(string format, bool writeToFile = false, params object[] args)
    {
        _writer.MarkupLine(format,writeToFile, args);
    }

    public static void MarkupInterpolated(FormattableString value, bool writeToFile = false)
    {
        _writer.MarkupInterpolated(value,writeToFile);
    }

    public static void MarkupLineInterpolated(FormattableString value, bool writeToFile = false)
    {
        _writer.MarkupLineInterpolated(value,writeToFile);
    }
}