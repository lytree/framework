using Framework.ZLogging;
using Microsoft.Extensions.Logging;
using TUnit.Core;
using ZLogger;

namespace Framework.ZLogging.Tests.Extensions;

public class ZLoggerSpectreExtensionsTests
{
    [Test]
    public async Task AddZLoggerSpectreConsole_ConfiguresBuilderSuccessfully()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddZLoggerSpectreConsole(options =>
            {
                options.UsePlainTextFormatter();
            });
        });

        var logger = loggerFactory.CreateLogger("Test");
        logger.LogInformation("Test message");
        await Task.CompletedTask;
    }

    [Test]
    public async Task AddZLoggerSpectreConsoleAndFile_WithFilePath_ConfiguresSuccessfully()
    {
        var filePath = Path.Combine(Path.GetTempPath(), "test1-" + Guid.NewGuid().ToString("N") + ".log");
        ILoggerFactory? loggerFactory = null;

        try
        {
            loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddZLoggerSpectreConsoleAndFile(filePath);
            });

            var logger = loggerFactory.CreateLogger("Test");
            logger.ZLogInformation($"Test message with file {filePath}");
        }
        finally
        {
            loggerFactory?.Dispose();
            TryDelete(filePath);
        }
        await Task.CompletedTask;
    }

    [Test]
    public async Task AddZLoggerSpectreConsoleAndFile_NullFilePath_ConfiguresWithoutFile()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddZLoggerSpectreConsoleAndFile(null);
        });

        var logger = loggerFactory.CreateLogger("Test");
        logger.LogInformation("Test without file");
        await Task.CompletedTask;
    }

    [Test]
    public async Task AddZLoggerSpectreConsoleAndFile_EmptyFilePath_ConfiguresWithoutFile()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddZLoggerSpectreConsoleAndFile("");
        });

        var logger = loggerFactory.CreateLogger("Test");
        logger.LogInformation("Test with empty path");
        await Task.CompletedTask;
    }

    [Test]
    public async Task AddZLoggerSpectreConsole_ReturnsBuilderForChaining()
    {
        ILoggingBuilder? captured = null;
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            captured = builder.AddZLoggerSpectreConsole(_ => { });
        });

        await Assert.That(captured).IsNotNull();
    }

    static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
    }
}
