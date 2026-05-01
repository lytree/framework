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

    public SpectreConsoleLogProcessor(ZLoggerSpectreConsoleOptions options)
    {
        this.options = options;
        formatter = options.CreateFormatter();
        console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.Detect
        });

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

    async Task WriteLoop()
    {

        await foreach (var entry in channel.Reader.ReadAllAsync())
        {
            try
            {
                var buffer = new ArrayPoolBufferWriter<byte>(16);
                formatter.FormatLogEntry(buffer, entry);
                var formatted = System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);

                console.Markup(formatted);

                if (entry.LogInfo.Exception != null)
                {
                    console.WriteLine();
                    RenderException(entry.LogInfo);
                }

                console.WriteLine();
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
            console.Markup($"{line}");
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
        channel.Writer.Complete();
        await writeLoop;
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