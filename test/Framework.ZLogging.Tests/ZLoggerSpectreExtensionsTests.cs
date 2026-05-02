using Microsoft.Extensions.Logging;
using TUnit.Core;
using ZLogger;

namespace Framework.ZLogging.Tests;

public class ZLoggerSpectreExtensionsTests
{
    [Test]
    public void AddZLoggerSpectreConsole_ConfiguresBuilderSuccessfully()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddZLoggerSpectreConsole(options =>
            {
                options.UsePlainTextFormatter();
            });
        });

        var logger = loggerFactory.CreateLogger("Test");

        logger.LogInformation("Test message");
    }

    [Test]
    public void AddZLoggerSpectreConsoleAndFile_WithFilePath_ConfiguresSuccessfully()
    {
        var filePath = Path.Combine(Path.GetTempPath(), "test1.log");

        try
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddZLoggerSpectreConsoleAndFile(filePath);
            });

            var logger = loggerFactory.CreateLogger("Test");
            logger.ZLogInformation($"Test message with file  {filePath}");
        }
        finally
        {
            
        }
    }

    [Test]
    public void AddZLoggerSpectreConsoleAndFile_NullFilePath_ConfiguresWithoutFile()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddZLoggerSpectreConsoleAndFile(null);
        });

        var logger = loggerFactory.CreateLogger("Test");
        logger.LogInformation("Test without file");
    }

    [Test]
    public void AddZLoggerSpectreConsoleAndFile_EmptyFilePath_ConfiguresWithoutFile()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddZLoggerSpectreConsoleAndFile("");
        });

        var logger = loggerFactory.CreateLogger("Test");
        logger.LogInformation("Test with empty path");
    }
}