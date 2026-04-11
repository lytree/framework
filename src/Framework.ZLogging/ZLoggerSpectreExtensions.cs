using System.Text.Json;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Framework.ZLogging;

public static partial class ZLoggerSpectreExtensions
{
    public static ILoggingBuilder AddZLoggerSpectreConsole(
        this ILoggingBuilder builder,
        Action<ZLoggerSpectreConsoleOptions>? configure = null)
    {
        var options = new ZLoggerSpectreConsoleOptions();
        configure?.Invoke(options);

        builder.AddProvider(new ZLoggerSpectreConsoleLoggerProvider(options));

        return builder;
    }
}

public static partial class ZLoggerSpectreOutputExtensions
{
    public static void WriteMarkup(this ILogger logger, string markup)
    {
        AnsiConsole.Markup(markup);
    }

    public static void WriteMarkupLine(this ILogger logger, string markup)
    {
        AnsiConsole.MarkupLine(markup);
    }

    public static void WriteMarkups(this ILogger logger, params string[] markups)
    {
        foreach (var markup in markups)
        {
            AnsiConsole.MarkupLine(markup);
        }
    }

    public static void WriteTable(this ILogger logger, Table table)
    {
        AnsiConsole.Write(table);
    }

    public static void WriteTree(this ILogger logger, string root, Action<Tree> configure)
    {
        var tree = new Tree(root);
        configure(tree);
        AnsiConsole.Write(tree);
    }

    public static void WritePanel(this ILogger logger, string content)
    {
        var panel = new Panel(content)
            .Border(BoxBorder.None)
            .Expand();

        AnsiConsole.Write(panel);
    }

    public static void WritePanel(this ILogger logger, string content, string title)
    {
        var panel = new Panel(content)
            .Border(BoxBorder.None)
            .Header(title)
            .Expand();

        AnsiConsole.Write(panel);
    }

    public static void WriteStatus(this ILogger logger, string status)
    {
        var panel = new Panel(status)
            .Border(BoxBorder.None)
            .Expand();

        AnsiConsole.Write(panel);
    }

    public static void WriteKeyValueTable(this ILogger logger, IEnumerable<(string Key, string Value)> items)
    {
        var table = new Table()
            .AddColumn("Key")
            .AddColumn("Value")
            .Expand();

        foreach (var (key, value) in items)
        {
            table.AddRow(key, value);
        }

        AnsiConsole.Write(table);
    }

    public static void WriteJson(this ILogger logger, object obj)
    {
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        AnsiConsole.WriteLine(json);
    }

    public static void WriteJsonMarkup(this ILogger logger, object obj)
    {
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        AnsiConsole.MarkupLine($"[cyan]{json}[/]");
    }

    public static void WriteEmptyLine(this ILogger logger)
    {
        AnsiConsole.WriteLine();
    }
}