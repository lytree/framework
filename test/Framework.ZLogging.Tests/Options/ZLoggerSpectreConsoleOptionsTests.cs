using System.Collections.Frozen;
using Framework.ZLogging;
using Microsoft.Extensions.Logging;
using ZLogger;
using ZLogger.Formatters;

namespace Framework.ZLogging.Tests.Options;

public class ZLoggerSpectreConsoleOptionsTests
{
    [Test]
    public async Task Defaults_AreSensible()
    {
        var opts = new ZLoggerSpectreConsoleOptions();

        await Assert.That(opts.TimeFormat).IsEqualTo("yyyy-MM-dd HH:mm:ss");
        await Assert.That(opts.CategoryColor).IsEqualTo("cyan");
        await Assert.That(opts.BoundedChannelSize).IsEqualTo(1024);
        await Assert.That(opts.IncludeScopes).IsFalse();
        await Assert.That(opts.TimeProvider).IsNull();
        await Assert.That(opts.FileEncoding).IsEqualTo(System.Text.Encoding.UTF8);
        await Assert.That(opts.FileAppend).IsTrue();
        await Assert.That(opts.FilePath).IsNull();
    }

    [Test]
    public async Task DefaultLogLevelColors_AreFrozenAndContainAllStandardLevels()
    {
        var opts = new ZLoggerSpectreConsoleOptions();
        var colors = opts.LogLevelColors;

        await Assert.That(colors).IsTypeOf<FrozenDictionary<LogLevel, string>>();
        // 默认字典只覆盖有意义的日志级别：Trace/Debug/Information/Warning/Error/Critical
        var expected = new[]
        {
            LogLevel.Trace, LogLevel.Debug, LogLevel.Information,
            LogLevel.Warning, LogLevel.Error, LogLevel.Critical
        };
        foreach (var level in expected)
        {
            await Assert.That(colors.ContainsKey(level)).IsTrue();
        }
    }

    [Test]
    public async Task LogLevelColors_NullAssignment_RestoresDefaults()
    {
        var opts = new ZLoggerSpectreConsoleOptions
        {
            LogLevelColors = new Dictionary<LogLevel, string> { { LogLevel.Warning, "magenta" } }.ToFrozenDictionary()
        };
        await Assert.That(opts.LogLevelColors[LogLevel.Warning]).IsEqualTo("magenta");

        opts.LogLevelColors = null!;
        await Assert.That(opts.LogLevelColors[LogLevel.Warning]).IsEqualTo("yellow");
    }

    [Test]
    public async Task UsePlainTextFormatter_ReturnsSelf_AndSetsPlainTextFormatter()
    {
        var opts = new ZLoggerSpectreConsoleOptions();
        var returned = opts.UsePlainTextFormatter();

        await Assert.That(returned).IsSameReferenceAs(opts);
        await Assert.That(opts.CreateFormatter()).IsTypeOf<PlainTextZLoggerFormatter>();
    }

    [Test]
    public async Task UseFormatter_OverridesFactory()
    {
        var opts = new ZLoggerSpectreConsoleOptions();
        var custom = new PlainTextZLoggerFormatter();

        opts.UseFormatter(() => custom);

        await Assert.That(opts.CreateFormatter()).IsSameReferenceAs(custom);
    }

    [Test]
    public async Task SetExceptionFormatter_StoresDelegate()
    {
        var opts = new ZLoggerSpectreConsoleOptions();
        Action<Spectre.Console.IAnsiConsole, Exception> formatter = (_, _) => { };

        opts.SetExceptionFormatter(formatter);

        await Assert.That(opts.ExceptionFormatter).IsSameReferenceAs(formatter);
    }
}
