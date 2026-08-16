using Framework.ZLogging;
using Framework.ZLogging.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using TUnit.Core;

namespace Framework.ZLogging.Tests.ExceptionRendering;

[ClassDataSource<TempDirectory>(Shared = SharedType.PerClass)]
public class ExceptionRenderingTests(TempDirectory temp)
{
    [Test]
    public async Task Log_WithException_WritesExceptionDetails()
    {
        var filePath = temp.NewFile(".log");
        var options = new ZLoggerSpectreConsoleOptions
        {
            FilePath = filePath,
            FileAppend = false
        };
        options.UsePlainTextFormatter();

        var provider = new ZLoggerSpectreConsoleLoggerProvider(options);
        var logger = provider.CreateLogger("Ex");

        var inner = new InvalidOperationException("inner");
        var outer = new ApplicationException("outer", inner);
        logger.LogError(outer, "boom");

        await provider.DisposeAsync();

        var content = await File.ReadAllTextAsync(filePath);
        await Assert.That(content).Contains("boom");
        await Assert.That(content).Contains("outer");
        await Assert.That(content).Contains("inner");
        await Assert.That(content).Contains("--- End of inner exception stack trace ---");
    }

    [Test]
    public async Task Log_WithException_WritesExceptionMessage()
    {
        var filePath = temp.NewFile(".log");
        var options = new ZLoggerSpectreConsoleOptions
        {
            FilePath = filePath,
            FileAppend = false
        };
        options.UsePlainTextFormatter();

        var provider = new ZLoggerSpectreConsoleLoggerProvider(options);
        var logger = provider.CreateLogger("Ex");

        var ex = new InvalidOperationException("plain exception message");
        logger.LogError(ex, "boom");

        await provider.DisposeAsync();

        var content = await File.ReadAllTextAsync(filePath);
        await Assert.That(content).Contains("plain exception message");
    }

    [Test]
    public async Task CustomExceptionFormatter_IsUsed()
    {
        var filePath = temp.NewFile(".log");
        var options = new ZLoggerSpectreConsoleOptions
        {
            FilePath = filePath,
            FileAppend = false
        };
        options.UsePlainTextFormatter();
        var customCalled = false;
        options.SetExceptionFormatter((console, ex) =>
        {
            customCalled = true;
            Spectre.Console.AnsiConsole.WriteLine($"CUSTOM:{ex.GetType().Name}");
        });

        var provider = new ZLoggerSpectreConsoleLoggerProvider(options);
        var logger = provider.CreateLogger("Ex");
        logger.LogError(new InvalidOperationException("x"), "boom");

        await provider.DisposeAsync();

        await Assert.That(customCalled).IsTrue();
    }

    [Test]
    public async Task NoStackTrace_DoesNotThrow()
    {
        var filePath = temp.NewFile(".log");
        var options = new ZLoggerSpectreConsoleOptions
        {
            FilePath = filePath,
            FileAppend = false
        };
        options.UsePlainTextFormatter();

        var provider = new ZLoggerSpectreConsoleLoggerProvider(options);
        var logger = provider.CreateLogger("Ex");
        logger.LogError(new Exception("no stack"), "msg");

        await provider.DisposeAsync();

        await Assert.That(File.Exists(filePath)).IsTrue();
    }
}
