using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Buffers;
using System.Text;
using System.Threading.Channels;
using ZLogger;
using ZLogger.Formatters;

namespace Framework.ZLogging;

public class SpectreConsoleLogProcessor : IAsyncLogProcessor, IAsyncDisposable
{
    readonly ZLoggerSpectreConsoleOptions options;
    readonly IZLoggerFormatter formatter;
    readonly Channel<IZLoggerEntry> channel;
    readonly Task writeLoop;
    readonly IAnsiConsole console;
    readonly StreamWriter? fileWriter;
    readonly PeriodicTimer? flushTimer;
    readonly Task flushLoop;
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
        channel.Writer.TryWrite(log);
    }

    async Task FlushLoop()
    {
        try
        {
            while (await flushTimer!.WaitForNextTickAsync())
            {
                try
                {
                    await fileWriter!.FlushAsync();
                }
                catch { }
            }
        }
        catch (ObjectDisposedException) { }
    }

    async Task WriteLoop()
    {
        await foreach (var entry in channel.Reader.ReadAllAsync())
        {
            try
            {
                bufferWriter.Clear();
                formatter.FormatLogEntry(bufferWriter, entry);
                var formatted = Encoding.UTF8.GetString(bufferWriter.WrittenSpan);

                console.Markup(formatted);

                if (entry.LogInfo.Exception != null)
                {
                    console.WriteLine();
                    RenderException(entry.LogInfo);
                }

                console.WriteLine();

                if (fileWriter != null)
                {
                    plainTextBuilder.Clear();
                    plainTextBuilder.Append(Markup.Remove(formatted));

                    if (entry.LogInfo.Exception != null)
                    {
                        AppendExceptionLines(plainTextBuilder, entry.LogInfo.Exception);
                    }

                    plainTextBuilder.AppendLine();
                    fileWriter.Write(plainTextBuilder);
                }
            }
            catch (Exception ex)
            {
                options.InternalErrorLogger?.Invoke(ex);
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

        var lines = GetExceptionLines(info.Exception!);
        foreach (var line in lines)
        {
            console.Markup($"{EscapeMarkup(line)}");
        }
    }

    static string EscapeMarkup(string text)
    {
        return text.Replace("[", "[[").Replace("]", "]]");
    }

    static void AppendExceptionLines(StringBuilder sb, Exception ex)
    {
        sb.AppendLine();
        sb.Append(ex.GetType().FullName);
        sb.Append(": ");
        sb.Append(ex.Message);

        if (ex.InnerException != null)
        {
            sb.Append(" ---> ");
            AppendExceptionLines(sb, ex.InnerException);
            sb.AppendLine();
            sb.Append("   --- End of inner exception stack trace ---");
        }

        if (ex.StackTrace != null)
        {
            sb.AppendLine();
            sb.Append(ex.StackTrace);
        }
    }

    List<string> GetExceptionLines(Exception ex)
    {
        var lines = new List<string>();
        lines.Add($"{ex.GetType().FullName}: {ex.Message}");

        if (ex.InnerException != null)
        {
            lines.Add(" ---> ");
            lines.AddRange(GetExceptionLines(ex.InnerException));
            lines.Add("   --- End of inner exception stack trace ---");
        }

        if (ex.StackTrace != null)
        {
            lines.Add(ex.StackTrace);
        }

        return lines;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 1) return;

        channel.Writer.Complete();
        await writeLoop;

        flushTimer?.Dispose();

        if (flushLoop != null)
        {
            try { await flushLoop; } catch { }
        }

        if (fileWriter != null)
        {
            await fileWriter.FlushAsync();
            await fileWriter.DisposeAsync();
        }

        bufferWriter.Dispose();
    }
}

public class ZLoggerSpectreConsoleLoggerProvider : ILoggerProvider, IAsyncDisposable
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
        }, options.IncludeScopes ? null : null);
    }

    public void Dispose()
    {
        processor.DisposeAsync().AsTask().Wait();
    }

    public async ValueTask DisposeAsync()
    {
        await processor.DisposeAsync();
    }
}
