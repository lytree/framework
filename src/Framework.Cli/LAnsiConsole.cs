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

    public static void WriteException(Exception exception, Spectre.Console.ExceptionFormats format = Spectre.Console.ExceptionFormats.Default)
    {
        Spectre.Console.AnsiConsole.WriteException(exception, format);
    }

    public static void WriteException(Exception exception, Spectre.Console.ExceptionSettings settings)
    {
        Spectre.Console.AnsiConsole.WriteException(exception, settings);
    }

    public static Progress Progress()
    {
        return Spectre.Console.AnsiConsole.Progress();
    }

    public static Status Status()
    {
        return Spectre.Console.AnsiConsole.Status();
    }

    public static LiveDisplay Live(IRenderable target)
    {
        return Spectre.Console.AnsiConsole.Live(target);
    }

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

    public static string ExportCustom(IAnsiConsoleEncoder encoder)
    {
        return Spectre.Console.AnsiConsole.ExportCustom(encoder);
    }

    public static T Prompt<T>(IPrompt<T> prompt)
    {
        return Spectre.Console.AnsiConsole.Prompt(prompt);
    }

    public static T Ask<T>(string prompt)
    {
        return Spectre.Console.AnsiConsole.Ask<T>(prompt);
    }

    public static T Ask<T>(string prompt, T defaultValue)
    {
        return Spectre.Console.AnsiConsole.Ask(prompt, defaultValue);
    }

    public static bool Confirm(string prompt, bool defaultValue = true)
    {
        return Spectre.Console.AnsiConsole.Confirm(prompt, defaultValue);
    }
}