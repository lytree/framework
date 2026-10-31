using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Text;

namespace Framework.ZLogging;

/// <summary>
/// 文件日志写入器：长连接 <see cref="FileStream"/> + 批量写 + 按体积滚动 + 按天换文件 + 断连重连。
///
/// 取舍说明：
/// <list type="bullet">
/// <item>用 <see cref="FileOptions.Asynchronous"/> + <see cref="FileOptions.SequentialScan"/>，
/// 让 OS 合并顺序写并走异步 IO 路径；不用 <c>AutoFlush</c>，改由批末或定时器刷盘。</item>
/// <item><see cref="FileShare.ReadWrite"/> 允许日志被 tail / VS 实时读取；
/// <see cref="FileShare.Delete"/> 允许 logrotate 之类的工具在写入期间轮转文件。</item>
/// <item>文件被外部删除或移动后句柄会静默失效，因此按固定间隔做一次存在性检查并重连。</item>
/// <item>路径含 <c>{Date}</c> 时按本地日期分文件，跨零点自动切换，常驻进程无需重启。</item>
/// </list>
/// </summary>
sealed class FileLogWriter : IAsyncDisposable
{
    const int StreamBufferSize = 4096;

    /// <summary>每写这么多条做一次文件存在性检查，兼顾及时性与目录访问开销。</summary>
    const int PeriodicCheckInterval = 100;

    readonly SpectreLogOptions options;
    readonly SpectreMessageFormatter formatter;
    readonly SpectreObjectPrinter printer;
    readonly bool rollingEnabled;
    readonly bool dateRolling;
    readonly char[] newline;

    string basePath;
    DateTime currentDate;
    FileStream? stream;
    StreamWriter? writer;
    long pendingBytes;
    string currentPath;
    int writeCount;
    int reconnecting;
    volatile bool disposed;

    public FileLogWriter(SpectreLogOptions options, SpectreMessageFormatter formatter, string resolvedPath)
    {
        this.options = options;
        this.formatter = formatter;
        // 绑定文件渲染宽度，让表格 / 面板在日志文件里按该宽度布局。
        printer = options.ObjectPrinter.WithWidth(options.FileRenderWidth);
        basePath = resolvedPath;
        currentPath = resolvedPath;
        rollingEnabled = options.FileSizeLimitBytes > 0;
        // 只有路径里真的带 {Date} 才需要跨天检查，否则整条判断退化为一次 bool 读。
        dateRolling = options.HasDateToken;
        currentDate = Today();
        newline = ['\n'];

        // 构造期即建目录并打开文件：路径不可写要在启动时就暴露，而不是等到第一条日志才失败。
        try
        {
            EnsureDirectory(currentPath);
        }
        catch (Exception exception)
        {
            Report(exception);
        }

        EnsureOpen();
    }

    /// <summary>统一上报写入侧异常；绝不让文件问题冒泡到业务线程。</summary>
    void Report(Exception exception)
    {
        options.FileWriteErrorHandler?.Invoke(new FileWriteError(currentPath, exception));
        options.InternalErrorLogger?.Invoke(exception);
    }

    /// <summary>当前正在写入的文件路径；滚动或重连后会变化。</summary>
    public string CurrentFileName => currentPath;

    /// <summary>取本地日期；统一走 <see cref="SpectreLogOptions.Clock"/>，便于测试注入。</summary>
    DateTime Today() => options.Clock.GetLocalNow().DateTime.Date;

    /// <summary>
    /// <c>{Date}</c> 占位符跨天换文件。
    /// 常驻进程夜里跨零点后如果仍写启动当天的文件名，日志会混在昨天的文件里，
    /// 按天归档直接失效——因此在写每批日志前用一次日期比较兜住。
    /// </summary>
    async ValueTask EnsureDateCurrentAsync()
    {
        if (!dateRolling || disposed) return;

        var today = Today();
        if (today == currentDate) return;

        await CloseStreamAsync().ConfigureAwait(false);

        currentDate = today;
        // 重新解析而非字符串替换：目录部分也可能含 {Date}，且要重新展开环境变量。
        basePath = options.ResolveFilePath(today);
        currentPath = basePath;
        writeCount = 0;
        EnsureOpen();
    }

