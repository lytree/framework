using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Collections.Frozen;
using System.Text;
using System.Threading.Channels;

namespace Framework.ZLogging;

/// <summary>
/// 日志处理器：调用线程只做「快照 + 入队」，控制台渲染与文件写盘都在后台单消费者完成。
///
/// 消费者固定为 1，因此控制台输出天然串行；
/// 这点很重要——<see cref="AnsiConsole"/> 本身并非线程安全，省掉一层锁也省掉一处死锁风险。
/// </summary>
sealed class SpectreLogProcessor : IAsyncDisposable
{
    readonly SpectreLogOptions options;
    readonly SpectreMessageFormatter formatter;
    readonly IAnsiConsole? console;
    readonly SpectreObjectPrinter? printer;
    readonly FileLogWriter? fileWriter;
    readonly Channel<SpectreLogEntry> channel;
    readonly Task consumeLoop;
    readonly Task? flushLoop;
    readonly PeriodicTimer? flushTimer;
    readonly CancellationTokenSource cts = new();

    /// <summary>
    /// 消费循环彻底退出的信号。
    /// 不能用 <c>Task.Wait()</c>：<c>Wait</c> 在某些同步上下文下会提前返回或阻塞线程池，
    /// 导致 <see cref="Dispose"/> 返回时日志还没落盘、文件句柄也没关。
    /// </summary>
    readonly ManualResetEventSlim drained = new(false);

    int disposed;

