using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Text;
using System.Threading.Channels;

namespace Framework.ZLogging;

/// <summary>
/// <see cref="SpectreLogProcessor"/> 的全部配置项。
/// 控制台与文件两条输出链路共享同一份配置，保证同一日志在两端的语义一致。
/// </summary>
public sealed class SpectreLogOptions
{
    /// <summary>时间戳格式，默认 <c>yyyy-MM-dd HH:mm:ss</c>。</summary>
    public string TimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

    /// <summary>各 <see cref="LogLevel"/> 对应的 Spectre.Console 颜色名。未命中的级别回退为 <c>white</c>。</summary>
    public FrozenDictionary<LogLevel, string> LogLevelColors { get; set; } = CreateDefaultLogLevelColors();

    /// <summary>是否把 <see cref="ILogger.BeginScope{TState}"/> 的作用域内容写入日志。</summary>
    public bool IncludeScopes { get; set; }

    /// <summary>时间源；为 <c>null</c> 时使用 <see cref="TimeProvider.System"/>。</summary>
    public TimeProvider? TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>后台写入循环内部发生异常时的回调；生产环境建议接入自己的告警通道。</summary>
    public Action<Exception>? InternalErrorLogger { get; set; }

    /// <summary>自定义异常渲染；为 <c>null</c> 时使用内置的类型名 / 消息 / 内部异常链 / 堆栈四段式输出。</summary>
    public Action<IAnsiConsole, Exception>? ExceptionFormatter
    {
        get => _exceptionFormatter;
        set => _exceptionFormatter = value;
    }
    Action<IAnsiConsole, Exception>? _exceptionFormatter;

    #region 队列

    int _queueCapacity = 12000;