    /// <summary>把一条日志渲染为纯文本块（含时间戳、级别、Category 与专用格式的文本化输出）。</summary>
    public string Format(SpectreLogEntry entry)
    {
        var (message, objects) = formatter.Format(entry);
        var builder = new StringBuilder(256);

        builder.Append(entry.Timestamp.ToString(options.TimeFormat, System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(" [").Append(SpectreLogProcessor.LevelText(entry.LogLevel)).Append("] ");
        builder.Append(entry.Category);

        if (entry.EventId.Id != 0 || entry.EventId.Name is not null)
        {
            // EventId.ToString() 在有 Name 时只返回 Name，这里补上数字 Id 以免丢失可检索信息。
            builder.Append(" [").Append(entry.EventId.Name ?? entry.EventId.Id.ToString());
            if (entry.EventId.Name is not null && entry.EventId.Id != 0)
                builder.Append('#').Append(entry.EventId.Id);
            builder.Append(']');
        }

        if (!string.IsNullOrEmpty(message)) builder.Append(' ').Append(message);

        // 专用格式在控制台是表格 / 面板，在文件里退化为等宽文本，信息不丢失。
        foreach (var argument in objects)
        {
            builder.AppendLine();
            IRenderable rendered;
            try
            {
                rendered = argument.Renderer(argument.Value, printer)
                           ?? new Text("<格式处理器返回 null>");
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                rendered = new Text($"<专用格式渲染失败: {exception.Message}>");
            }
            builder.Append(SpectreObjectPrinter.ExportText(rendered, options.FileRenderWidth));
        }

        if (entry.Exception is not null)
        {
            builder.AppendLine().Append(AppendException(entry.Exception));
        }

        if (!string.IsNullOrEmpty(entry.Scopes))
        {
            builder.AppendLine().Append("  => ").Append(entry.Scopes);
        }

        return builder.ToString();
    }

    /// <summary>打开（必要时重开）文件流。失败时通过 <see cref="SpectreLogOptions.FileWriteErrorHandler"/> 通知并降级为不写文件。</summary>
    void EnsureOpen()
    {
        if (writer is not null) return;

        try
        {
            EnsureDirectory(currentPath);
            var fileStream = new FileStream(
                currentPath,
                options.FileAppend ? FileMode.Append : FileMode.Create,
                FileAccess.Write,
                FileShare.ReadWrite | FileShare.Delete,
                StreamBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            stream = fileStream;
            writer = new StreamWriter(fileStream, options.FileEncoding, StreamBufferSize, leaveOpen: true)
            {
                AutoFlush = false,
                NewLine = "\n"
            };
        }
        catch (Exception exception)
        {
            Report(exception);
            // 打开失败时保持 writer 为 null，后续写入会被静默跳过，不影响控制台链路。
        }
    }

    /// <summary>写入一批已格式化的文本。滚动判定与刷盘都在这里完成。</summary>
    public async ValueTask WriteAsync(string text, bool isLastOfBatch, CancellationToken cancellationToken)
    {
        if (disposed) return;

        if (dateRolling) await EnsureDateCurrentAsync().ConfigureAwait(false);

        if (rollingEnabled && writer is not null
            && stream!.Length + pendingBytes >= options.FileSizeLimitBytes)
        {
            await RollAsync(cancellationToken).ConfigureAwait(false);
        }

        EnsureOpen();
        if (writer is null) return;

        var retry = 0;
        const int maxRetries = 3;
        while (true)
        {
            try
            {
                // 显式补换行：StreamWriter.WriteLine 会引入额外调用，
                // 这里一次写入「内容 + 换行」，既保证逐行可读，也减少写盘次数。
                var payload = text.Length + 1;
                await writer.WriteAsync(text.AsMemory(0, text.Length), cancellationToken).ConfigureAwait(false);
                await writer.WriteAsync(newline, cancellationToken).ConfigureAwait(false);
                // 未刷盘的数据不计入 FileStream.Length，滚动判定必须自行累计。
                pendingBytes += payload;
                if (isLastOfBatch)
                {
                    await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                    pendingBytes = 0;
                }

                // 显式刷盘时或每 N 次写入时确认文件仍在原处（可能被外部删除 / 移动）。
                if (isLastOfBatch || ++writeCount >= PeriodicCheckInterval)
                {
                    writeCount = 0;
                    if (!File.Exists(currentPath)) await ReconnectAsync(cancellationToken).ConfigureAwait(false);
                }
                return;
            }
            catch (IOException) when (retry < maxRetries - 1)
            {
                // 瞬时 IO 冲突（如 logrotate 正在搬文件）：线性退避后重试。
                await Task.Delay(100 * ++retry, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (IsRecoverable(exception) && retry < maxRetries - 1)
            {
                await ReconnectAsync(cancellationToken).ConfigureAwait(false);
                await Task.Delay(100 * ++retry, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Report(exception);
                return;
            }
        }
    }

    /// <summary>超过体积上限时切换到下一个滚动文件，并淘汰超出保留数量的最旧文件。</summary>
    async Task RollAsync(CancellationToken cancellationToken)
    {
        var next = GetNextFileName();
        if (string.Equals(next, currentPath, StringComparison.OrdinalIgnoreCase)) return;

        await CloseStreamAsync().ConfigureAwait(false);
        currentPath = next;
        await DeleteOverflowAsync(cancellationToken).ConfigureAwait(false);
        EnsureOpen();
    }

    /// <summary>计算下一个滚动文件名。基名首次溢出时切到 <c>_1</c>，其后依次递增。</summary>
    string GetNextFileName()
    {
        var directory = Path.GetDirectoryName(basePath) ?? string.Empty;
        var stem = Path.GetFileNameWithoutExtension(basePath);
        var extension = Path.GetExtension(basePath);

        // 解析当前序号：app.log -> 0，app_3.log -> 3
        var index = 0;
        if (!string.Equals(currentPath, basePath, StringComparison.OrdinalIgnoreCase))
        {
            var current = Path.GetFileNameWithoutExtension(currentPath);
            if (current.Length > stem.Length
                && current.StartsWith(stem, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(current.AsSpan(stem.Length).TrimStart('_'), out var parsed))
            {
                index = parsed;
            }
        }

        var next = index + 1;
        // 滚动文件序号循环覆盖，避免文件无限增长。
        // 序号范围是 1..(MaxRollingFiles-1)：基文件已占一个名额。
        if (options.MaxRollingFiles > 1)
        {
            var rollingSlots = options.MaxRollingFiles - 1;
            next = (next - 1) % rollingSlots + 1;
        }

        return Path.Combine(directory, stem + "_" + next + extension);
    }

    /// <summary>
    /// 淘汰超出保留数量的最旧滚动文件。
    /// 计数包含基文件本身：<c>MaxRollingFiles = 5</c> 意味着最多 5 个文件。
    /// 基文件一旦被写入就长期保留（它是用户配置的主日志），只淘汰带序号的滚动文件。
    /// </summary>
    async Task DeleteOverflowAsync(CancellationToken cancellationToken)
    {
        if (options.MaxRollingFiles <= 1) return;

        try
        {
            var directory = Path.GetDirectoryName(basePath);
            if (!Directory.Exists(directory)) return;

            var stem = Path.GetFileNameWithoutExtension(basePath);
            var extension = Path.GetExtension(basePath);
            var baseFull = Path.GetFullPath(basePath);

            // 基文件占一个名额，其余名额分配给滚动文件。
            var rollingSlots = options.MaxRollingFiles - 1;

            // 只保留最新的 rollingSlots 个滚动文件，其余删除。
            var stale = new DirectoryInfo(directory)
                .EnumerateFiles($"{stem}_*{extension}", SearchOption.TopDirectoryOnly)
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Skip(rollingSlots);

            foreach (var file in stale)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.Equals(file.FullName, baseFull, StringComparison.OrdinalIgnoreCase)) continue;
                try { file.Delete(); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
        catch (Exception exception)
        {
            options.InternalErrorLogger?.Invoke(exception);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>文件被外部删除 / 移动后重建句柄，重新从基名开始写。</summary>
    async Task ReconnectAsync(CancellationToken cancellationToken)
    {
        if (disposed) return;
        // 用交换而非锁：多线程同时发现失效时只允许一个真正重连。
        if (Interlocked.Exchange(ref reconnecting, 1) == 1) return;

        try
        {
            await CloseStreamAsync().ConfigureAwait(false);
            currentPath = basePath;
            EnsureOpen();
        }
        finally
        {
            Interlocked.Exchange(ref reconnecting, 0);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>刷盘但不关闭；供定时器调用。</summary>
    public async ValueTask FlushAsync(CancellationToken cancellationToken)
    {
        if (writer is null) return;
        try
        {
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            pendingBytes = 0;
        }
        catch (Exception exception)
        {
            options.InternalErrorLogger?.Invoke(exception);
        }
    }

    async Task CloseStreamAsync()
    {
        if (writer is null) return;
        try
        {
            await writer.FlushAsync().ConfigureAwait(false);
            await writer.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            options.InternalErrorLogger?.Invoke(exception);
        }
        finally
        {
            writer = null;
            pendingBytes = 0;
            if (stream is not null)
            {
                await stream.DisposeAsync().ConfigureAwait(false);
                stream = null;
            }
        }
    }

    /// <summary>异常是否代表句柄失效、可通过重连恢复。</summary>
    static bool IsRecoverable(Exception exception) => exception switch
    {
        FileNotFoundException or DirectoryNotFoundException or ObjectDisposedException => true,
        UnauthorizedAccessException => true,
        // 低 16 位为 Win32 错误码：2=NOT_FOUND 3=PATH_NOT_FOUND 6=INVALID_HANDLE 32=SHARING_VIOLATION
        IOException io => (io.HResult & 0xFFFF) is 2 or 3 or 6 or 32,
        _ => false
    };

    static void EnsureDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
    }

    /// <summary>异常信息：类型名、消息、内部异常链、堆栈。控制台与文件共用同一实现。</summary>
    internal static void AppendException(StringBuilder builder, Exception exception)
    {
        builder.Append(exception.GetType().FullName).Append(": ").Append(exception.Message);

        if (exception.InnerException is { } inner)
        {
            builder.Append(" ---> ");
            AppendException(builder, inner);
            builder.AppendLine().Append("   --- End of inner exception stack trace ---");
        }

        if (exception.StackTrace is { } stack)
        {
            builder.AppendLine().Append(stack);
        }
    }

    internal static string AppendException(Exception exception)
    {
        var builder = new StringBuilder(512);
        AppendException(builder, exception);
        return builder.ToString();
    }

    static string LevelText(LogLevel level) => level switch
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
        if (disposed) return;
        disposed = true;
        await CloseStreamAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// 同步释放。ILoggerProvider.Dispose 是同步契约，
    /// 同步关闭句柄可确保调用方返回后日志文件不再被独占。
    /// </summary>
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (writer is null) return;

        try
        {
            writer.Flush();
            writer.Dispose();
        }
        catch (Exception exception)
        {
            options.InternalErrorLogger?.Invoke(exception);
        }
        finally
        {
            writer = null;
            pendingBytes = 0;
            stream?.Dispose();
            stream = null;
        }
    }
}
