using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Text.Json;

namespace Framework.ZLogging;

/// <summary>
/// 日志注册入口：把 Spectre.Console 渲染能力接入 <see cref="ILoggingBuilder"/>。
/// </summary>
public static class SpectreLoggingExtensions
{
    /// <summary>
    /// 注册 Spectre.Console 日志提供程序，仅输出到控制台（<see cref="SpectreLogOptions.FilePath"/> 保持 <c>null</c>）。
    /// </summary>
    /// <param name="builder">日志构建器。</param>
    /// <param name="configure">配置委托，可设置时间格式、级别颜色、是否包含作用域等。</param>
    /// <returns>同一个 <paramref name="builder"/>，便于链式调用。</returns>
    public static ILoggingBuilder AddSpectreConsole(this ILoggingBuilder builder,
        Action<SpectreLogOptions>? configure = null)
    {
        var options = new SpectreLogOptions();
        configure?.Invoke(options);
        return builder.AddSpectre(options);
    }

    /// <summary>
    /// 注册 Spectre.Console 日志提供程序，把日志以纯文本写入 <paramref name="filePath"/>。
    ///
    /// 默认 <b>只写文件</b>（<see cref="SpectreLogOptions.ConsoleOutput"/> 预置为 <c>false</c>），
    /// 与 <see cref="AddSpectreConsoleAndFile"/> 区分开；需要同时输出到终端时，
    /// 在 <paramref name="configure"/> 里把它改回 <c>true</c> 即可（委托在预置之后执行）。
    /// <paramref name="filePath"/> 为空时不产生任何输出目标。
    /// </summary>
    /// <param name="builder">日志构建器。</param>
    /// <param name="filePath">日志文件路径；路径中可用 <c>{Date}</c> 占位符按天分文件（跨零点自动切换）。为空时不写文件。</param>
    /// <param name="configure">在默认配置之上做二次覆盖，可设置滚动、编码等。</param>
    /// <returns>同一个 <paramref name="builder"/>，便于链式调用。</returns>
    public static ILoggingBuilder AddSpectreFile(this ILoggingBuilder builder, string? filePath,
        Action<SpectreLogOptions>? configure = null)
    {
        var options = new SpectreLogOptions { FilePath = filePath, ConsoleOutput = false };
        configure?.Invoke(options);
        return builder.AddSpectre(options);
    }

    /// <summary>
    /// 注册控制台 + 文件双输出的日志提供程序（<see cref="SpectreLogOptions.ConsoleOutput"/> 保持默认 <c>true</c>）。
    /// </summary>
    /// <param name="builder">日志构建器。</param>
    /// <param name="filePath">日志文件路径；为 <c>null</c> / 空白时退化为仅控制台。</param>
    /// <param name="configure">在默认配置之上做二次覆盖。</param>
    public static ILoggingBuilder AddSpectreConsoleAndFile(this ILoggingBuilder builder, string? filePath,
        Action<SpectreLogOptions>? configure = null)
    {
        var options = new SpectreLogOptions { FilePath = filePath };
        configure?.Invoke(options);
        return builder.AddSpectre(options);
    }

    /// <summary>
    /// 用已构造好的配置注册提供程序。
    ///
    /// 刻意不提供 <c>AddSpectre(Action&lt;SpectreLogOptions&gt;)</c> 重载：
    /// lambda 会与本方法的 <see cref="SpectreLogOptions"/> 形参产生歧义，
    /// 调用点也无法用命名实参消歧。改用 <see cref="AddSpectreConsole"/> / <see cref="AddSpectreFile"/>。
    ///
    /// 生命周期由 DI 容器托管：容器释放时会调用 <see cref="SpectreConsoleLoggerProvider.DisposeAsync"/>，
    /// 排空队列并关闭日志文件句柄。
    /// </summary>
    public static ILoggingBuilder AddSpectre(this ILoggingBuilder builder, SpectreLogOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        // 用工厂委托注册：实例归容器所有，容器释放时才构造并负责 Dispose。
        // 若传入已构造好的实例（AddSingleton(instance) 或 AddProvider），
        // 容器 / LoggerFactory 都不会释放它，日志文件句柄会一直占用到进程结束。
        builder.Services.AddSingleton<ILoggerProvider>(_ => new SpectreConsoleLoggerProvider(options));
        return builder;
    }
}

