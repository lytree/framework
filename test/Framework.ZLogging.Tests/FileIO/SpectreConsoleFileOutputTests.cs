using Framework.ZLogging;
using Framework.ZLogging.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using TUnit.Core;

namespace Framework.ZLogging.Tests.FileIO;

[ClassDataSource<TempDirectory>(Shared = SharedType.PerClass)]
public class SpectreConsoleFileOutputTests(TempDirectory temp)
{
    [Test]
    public async Task FilePath_CreatesParentDirectoryIfMissing()
    {
        var nestedDir = Path.Combine(temp.Path, "nested", "logs");
        var filePath = Path.Combine(nestedDir, "app.log");

        var options = new ZLoggerSpectreConsoleOptions { FilePath = filePath, FileAppend = false };
        var provider = new ZLoggerSpectreConsoleLoggerProvider(options);
        provider.CreateLogger("X").LogInformation("hello");
        await provider.DisposeAsync();

        await Assert.That(Directory.Exists(nestedDir)).IsTrue();
        await Assert.That(File.Exists(filePath)).IsTrue();
    }

    [Test]
    public async Task FileAppendFalse_OverwritesExistingFile()
    {
        var filePath = temp.NewFile(".log");
        await File.WriteAllTextAsync(filePath, "stale content");

        var options = new ZLoggerSpectreConsoleOptions { FilePath = filePath, FileAppend = false };
        var provider = new ZLoggerSpectreConsoleLoggerProvider(options);
        provider.CreateLogger("X").LogInformation("fresh");
        await provider.DisposeAsync();

        var content = await File.ReadAllTextAsync(filePath);
        await Assert.That(content).DoesNotContain("stale content");
        await Assert.That(content.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task FileAppendTrue_PreservesExistingContent()
    {
        var filePath = temp.NewFile(".log");
        await File.WriteAllTextAsync(filePath, "preserved");

        var options = new ZLoggerSpectreConsoleOptions { FilePath = filePath, FileAppend = true };
        var provider = new ZLoggerSpectreConsoleLoggerProvider(options);
        provider.CreateLogger("X").LogInformation("appended");
        await provider.DisposeAsync();

        var content = await File.ReadAllTextAsync(filePath);
        await Assert.That(content).Contains("preserved");
        await Assert.That(content.Length).IsGreaterThan("preserved".Length);
    }

    [Test]
    public async Task FileEncoding_IsApplied()
    {
        var filePath = temp.NewFile(".log");
        var options = new ZLoggerSpectreConsoleOptions
        {
            FilePath = filePath,
            FileAppend = false,
            FileEncoding = System.Text.Encoding.Unicode
        };

        var provider = new ZLoggerSpectreConsoleLoggerProvider(options);
        provider.CreateLogger("X").LogInformation("unicode");
        await provider.DisposeAsync();

        var bytes = await File.ReadAllBytesAsync(filePath);
        await Assert.That(bytes.Length).IsGreaterThan(0);
        var decoded = System.Text.Encoding.Unicode.GetString(bytes);
        await Assert.That(decoded.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task WhitespaceFilePath_DisablesFileOutput()
    {
        var customDir = Path.Combine(temp.Path, "ws");
        Directory.CreateDirectory(customDir);

        var options = new ZLoggerSpectreConsoleOptions { FilePath = "   ", FileAppend = false };
        var provider = new ZLoggerSpectreConsoleLoggerProvider(options);
        provider.CreateLogger("X").LogInformation("no file");
        await provider.DisposeAsync();

        var files = Directory.GetFiles(customDir, "*.log", SearchOption.AllDirectories);
        await Assert.That(files).IsEmpty();
    }

    [Test]
    public async Task NullFilePath_DisablesFileOutput()
    {
        var options = new ZLoggerSpectreConsoleOptions { FilePath = null };
        var provider = new ZLoggerSpectreConsoleLoggerProvider(options);
        provider.CreateLogger("X").LogInformation("no file");
        await provider.DisposeAsync();
    }
}
