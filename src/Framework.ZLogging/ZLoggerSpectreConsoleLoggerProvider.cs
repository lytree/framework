using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Buffers;
using System.Text;
using System.Threading.Channels;
using ZLogger;
using ZLogger.Formatters;

namespace Framework.ZLogging;

public sealed class SpectreConsoleLogProcessor : IAsyncLogProcessor, IAsyncDisposable
{
    readonly ZLoggerSpectreConsoleOptions options;
    readonly IZLoggerFormatter formatter;
    readonly Channel<IZLoggerEntry> channel;
    readonly Task writeLoop;
    readonly IAnsiConsole console;
    readonly StreamWriter? fileWriter;
    readonly PeriodicTimer? flushTimer;
    readonly Task? flushLoop;
    readonly ArrayPoolBufferWriter<byte> bufferWriter;
    readonly StringBuilder plainTextBuilder;
    int disposed;

    const int StreamWriterBufferSize = 65536;

    public SpectreConsoleLogProcessor(ZLoggerSpectreConsoleOptions options)
    {
        this.options = options;
        formatter = options.CreateFormatter();
        console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.Detect
        });

        bufferWriter = new ArrayPoolBufferWriter<byte>(1024);
        plainTextBuilder = new StringBuilder(512);

        if (!string.IsNullOrWhiteSpace(options.FilePath))
        {
            var dir = Path.GetDirectoryName(options.FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            fileWriter = new StreamWriter(
                new FileStream(options.FilePath, options.FileAppend ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.ReadWrite),
                options.FileEncoding,
                StreamWriterBufferSize);

            flushTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            flushLoop = Task.Run(FlushLoop);
        }

        channel = Channel.CreateBounded<IZLoggerEntry>(new BoundedChannelOptions(options.BoundedChannelSize)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        writeLoop = Task.Run(WriteLoop);
    }

    public void Post(IZLoggerEntry log)
    {
        // Dispose 后直接归还 entry，避免 byte[] buffer 泄漏到 GC
        if (disposed == 1 || !channel.Writer.TryWrite(log))
        {
            log.Return();
        }
    }

    async Task FlushLoop()
    {
        try
        {
            while (await flushTimer!.WaitForNextTickAsync().ConfigureAwait(false))
            {
                try
                {
                    await fileWriter!.FlushAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    options.InternalErrorLogger?.Invoke(ex);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    async Task WriteLoop()
    {
        await foreach (var entry in channel.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            try
            {
                bufferWriter.Clear();
                formatter.FormatLogEntry(bufferWriter, entry);
                var formatted = Encoding.UTF8.GetString(bufferWriter.WrittenSpan);

                var hasException = entry.LogInfo.Exception != null;

                // 合并 Markup + WriteLine 为 MarkupLine，减少一次 AnsiConsole 调用
                // 保持原输出行为：无异常时 formatted 行；有异常时 formatted 行 + 异常多行 + 空行
                console.MarkupLine(formatted);

                if (hasException)
                {
                    RenderException(entry.LogInfo);
                    console.WriteLine();
                }

                if (fileWriter != null)
                {
                    plainTextBuilder.Clear();
                    plainTextBuilder.Append(Markup.Remove(formatted));

                    if (hasException)
                    {
                        AppendException(plainTextBuilder, entry.LogInfo.Exception!);
                    }

                    await fileWriter.WriteLineAsync(plainTextBuilder.ToString()).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                options.InternalErrorLogger?.Invoke(ex);
            }
            finally
            {
                // 归还 entry 内部 byte[] buffer 到 ArrayPool，避免 GC 压力
                entry.Return();
            }
        }
    }

    void RenderException(LogInfo info)
    {
        if (options.ExceptionFormatter != null)
        {
            options.ExceptionFormatter(console, info.Exception!);
            return;
        }

        // 复用 plainTextBuilder（WriteLoop 单线程消费，无并发；文件写入路径会 Clear）
        plainTextBuilder.Clear();
        AppendException(plainTextBuilder, info.Exception!);
        var text = plainTextBuilder.ToString();
        foreach (var line in text.Split('\n'))
        {
            console.MarkupLine(EscapeMarkup(line.TrimEnd('\r')));
        }
    }

    /// <summary>
    /// 转义 Spectre.Console Markup 中的 <c>[</c> / <c>]</c> 字符。
    /// 仅当字符串包含需转义字符时才分配新字符串；无转义字符时返回原字符串。
    /// </summary>
    static string EscapeMarkup(string text)
    {
        if (text.Length == 0) return text;

        int escapeCount = 0;
        foreach (var c in text)
        {
            if (c == '[' || c == ']') escapeCount++;
        }

        if (escapeCount == 0) return text;

        return string.Create(text.Length + escapeCount, text, static (span, src) =>
        {
            int i = 0;
            foreach (var c in src)
            {
                if (c == '[')
                {
                    span[i++] = '[';
                    span[i++] = '[';
                }
                else if (c == ']')
                {
                    span[i++] = ']';
                    span[i++] = ']';
                }
                else
                {
                    span[i++] = c;
                }
            }
        });
    }

    /// <summary>
    /// 将异常信息（类型名、消息、内部异常链、堆栈）追加到 <paramref name="sb"/>。
    /// 控制台渲染与文件写入共用此实现，避免逻辑重复。
    /// </summary>
    static void AppendException(StringBuilder sb, Exception ex)
    {
        sb.Append(ex.GetType().FullName);
        sb.Append(": ");
        sb.Append(ex.Message);

        if (ex.InnerException != null)
        {
            sb.Append(" ---> ");
            AppendException(sb, ex.InnerException);
            sb.AppendLine();
            sb.Append("   --- End of inner exception stack trace ---");
        }

        if (ex.StackTrace != null)
        {
            sb.AppendLine();
            sb.Append(ex.StackTrace);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 1) return;

        channel.Writer.Complete();
        try
        {
            await writeLoop.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            options.InternalErrorLogger?.Invoke(ex);
        }

        flushTimer?.Dispose();

        if (flushLoop != null)
        {
            try { await flushLoop.ConfigureAwait(false); }
            catch (Exception ex) { options.InternalErrorLogger?.Invoke(ex); }
        }

        if (fileWriter != null)
        {
            try
            {
                await fileWriter.FlushAsync().ConfigureAwait(false);
                await fileWriter.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                options.InternalErrorLogger?.Invoke(ex);
            }
        }

        bufferWriter.Dispose();
    }
}

public sealed class ZLoggerSpectreConsoleLoggerProvider : ILoggerProvider, IAsyncDisposable
{
    readonly ZLoggerSpectreConsoleOptions options;
    readonly SpectreConsoleLogProcessor processor;

    public ZLoggerSpectreConsoleLoggerProvider(ZLoggerSpectreConsoleOptions options)
    {
        this.options = options;
        processor = new SpectreConsoleLogProcessor(options);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new ZLoggerLogger(categoryName, processor, new ZLoggerOptions
        {
            IncludeScopes = options.IncludeScopes,
            TimeProvider = options.TimeProvider
        }, null);
    }

    public void Dispose()
    {
        // 同步 Dispose 不能长时间阻塞；fire-and-forget 并观察异常，避免未观察异常导致进程崩溃
        var t = processor.DisposeAsync().AsTask();
        if (!t.Wait(TimeSpan.FromSeconds(5)))
        {
            _ = t.ContinueWith(
                x => options.InternalErrorLogger?.Invoke(x.Exception),
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await processor.DisposeAsync().ConfigureAwait(false);
    }
}
