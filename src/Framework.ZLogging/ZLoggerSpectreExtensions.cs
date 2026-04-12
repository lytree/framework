using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Rendering;
using ZLogger;
using ZLogger.Formatters;

namespace Framework.ZLogging;

public static class ZLoggerSpectreExtensions
{
    public static ILoggingBuilder AddZLoggerSpectreConsole(this ILoggingBuilder builder)
    {
        return builder.AddZLoggerSpectreConsole(options =>
        {
            options.UsePlainTextFormatter(formatter =>
            {
                DataTimeUtcLogLevelCategoryFormater(options, formatter);
            });

        });
        void DataTimeUtcLogLevelCategoryFormater(ZLoggerSpectreConsoleOptions options, PlainTextZLoggerFormatter formatter)
        {
            formatter.SetPrefixFormatter($"{0:utc-datetime}|{1}{2:short}{3}|{4}|",
                (in MessageTemplate template, in LogInfo i) =>
                {
                    template.Format(
                                i.Timestamp,
                                $"[{options.LogLevelColors.GetValueOrDefault(i.LogLevel, "white")}]", i.LogLevel, "[/]",
                                i.Category);
                });
        }

    }
    public static ILoggingBuilder AddZLoggerSpectreConsole(this ILoggingBuilder builder, Action<ZLoggerSpectreConsoleOptions> configure)
    {
        var options = new ZLoggerSpectreConsoleOptions();
        configure(options);

        builder.AddProvider(new ZLoggerSpectreConsoleLoggerProvider(options));

        return builder;
    }
}

public static partial class ZLoggerSpectreOutputExtensions
{
    public static void WriteLine(this ILogger logger, string markup)
    {
        AnsiConsole.MarkupLine(markup);
    }

    public static void WriteLine(this ILogger logger, params string[] markups)
    {
        foreach (var markup in markups)
        {
            AnsiConsole.MarkupLine(markup);
        }
    }

    public static void Render(this ILogger logger, Table table)
    {
        AnsiConsole.Write(table);
    }

    public static Tree Tree(string root, Action<Tree> configure)
    {
        var tree = new Tree(root);
        configure(tree);
        return tree;
    }

    public static void Write(this ILogger logger, Panel panel)
    {
        AnsiConsole.Write(panel);
    }

    public static Panel Panel(string content, string title = "")
    {
        var panel = new Panel(content)
            .Border(BoxBorder.None)
            .Expand();

        if (!string.IsNullOrEmpty(title))
            panel.Header(title);

        return panel;
    }

    public static void WriteLine(this ILogger logger, IEnumerable<(string Key, string Value)> items)
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
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        logger.LogInformation(json);
    }

    public static void Write(this ILogger logger, IRenderable renderable)
    {
        AnsiConsole.Write(renderable);
    }

    public static void Clear(this ILogger logger)
    {
        AnsiConsole.Clear();
    }

    public static LiveDisplay Live(this ILogger logger, IRenderable target) => AnsiConsole.Live(target);

    public static void StartProgress(this ILogger logger, Action<ProgressContext> action)
    {
        new Progress(AnsiConsole.Console).HideCompleted(true).Start(action);
    }

    public static StatusContext StartStatus(this ILogger logger, string status, Action<Status> configure = null)
    {
        var s = new Status(AnsiConsole.Console);
        configure?.Invoke(s);
        StatusContext ctx = null;
        s.Start(status, c => ctx = c);
        return ctx;
    }

    public static T Ask<T>(this ILogger logger, string prompt, T defaultValue = default)
    {
        return AnsiConsole.Ask(prompt, defaultValue);
    }

    public static TextPrompt<T> Prompt<T>(this ILogger logger, string prompt) => new(prompt);

    public static void Write(this ILogger logger, string title, Action<Rule> configure)
    {
        var rule = new Rule(title);
        configure(rule);
        AnsiConsole.Write(rule);
    }

    public static Profile Profile(this ILogger logger) => AnsiConsole.Profile;

    public static IAnsiConsoleCursor Cursor(this ILogger logger) => AnsiConsole.Cursor;
}