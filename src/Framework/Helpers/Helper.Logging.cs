using System;
using Spectre.Console;

namespace Framework;
public static partial class Helper
{
    public static string RemoveMarkup(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        return Markup.Remove(value);
    }

    public static string GetLogLevel(string message)
    {
        if (message.Contains("Trace")) return "TRACE";
        if (message.Contains("Debug")) return "DEBUG";
        if (message.Contains("Information") || message.Contains("Info")) return "INFO";
        if (message.Contains("Warning") || message.Contains("Warn")) return "WARN";
        if (message.Contains("Error") || message.Contains("Fail")) return "ERROR";
        if (message.Contains("Critical") || message.Contains("Fatal")) return "CRITICAL";
        return "INFO";
    }

    public static string GetLogCategory(string message)
    {
        var start = message.IndexOf('[');
        var end = message.IndexOf(']');
        if (start >= 0 && end > start)
        {
            return message.Substring(start + 1, end - start - 1);
        }
        return "";
    }
}