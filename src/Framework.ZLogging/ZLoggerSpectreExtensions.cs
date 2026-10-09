using System.Text.Json;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;
using ZLogger;
using ZLogger.Formatters;

namespace Framework.ZLogging;

/// <summary>
/// 把 Spectre.Console 的丰富渲染能力接入 <see cref="ILoggingBuilder"/> 与 <see cref="ILogger"/>：
/// <see cref="ZLoggerSpectreExtensions"/> 负责注册 Provider，
/// <see cref="ZLoggerSpectreOutputExtensions"/> 提供基于 <see cref="AnsiConsole"/> 的写行、表格、面板、JSON、交互控件等便捷方法。
/// </summary>
public static class ZLoggerSpectreExtensions
{
    /// <summary>
    /// 注册 ZLogger 的 Spectre.Console Provider，同时把日志按纯文本格式追加写入 <paramref name="filePath"/>。
    /// 控制台输出由 <see cref="AddZLoggerSpectreConsole(ILoggingBuilder, Action{ZLoggerSpectreConsoleOptions})"/> 完成，
    /// 文件输出则通过 <see cref="ZLoggerSpectreConsoleOptions.FilePath"/> 配置。
    /// </summary>
    /// <param name="builder">日志构建器。</param>
    /// <param name="filePath">日志文件路径；为 <c>null</c> 时表示不写文件。</param>
    /// <returns>同一个 <paramref name="builder"/>，便于链式调用。</returns>
    public static ILoggingBuilder AddZLoggerSpectreConsoleAndFile(this ILoggingBuilder builder, string? filePath)
    {
        return AddZLoggerSpectreConsoleAndFile(builder, filePath, _ => { });
    }

    /// <summary>
    /// 注册 ZLogger 的 Spectre.Console Provider，同时按纯文本格式追加写入 <paramref name="filePath"/>，并允许进一步自定义 <see cref="ZLoggerSpectreConsoleOptions"/>。
    /// </summary>
    /// <param name="builder">日志构建器。</param>
    /// <param name="filePath">日志文件路径；为 <c>null</c> 时表示不写文件。</param>
    /// <param name="configure">在默认配置完成后再执行的二次配置委托，可用于覆盖颜色、前缀、文件路径等。</param>
    /// <returns>同一个 <paramref name="builder"/>，便于链式调用。</returns>
    public static ILoggingBuilder AddZLoggerSpectreConsoleAndFile(this ILoggingBuilder builder, string? filePath, Action<ZLoggerSpectreConsoleOptions> configure)
    {
        builder.AddZLoggerSpectreConsole(options =>
        {
            options.FilePath = filePath;
            options.UsePlainTextFormatter(formatter =>
            {
                formatter.SetPrefixFormatter($"{0:local-longdate}|{1}{2:short}{3}|{4}|",
                    (in MessageTemplate template, in LogInfo i) =>
                    {
                        template.Format(
                            i.Timestamp,
                            $"[{options.LogLevelColors.GetValueOrDefault(i.LogLevel, "white")}]", i.LogLevel, "[/]",
                            i.Category);
                    });
            });
            configure(options);
        });
        return builder;
    }

    /// <summary>
    /// 仅注册 ZLogger 的 Spectre.Console Provider，通过 <paramref name="configure"/> 配置 <see cref="ZLoggerSpectreConsoleOptions"/>（颜色、前缀、文件路径等）。
    /// </summary>
    /// <param name="builder">日志构建器。</param>
    /// <param name="configure">用于在新建 <see cref="ZLoggerSpectreConsoleOptions"/> 上进行配置的委托，必须非 <c>null</c>。</param>
    /// <returns>同一个 <paramref name="builder"/>，便于链式调用。</returns>
    public static ILoggingBuilder AddZLoggerSpectreConsole(this ILoggingBuilder builder, Action<ZLoggerSpectreConsoleOptions> configure)
    {
        var options = new ZLoggerSpectreConsoleOptions();
        configure(options);

        builder.AddProvider(new ZLoggerSpectreConsoleLoggerProvider(options));
        return builder;
    }
}