    /// <summary>
    /// 有界队列容量。默认 12000，来自对高吞吐场景的实践取值。
    /// </summary>
    public int QueueCapacity
    {
        get => _queueCapacity;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _queueCapacity = value;
        }
    }

    BoundedChannelFullMode _queueFullMode = BoundedChannelFullMode.DropWrite;

    /// <summary>
    /// 队列满时的处置策略。默认 <see cref="BoundedChannelFullMode.DropWrite"/>：
    /// 丢弃新消息并立即返回，绝不阻塞业务线程。若日志不允许丢失，可改为
    /// <see cref="BoundedChannelFullMode.Wait"/> 让生产者等待。
    /// </summary>
    public BoundedChannelFullMode QueueFullMode
    {
        get => _queueFullMode;
        set
        {
            if (value == BoundedChannelFullMode.DropWrite)
                ThrowInvalidFullMode();
            _queueFullMode = value;
        }
    }

    /// <summary>后台消费时单批最多读取的消息条数，用于摊薄每次写盘的系统调用次数。</summary>
    public int BatchSize { get; set; } = 100;

    #endregion

    #region 控制台输出

    /// <summary>是否输出到控制台。<see cref="FilePath"/> 非空且此项为 <c>false</c> 时即为纯文件日志。</summary>
    public bool ConsoleOutput { get; set; } = true;

    /// <summary>
    /// 控制台渲染宽度。<c>0</c> 表示跟随终端实际宽度；正数则强制该宽度，
    /// 便于表格等宽输出在重定向或快照测试中保持稳定。
    /// </summary>
    public int ConsoleRenderWidth { get; set; }

    #endregion

    #region 文件输出

    /// <summary>
    /// 日志文件路径。为 <c>null</c> / 空白时关闭文件输出。
    /// 路径中可包含 <c>{Date}</c> 占位符，会替换为 <see cref="DateTime.Now"/> 的 <c>yyyy-MM-dd</c>。
    /// </summary>
    public string? FilePath { get; set; }

    bool _fileAppend = true;

    /// <summary>为 <c>true</c> 追加写入已有文件；为 <c>false</c> 覆盖。</summary>
    public bool FileAppend
    {
        get => _fileAppend;
        set => _fileAppend = value;
    }

    /// <summary>文件编码。默认不带 BOM 的 UTF-8，避免外部编辑器显示乱码。</summary>
    public Encoding FileEncoding { get; set; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    long _fileSizeLimitBytes;

    /// <summary>
    /// 单文件体积上限（字节）。超过后滚动为 <c>name_1.log</c>、<c>name_2.log</c>……
    /// <c>0</c> 表示不按大小滚动。
    /// </summary>
    public long FileSizeLimitBytes
    {
        get => _fileSizeLimitBytes;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _fileSizeLimitBytes = value;
        }
    }

    int _maxRollingFiles;

    /// <summary>
    /// 滚动保留的最大文件数，超出后按最后写入时间淘汰最旧的文件。
    /// <c>0</c> 表示不限制。<see cref="FileSizeLimitBytes"/> 为 <c>0</c> 时本项无效。
    /// </summary>
    public int MaxRollingFiles
    {
        get => _maxRollingFiles;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _maxRollingFiles = value;
        }
    }

    /// <summary>周期性刷盘的间隔。默认 1 秒，兼顾实时性与吞吐。</summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>关闭时等待后台刷盘的时长，超时则放弃等待以免卡住进程退出。</summary>
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 写入文件时用于渲染表格 / 面板的宽度；<c>0</c> 表示不折行（推荐）。
    /// </summary>
    public int FileRenderWidth { get; set; }

    /// <summary>文件写入失败（打开、滚动、删除旧文件）时的回调，可在其中切换降级路径。</summary>
    public Action<FileWriteError>? FileWriteErrorHandler { get; set; }

    #endregion

    #region 专用格式（自定义指令）

    /// <summary>对象渲染器：决定 <c>{Value:obj}</c> 这类结构化参数如何可视化。</summary>
    public SpectreObjectPrinter ObjectPrinter { get; } = new();

    readonly ConcurrentDictionary<string, Func<object?, SpectreObjectPrinter, IRenderable>> _formats = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 注册消息模板中的专用格式指令，例如 <c>{Data:panel}</c>。
    /// 同名格式后注册者覆盖先注册者。格式名仅支持字母、数字、下划线与连字符。
    /// </summary>
    public SpectreLogOptions RegisterFormat(string format, Func<object?, SpectreObjectPrinter, IRenderable> render)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        ArgumentNullException.ThrowIfNull(render);
        if (format.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('_' or '-')))
            throw new ArgumentException("格式名称仅支持字母、数字、下划线和连字符。", nameof(format));

        _formats[format] = render;
        return this;
    }

    /// <summary>注册一个按指定宽度渲染的格式；写文件时用它避免表格被折断。</summary>
    internal SpectreLogOptions RegisterFormat(string format, Func<object?, int, IRenderable> render, int width)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        ArgumentNullException.ThrowIfNull(render);

        _formats[format] = (value, _) => render(value, width);
        return this;
    }

    /// <summary>内置专用格式；构造时一次性注册，避免每个 Provider 重复注册。</summary>
    internal void RegisterDefaultFormats() => SpectreLogFormats.RegisterDefaults(this);

    internal bool TryGetFormat(string format, out Func<object?, SpectreObjectPrinter, IRenderable>? render)
        => _formats.TryGetValue(format, out render);

    #endregion

    /// <summary>
    /// 解析 <c>{Date}</c> 占位符、展开环境变量，并把相对路径转成基于应用目录的绝对路径。
    /// 相对路径基于 <see cref="AppContext.BaseDirectory"/> 而非当前工作目录，
    /// 避免进程工作目录漂移后日志写到别处。
    /// </summary>
    public string ResolveFilePath(DateTime now)
    {
        var path = Environment.ExpandEnvironmentVariables(FilePath!);
        if (path.Contains("{Date}", StringComparison.OrdinalIgnoreCase))
        {
            path = path.Replace("{Date}", now.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase);
        }
        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, path);
        }
        return path;
    }

    internal static FrozenDictionary<LogLevel, string> CreateDefaultLogLevelColors()
        => new Dictionary<LogLevel, string>
        {
            { LogLevel.Trace, "grey" },
            { LogLevel.Debug, "grey" },
            { LogLevel.Information, "green" },
            { LogLevel.Warning, "yellow" },
            { LogLevel.Error, "red" },
            { LogLevel.Critical, "red bold" }
        }.ToFrozenDictionary();

    static void ThrowInvalidFullMode()
        => throw new ArgumentException(
            "QueueFullMode 只支持 DropOldest / DropWrite / Wait；DropWrite 已是默认值，如需丢弃最新消息请直接保留该设置。",
            nameof(QueueFullMode));

    /// <summary>最终生效的时间源，避免每条日志重复判空。</summary>
    internal TimeProvider Clock => TimeProvider ?? TimeProvider.System;
}

/// <summary>文件写入失败时抛出的诊断信息，供 <see cref="SpectreLogOptions.FileWriteErrorHandler"/> 决策。</summary>
/// <param name="FileName">失败时正在写入的物理文件路径。</param>
/// <param name="Exception">底层异常。</param>
public readonly record struct FileWriteError(string FileName, Exception Exception);
