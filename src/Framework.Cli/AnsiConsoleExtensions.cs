using Spectre.Console;

namespace Framework.Cli;

public static partial class AnsiConsoleExtensions
{
    private static string? _filePath;
    private static string? _prefix;
    private static string? _suffix;
    private static readonly object _lock = new();

    public static void ConfigureFileOutput(string? filePath, string? prefix = null, string? suffix = null)
    {
        _filePath = filePath;
        _prefix = prefix ?? string.Empty;
        _suffix = suffix ?? string.Empty;
    }

    private static void WriteToFile(string message)
    {
        if (string.IsNullOrEmpty(_filePath))
            return;

        lock (_lock)
        {
            using var stream = new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream);
            writer.Write(_prefix);
            writer.Write(message);
            writer.WriteLine(_suffix);
            writer.Flush();
        }
    }

    public static void Write(string message, bool writeToFile = false)
    {
        AnsiConsole.Write(message);
        if (writeToFile)
        {
            WriteToFile(message);
        }
    }

    public static void WriteLine(string message = "", bool writeToFile = false)
    {
        AnsiConsole.WriteLine(message);
        if (writeToFile)
        {
            WriteToFile(message);
        }
    }

    public static void Markup(string markup, bool writeToFile = false)
    {
        AnsiConsole.Markup(markup);
        if (writeToFile)
        {
            WriteToFile(markup);
        }
    }

    public static void MarkupLine(string markup, bool writeToFile = false)
    {
        AnsiConsole.MarkupLine(markup);
        if (writeToFile)
        {
            WriteToFile(markup);
        }
    }

    public static void Markup(string format, params object[] args)
    {
        AnsiConsole.Markup(format, args);
    }

    public static void MarkupLine(string format, params object[] args)
    {
        AnsiConsole.MarkupLine(format, args);
    }

    public static void WriteException(Exception exception, ExceptionFormats format = ExceptionFormats.Default, bool writeToFile = false)
    {
        AnsiConsole.WriteException(exception, format);
        if (writeToFile)
        {
            WriteToFile(exception.ToString() ?? string.Empty);
        }
    }

    public static void WriteException(Exception exception, ExceptionSettings settings, bool writeToFile = false)
    {
        AnsiConsole.WriteException(exception, settings);
        if (writeToFile)
        {
            WriteToFile(exception.ToString() ?? string.Empty);
        }
    }

    public static T Prompt<T>(IPrompt<T> prompt, bool writeToFile = false)
    {
        var result = AnsiConsole.Prompt(prompt);
        if (writeToFile)
        {
            WriteToFile(result?.ToString() ?? string.Empty);
        }
        return result;
    }

    public static T Ask<T>(string prompt, bool writeToFile = false)
    {
        var result = AnsiConsole.Ask<T>(prompt);
        if (writeToFile)
        {
            WriteToFile(prompt);
        }
        return result;
    }

    public static T Ask<T>(string prompt, T defaultValue, bool writeToFile = false)
    {
        var result = AnsiConsole.Ask(prompt, defaultValue);
        if (writeToFile)
        {
            WriteToFile(prompt);
        }
        return result;
    }

    public static bool Confirm(string prompt, bool defaultValue = true, bool writeToFile = false)
    {
        var result = AnsiConsole.Confirm(prompt, defaultValue);
        if (writeToFile)
        {
            WriteToFile(prompt);
        }
        return result;
    }

    public static T Choose<T>(string prompt, IEnumerable<T> items, bool writeToFile = false)
    {
        var selection = new SelectionPrompt<T>().Title(prompt).AddChoices(items);
        var result = AnsiConsole.Prompt(selection);
        if (writeToFile)
        {
            WriteToFile(prompt);
        }
        return result;
    }
}