/// <summary>
/// 基于 Spectre.Console 的 <see cref="ILogger"/> 扩展方法：
/// 把控制台视为渲染目标，提供写 Markup 文本、表格、面板、JSON、进度条、状态、交互 Prompt、规则、Live、Profile 等便捷写法。
/// 这些方法不直接参与日志级别过滤——可读性优先于日志通道语义。
/// </summary>
public static partial class ZLoggerSpectreOutputExtensions
{
    // 缩进选项：让 WriteJson 输出的 JSON 便于在控制台中阅读
    private static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };

    /// <summary>
    /// 把 <paramref name="markup"/> 作为 Spectre.Console Markup 写入控制台一行。
    /// </summary>
    /// <param name="logger">本方法的 <see cref="ILogger"/> 接收者，仅用于链式调用形式，不会被实际使用。</param>
    /// <param name="markup">要写入的 Spectre.Console Markup 字符串（支持 <c>[red]...[/]</c> 等标签）。</param>
    public static void WriteLine(this ILogger logger, string markup)
    {
        AnsiConsole.MarkupLine(markup);
    }

    /// <summary>
    /// 把多条 Markup 字符串依次写入控制台，每条独立成行。
    /// </summary>
    /// <param name="logger">本方法的 <see cref="ILogger"/> 接收者，仅用于链式调用形式。</param>
    /// <param name="markups">要写入的若干条 Markup 文本。</param>
    public static void WriteLine(this ILogger logger, params string[] markups)
    {
        foreach (var markup in markups)
        {
            AnsiConsole.MarkupLine(markup);
        }
    }

    /// <summary>
    /// 把 <paramref name="table"/> 直接渲染到控制台。
    /// </summary>
    /// <param name="logger">本方法的 <see cref="ILogger"/> 接收者，仅用于链式调用形式。</param>
    /// <param name="table">要渲染的 Spectre.Console <see cref="Table"/>。</param>
    public static void Render(this ILogger logger, Table table)
    {
        AnsiConsole.Write(table);
    }

    /// <summary>
    /// 创建一个根节点为 <paramref name="root"/> 的 <see cref="Tree"/>，并通过 <paramref name="configure"/> 让调用方添加子节点。
    /// </summary>
    /// <param name="root">根节点显示文本。</param>
    /// <param name="configure">用于在新建的 <see cref="Tree"/> 上添加子节点的配置委托。</param>
    /// <returns>配置完成的 <see cref="Tree"/>。</returns>
    public static Tree Tree(string root, Action<Tree> configure)
    {
        var tree = new Tree(root);
        configure(tree);
        return tree;
    }

    /// <summary>
    /// 把 <paramref name="panel"/> 渲染到控制台。
    /// </summary>
    /// <param name="logger">本方法的 <see cref="ILogger"/> 接收者，仅用于链式调用形式。</param>
    /// <param name="panel">要渲染的 Spectre.Console <see cref="Panel"/>。</param>
    public static void Write(this ILogger logger, Panel panel)
    {
        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// 创建一个无边框、自动展开的 <see cref="Panel"/>，可选地为它设置标题。
    /// </summary>
    /// <param name="content">面板正文 Markup 字符串。</param>
    /// <param name="title">可选的标题；为空字符串或 <c>null</c> 时不设置标题。</param>
    /// <returns>已构造好的 <see cref="Panel"/>。</returns>
    public static Panel Panel(string content, string title = "")
    {
        var panel = new Panel(content)
            .Border(BoxBorder.None)
            .Expand();

        if (!string.IsNullOrEmpty(title))
            panel.Header(title);

        return panel;
    }

    /// <summary>
    /// 把 <paramref name="items"/> 渲染为一个 Key/Value 两列的表格并输出到控制台，每行一项。
    /// </summary>
    /// <param name="logger">本方法的 <see cref="ILogger"/> 接收者，仅用于链式调用形式。</param>
    /// <param name="items">要展示的键值对集合。</param>
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

    /// <summary>
    /// 把 <paramref name="obj"/> 序列化为缩进 JSON，并以 <see cref="LogLevel.Information"/> 级别写入日志通道。
    /// </summary>
    /// <param name="logger">用于实际输出 JSON 文本的 <see cref="ILogger"/>。</param>
    /// <param name="obj">要序列化的对象。</param>
    public static void WriteJson(this ILogger logger, object obj)
    {
        var json = JsonSerializer.Serialize(obj, IndentedOptions);
        logger.LogInformation(json);
    }

    /// <summary>
    /// 把任意 <see cref="IRenderable"/> 写入控制台。
    /// </summary>
    /// <param name="logger">本方法的 <see cref="ILogger"/> 接收者，仅用于链式调用形式。</param>
    /// <param name="renderable">要渲染的对象。</param>
    public static void Write(this ILogger logger, IRenderable renderable)
    {
        AnsiConsole.Write(renderable);
    }

    /// <summary>
    /// 清空控制台。
    /// </summary>
    /// <param name="logger">本方法的 <see cref="ILogger"/> 接收者，仅用于链式调用形式。</param>
    public static void Clear(this ILogger logger)
    {
        AnsiConsole.Clear();
    }

    /// <summary>
    /// 在 <paramref name="target"/> 上启动一个 <see cref="LiveDisplay"/>，用于实时更新渲染内容。
    /// </summary>
    /// <param name="logger">本方法的 <see cref="ILogger"/> 接收者，仅用于链式调用形式。</param>
    /// <param name="target">实时显示的目标渲染对象。</param>
    /// <returns>Spectre.Console 的 <see cref="LiveDisplay"/> 实例。</returns>
    public static LiveDisplay Live(this ILogger logger, IRenderable target) => AnsiConsole.Live(target);

    /// <summary>
    /// 启动一个自动隐藏已完成任务的进度条，并在 <paramref name="action"/> 中执行业务逻辑（典型用法：循环调用 <c>ctx.AddTask(...)</c>）。
    /// </summary>
    /// <param name="logger">本方法的 <see cref="ILogger"/> 接收者，仅用于链式调用形式。</param>
    /// <param name="action">由 Spectre.Console 调用的 <see cref="ProgressContext"/> 回调。</param>
    public static void StartProgress(this ILogger logger, Action<ProgressContext> action)
    {
        new Progress(AnsiConsole.Console).HideCompleted(true).Start(action);
    }

    /// <summary>
    /// 启动一个 <see cref="Status"/>，以 <paramref name="status"/> 为初始文本开始刷新，并返回 <see cref="StatusContext"/> 以便调用方持续更新。
    /// </summary>
    /// <param name="logger">本方法的 <see cref="ILogger"/> 接收者，仅用于链式调用形式。</param>
    /// <param name="status">状态栏初始文本。</param>
    /// <param name="configure">可选的二次配置委托，可用于调整 <see cref="Status"/> 的颜色、Spinner 等。</param>
    /// <returns>Spectre.Console 的 <see cref="StatusContext"/>，用于后续更新状态文本。</returns>
    public static StatusContext StartStatus(this ILogger logger, string status, Action<Status>? configure = null)
    {
        var s = new Status(AnsiConsole.Console);
        configure?.Invoke(s);
        StatusContext? ctx = null;
        s.Start(status, c => ctx = c);
        return ctx!;
    }

    /// <summary>
    /// 通过 Spectre.Console 的 <see cref="AnsiConsole.Ask{T}(string, T)"/> 询问用户并把回答作为返回值。
    /// </summary>
    /// <typeparam name="T">期望的回答类型。</typeparam>
    /// <param name="logger">本方法的 <see cref="ILogger"/> 接收者，仅用于链式调用形式。</param>
    /// <param name="question">询问提示文本。</param>
    /// <param name="defaultValue">用户直接回车时采用的默认值。</param>
    /// <returns>用户输入（经过 Spectre.Console 类型转换的结果）。</returns>
    public static T Ask<T>(this ILogger logger, string prompt, T defaultValue = default)
    {
        return AnsiConsole.Ask(prompt, defaultValue);
    }

    /// <summary>
    /// 创建一个可由调用方继续配置的 <see cref="TextPrompt{T}"/>，用于在交互式 CLI 中收集输入。
    /// </summary>
    /// <typeparam name="T">期望的回答类型。</typeparam>
    /// <param name="logger">本方法的 <see cref="ILogger"/> 接收者，仅用于链式调用形式。</param>
    /// <param name="prompt">提示文本。</param>
    /// <returns>新建的 <see cref="TextPrompt{T}"/> 实例。</returns>
    public static TextPrompt<T> Prompt<T>(this ILogger logger, string prompt) => new(prompt);

    /// <summary>
    /// 渲染一个 Spectre.Console <see cref="Rule"/>（水平分隔线/带标题），由 <paramref name="configure"/> 配置样式。
    /// </summary>
    /// <param name="logger">本方法的 <see cref="ILogger"/> 接收者，仅用于链式调用形式。</param>
    /// <param name="title">规则标题。</param>
    /// <param name="configure">用于在新建的 <see cref="Rule"/> 上配置样式的委托。</param>
    public static void Write(this ILogger logger, string title, Action<Rule> configure)
    {
        var rule = new Rule(title);
        configure(rule);
        AnsiConsole.Write(rule);
    }

    /// <summary>
    /// 获取 Spectre.Console 的 <see cref="Profile"/>，用于渲染带颜色的 Profile 文本。
    /// </summary>
    /// <param name="logger">本方法的 <see cref="ILogger"/> 接收者，仅用于链式调用形式。</param>
    /// <returns>Spectre.Console 的 <see cref="Profile"/>。</returns>
    public static Profile Profile(this ILogger logger) => AnsiConsole.Profile;

    /// <summary>
    /// 获取 Spectre.Console 的 <see cref="IAnsiConsoleCursor"/>，用于在控制台移动光标。
    /// </summary>
    /// <param name="logger">本方法的 <see cref="ILogger"/> 接收者，仅用于链式调用形式。</param>
    /// <returns>Spectre.Console 的光标对象。</returns>
    public static IAnsiConsoleCursor Cursor(this ILogger logger) => AnsiConsole.Cursor;
}