    public SpectreLogProcessor(SpectreLogOptions options, string? resolvedFilePath)
    {
        this.options = options;
        formatter = new SpectreMessageFormatter(options);

        if (options.ConsoleOutput)
        {
            console = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.Detect,
                ColorSystem = ColorSystemSupport.Detect,
                Interactive = InteractionSupport.No
            });
            if (options.ConsoleRenderWidth > 0) console.Profile.Width = options.ConsoleRenderWidth;
            // 绑定终端实际宽度，表格才能按可用空间正确折行。
            printer = options.ObjectPrinter.WithWidth(console.Profile.Width);
        }

        if (!string.IsNullOrWhiteSpace(resolvedFilePath))
        {
            fileWriter = new FileLogWriter(options, formatter, resolvedFilePath);

            if (options.FlushInterval > TimeSpan.Zero)
            {
                flushTimer = new PeriodicTimer(options.FlushInterval);
                flushLoop = Task.Run(FlushLoopAsync);
            }
        }

        channel = Channel.CreateBounded<SpectreLogEntry>(new BoundedChannelOptions(options.QueueCapacity)
        {
            FullMode = options.QueueFullMode,
            SingleReader = true,
            SingleWriter = false
        });

        consumeLoop = Task.Run(ConsumeAsync);
    }

    /// <summary>当前正在写入的日志文件路径；未启用文件输出时为 <c>null</c>。</summary>
    public string? CurrentFileName => fileWriter?.CurrentFileName;

    /// <summary>供 <see cref="SpectreLogger"/> 投递。队列满时按配置策略丢弃或等待。</summary>
    public bool Post(SpectreLogEntry entry)
    {
        if (Volatile.Read(ref disposed) == 1) return false;
        return channel.Writer.TryWrite(entry);
    }

    /// <summary>周期性刷盘，保证进程存活期间日志不会长时间滞留缓冲区。</summary>
    async Task FlushLoopAsync()
    {
        try
        {
            while (await flushTimer!.WaitForNextTickAsync(cts.Token).ConfigureAwait(false))
            {
                await fileWriter!.FlushAsync(cts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (Exception exception)
        {
            options.InternalErrorLogger?.Invoke(exception);
        }
    }

    async Task ConsumeAsync()
    {
        var batchSize = Math.Max(1, options.BatchSize);
        var batch = new SpectreLogEntry[batchSize];

        try
        {
            // 不传 cts.Token：释放只通过 TryComplete 通知结束，
            // 一旦被取消，队列里已入队的日志就会被直接丢弃。
            while (await channel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                var count = 0;
                while (count < batchSize && channel.Reader.TryRead(out var entry))
                {
                    batch[count++] = entry;
                }
                if (count == 0) continue;

                try
                {
                    for (var i = 0; i < count; i++)
                    {
                        await DispatchAsync(batch[i], i == count - 1).ConfigureAwait(false);
                    }
                }
                catch (Exception exception)
                {
                    options.InternalErrorLogger?.Invoke(exception);
                }
                finally
                {
                    Array.Clear(batch, 0, count);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 释放过程中的取消：正常收尾，不是错误。
        }
        catch (Exception exception)
        {
            options.InternalErrorLogger?.Invoke(exception);
        }
        finally
        {
            // 无论正常退出还是被取消，都必须放行等待方。
            drained.Set();
        }
    }

    /// <summary>
    /// 同步排空：关闭入队口并等待消费循环真正退出。
    /// 供同步 <see cref="Dispose"/> 使用，保证返回后文件句柄已释放。
    /// </summary>
    public void Drain(TimeSpan timeout)
    {
        if (drained.IsSet) return;
        // 等待期间让出线程，保证消费循环能在同一个同步上下文里拿到线程继续跑。
        drained.Wait(timeout);
    }

    /// <summary>把一条日志同时送往控制台与文件。任一端失败都不影响另一端。</summary>
    async ValueTask DispatchAsync(SpectreLogEntry entry, bool isLastOfBatch)
    {
        if (console is not null) WriteConsole(entry);
        if (fileWriter is not null)
        {
            // 写盘不用 cts.Token：释放阶段必须保证队列里的日志真正落盘。
            await fileWriter.WriteAsync(fileWriter.Format(entry), isLastOfBatch, CancellationToken.None)
                .ConfigureAwait(false);
        }
    }

    void WriteConsole(SpectreLogEntry entry)
    {
        var (message, objects) = formatter.Format(entry);
        var color = ResolveColor(options.LogLevelColors, entry.LogLevel);

        console!.Write(new Text(
            entry.Timestamp.ToString(options.TimeFormat, System.Globalization.CultureInfo.InvariantCulture)
            + " ",
            new Style(Color.Grey)));
        console.Write(new Text($"[{LevelText(entry.LogLevel)}] ", new Style(color, decoration: Decoration.Bold)));
        console.Write(new Text(entry.Category, new Style(Color.Cyan)));

        if (entry.EventId.Id != 0 || entry.EventId.Name is not null)
        {
            // EventId.ToString() 在有 Name 时只返回 Name，这里补上数字 Id。
            var label = entry.EventId.Name ?? entry.EventId.Id.ToString();
            if (entry.EventId.Name is not null && entry.EventId.Id != 0)
                label += "#" + entry.EventId.Id;
            console.Write(new Text($" [{label}]", new Style(Color.Grey)));
        }

        // 消息用 Text 而非 Markup：日志内容可能来自外部报文，其中的 [xxx] 不该被当成 Spectre 标记解析。
        // 换行直接拼进 Text —— IAnsiConsole.WriteLine 在本版本并非实例成员。
        if (!string.IsNullOrEmpty(message))
            console.Write(new Text(message + Environment.NewLine, new Style(color)));

        if (!string.IsNullOrEmpty(entry.Scopes))
            console.Write(new Text($"  => {entry.Scopes}{Environment.NewLine}", new Style(Color.Grey)));

        foreach (var argument in objects)
        {
            console.Write(new Text(string.Empty));
            IRenderable rendered;
            try
            {
                rendered = argument.Renderer(argument.Value, printer!)
                           ?? new Text("<格式处理器返回 null>", new Style(Color.Red));
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                rendered = new Text($"<专用格式渲染失败: {exception.Message}>", new Style(Color.Red));
            }
            console.Write(rendered);
            console.Write(new Text(string.Empty));
        }

        if (entry.Exception is null) return;

        console.Write(new Text(string.Empty));
        var builder = new StringBuilder(512);
        FileLogWriter.AppendException(builder, entry.Exception);
        foreach (var line in builder.ToString().Split('\n'))
        {
            console.Write(new Text(line.TrimEnd('\r') + Environment.NewLine, new Style(color)));
        }
        console.Write(new Text(string.Empty));
    }

    /// <summary>把 Spectre 颜色名（如 <c>red bold</c>）解析为 <see cref="Color"/>；无法识别时回退为白色。</summary>
    static Color ResolveColor(FrozenDictionary<LogLevel, string> colors, LogLevel level)
    {
        if (!colors.TryGetValue(level, out var name)) return Color.White;
        var space = name.IndexOf(' ');
        var colorName = space < 0 ? name : name[..space];
        return Enum.TryParse<Color>(colorName, ignoreCase: true, out var color) ? color : Color.White;
    }

    internal static string LevelText(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRCE",
        LogLevel.Debug => "DBUG",
        LogLevel.Information => "INFO",
        LogLevel.Warning => "WARN",
        LogLevel.Error => "FAIL",
        LogLevel.Critical => "CRIT",
        _ => level.ToString().ToUpperInvariant()
    };

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 1) return;

        // 先关入队口再等待排空，保证已投递的日志不会丢。
        channel.Writer.TryComplete();

        // 取消必须早于等待：flushLoop 阻塞在 WaitForNextTickAsync(token) 上，
        // 先取消才能让它退出，否则 await 会一直挂着。
        if (flushTimer is not null) await cts.CancelAsync().ConfigureAwait(false);

        try
        {
            await consumeLoop.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            options.InternalErrorLogger?.Invoke(exception);
        }

        drained.Set();

        if (flushTimer is not null)
        {
            flushTimer.Dispose();
            if (flushLoop is not null)
            {
                try { await flushLoop.ConfigureAwait(false); }
                catch (Exception exception) { options.InternalErrorLogger?.Invoke(exception); }
            }
        }

        // 文件句柄必须关闭，否则调用方在 Dispose 后立刻读日志会撞上文件占用。
        if (fileWriter is not null)
        {
            try { await fileWriter.DisposeAsync().ConfigureAwait(false); }
            catch (Exception exception) { options.InternalErrorLogger?.Invoke(exception); }
        }

        drained.Dispose();
        cts.Dispose();
    }

    /// <summary>
    /// 同步释放：<see cref="ILoggerProvider.Dispose"/> 只给了同步契约，
    /// 但后台消费是异步的。这里显式等待排空并关闭文件句柄，
    /// 否则进程退出时尾部日志会丢，且日志文件仍被独占。
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 1) return;

        // 只关入队口，不取消消费循环：必须让已入队的日志全部写完再收尾。
        channel.Writer.TryComplete();

        var timeout = options.ShutdownTimeout > TimeSpan.Zero ? options.ShutdownTimeout : TimeSpan.FromSeconds(5);
        if (!drained.Wait(timeout))
        {
            options.InternalErrorLogger?.Invoke(new TimeoutException(
                $"后台日志消费未能在 {timeout.TotalSeconds:F1} 秒内完成，已放弃等待；部分日志可能丢失。"));
        }

        // 消费循环已退出（或已超时），此时才停掉定时刷盘并关闭句柄。
        try
        {
            if (flushTimer is not null)
            {
                cts.Cancel();
                flushTimer.Dispose();
            }
        }
        catch (ObjectDisposedException) { }

        if (fileWriter is not null)
        {
            try
            {
                // 同步关闭文件：即使消费循环超时，也必须释放句柄。
                fileWriter.Dispose();
            }
            catch (Exception exception)
            {
                options.InternalErrorLogger?.Invoke(exception);
            }
        }

        // flushLoop 的异常在此观察，避免未观察异常拖垮进程。
        if (flushLoop is not null) _ = flushLoop.ContinueWith(t => _ = t.Exception,
            TaskContinuationOptions.OnlyOnFaulted);
    }
}