/// <summary>
/// 基于 Spectre.Console 的 <see cref="ILogger"/> 扩展方法。
/// 把控制台视为渲染目标，提供写 Markup 文本、表格、面板、JSON、进度条、状态、交互 Prompt 等便捷写法。
/// 这些方法不参与日志级别过滤——可读性优先于日志通道语义。
/// </summary>
public static class SpectreOutputExtensions
{
    static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };

    /// <summary>把 <paramref name="markup"/> 作为 Spectre.Console Markup 写入控制台一行。</summary>
    public static void WriteLine(this ILogger logger, string markup) => AnsiConsole.MarkupLine(markup);

    /// <summary>把多条 Markup 字符串依次写入控制台，每条独立成行。</summary>
    public static void WriteLine(this ILogger logger, params string[] markups)
    {
        foreach (var markup in markups) AnsiConsole.MarkupLine(markup);
    }

    /// <summary>把键值对渲染为两列表格并输出到控制台。</summary>
    public static void WriteLine(this ILogger logger, IEnumerable<(string Key, string Value)> items)
    {
        var table = new Table().AddColumn("Key").AddColumn("Value").Expand();
        foreach (var (key, value) in items) table.AddRow(key, value);
        AnsiConsole.Write(table);
    }

    /// <summary>渲染任意 <see cref="IRenderable"/>。</summary>
    public static void Write(this ILogger logger, IRenderable renderable) => AnsiConsole.Write(renderable);

    /// <summary>渲染 <see cref="Table"/>。</summary>
    public static void Render(this ILogger logger, Table table) => AnsiConsole.Write(table);

    /// <summary>渲染 <see cref="Panel"/>。</summary>
    public static void Write(this ILogger logger, Panel panel) => AnsiConsole.Write(panel);

    /// <summary>把 <paramref name="obj"/> 序列化为缩进 JSON，并以 <see cref="LogLevel.Information"/> 写入日志通道。</summary>
    public static void WriteJson(this ILogger logger, object obj)
        => logger.LogInformation(JsonSerializer.Serialize(obj, IndentedOptions));

    /// <summary>清空控制台。</summary>
    public static void Clear(this ILogger logger) => AnsiConsole.Clear();

    /// <summary>创建一个根节点为 <paramref name="root"/> 的 <see cref="Tree"/>。</summary>
    public static Tree Tree(string root, Action<Tree> configure)
    {
        var tree = new Tree(root);
        configure(tree);
        return tree;
    }

    /// <summary>创建一个无边框、自动展开的 <see cref="Panel"/>。</summary>
    public static Panel Panel(string content, string title = "")
    {
        var panel = new Panel(content).Border(BoxBorder.None).Expand();
        if (!string.IsNullOrEmpty(title)) panel.Header(title);
        return panel;
    }

    /// <summary>启动实时显示，用于持续刷新渲染内容。</summary>
    public static LiveDisplay Live(this ILogger logger, IRenderable target) => AnsiConsole.Live(target);

    /// <summary>启动一个自动隐藏已完成任务的进度条。</summary>
    public static void StartProgress(this ILogger logger, Action<ProgressContext> action)
        => new Progress(AnsiConsole.Console).HideCompleted(true).Start(action);

    /// <summary>启动状态栏并返回上下文以便持续更新。</summary>
    public static StatusContext StartStatus(this ILogger logger, string status, Action<Status>? configure = null)
    {
        var s = new Status(AnsiConsole.Console);
        configure?.Invoke(s);
        StatusContext? ctx = null;
        s.Start(status, c => ctx = c);
        return ctx!;
    }

    /// <summary>询问用户并返回转换后的回答。</summary>
    public static T Ask<T>(this ILogger logger, string prompt, T defaultValue = default)
        => AnsiConsole.Ask(prompt, defaultValue);

    /// <summary>创建一个可继续配置的文本输入提示。</summary>
    public static TextPrompt<T> Prompt<T>(this ILogger logger, string prompt) => new(prompt);

    /// <summary>渲染水平分隔线。</summary>
    public static void Write(this ILogger logger, string title, Action<Rule> configure)
    {
        var rule = new Rule(title);
        configure(rule);
        AnsiConsole.Write(rule);
    }

    /// <summary>获取 Spectre.Console 的 <see cref="Profile"/>，用于渲染带颜色的文本。</summary>
    public static Profile Profile(this ILogger logger) => AnsiConsole.Profile;

    /// <summary>获取控制台光标，用于移动光标位置。</summary>
    public static IAnsiConsoleCursor Cursor(this ILogger logger) => AnsiConsole.Cursor;
}