/// <summary>
/// 自建 <see cref="ILogger"/>：只负责把结构化状态快照成不可变条目并入队。
/// 此处不做任何渲染，因此单条日志的调用开销主要就是一次数组拷贝。
/// </summary>
sealed class SpectreLogger : ILogger
{
    readonly string category;
    readonly SpectreLogProcessor processor;
    readonly SpectreLogOptions options;
    readonly TimeProvider clock;
    readonly Func<IExternalScopeProvider?> scopeAccessor;

    public SpectreLogger(string category, SpectreLogProcessor processor, SpectreLogOptions options,
        Func<IExternalScopeProvider?> scopeAccessor)
    {
        this.category = category;
        this.processor = processor;
        this.options = options;
        this.scopeAccessor = scopeAccessor;
        clock = options.Clock;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => scopeAccessor()?.Push(state);

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (logLevel == LogLevel.None) return;

        try
        {
            // 作用域必须在调用线程展开：作用域对象随时可能被 Dispose，留到后台读会拿到脏数据。
            string? scopes = null;
            if (options.IncludeScopes && scopeAccessor() is { } scopes_) scopes = CaptureScopes(scopes_);

            var entry = SpectreLogEntry.Capture(
                logLevel, eventId, state, exception, formatter,
                clock.GetUtcNow(), category, scopes);

            processor.Post(entry);
        }
        catch (Exception error)
        {
            // 日志失败绝不能反过来打断业务代码。
            options.InternalErrorLogger?.Invoke(error);
        }
    }

    string? CaptureScopes(IExternalScopeProvider scopeProvider)
    {
        var builder = new StringBuilder();
        scopeProvider.ForEachScope((scope, state) =>
        {
            var text = (StringBuilder)state!;
            if (text.Length > 0) text.Append(" => ");
            text.Append(scope);
        }, builder);
        return builder.Length == 0 ? null : builder.ToString();
    }
}

/// <summary>
/// 日志提供程序：注册到 <see cref="ILoggingBuilder"/> 后接管控制台与文件双输出。
/// </summary>
public sealed class SpectreConsoleLoggerProvider : ILoggerProvider, ISupportExternalScope, IAsyncDisposable
{
    readonly SpectreLogOptions options;
    readonly SpectreLogProcessor processor;
    IExternalScopeProvider? scopeProvider;

    public SpectreConsoleLoggerProvider(SpectreLogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.TimeProvider);

        options.RegisterDefaultFormats();

        // 每次启动重新解析路径：{Date} 占位符应指向当天，且避免复用上一次运行的滚动序号。
        var path = string.IsNullOrWhiteSpace(options.FilePath)
            ? null
            : options.ResolveFilePath(options.Clock.GetLocalNow().DateTime);

        this.options = options;
        processor = new SpectreLogProcessor(options, path);
    }

    /// <summary>当前正在写入的日志文件路径；未启用文件输出时为 <c>null</c>。</summary>
    public string? CurrentFileName => processor.CurrentFileName;

    public ILogger CreateLogger(string categoryName)
        => new SpectreLogger(categoryName, processor, options, () => scopeProvider);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        => this.scopeProvider = scopeProvider;

    public void Dispose() => processor.Dispose();

    public async ValueTask DisposeAsync()
    {
        await processor.DisposeAsync().ConfigureAwait(false);
    }
}
