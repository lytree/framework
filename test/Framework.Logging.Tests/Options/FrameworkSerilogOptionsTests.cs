using Serilog;
using Serilog.Events;
using TUnit.Core;

namespace Framework.Logging.Tests.Options;

public class FrameworkSerilogOptionsTests
{
	[Test]
	public async Task Defaults_AreSensible()
	{
		var options = new FrameworkSerilogOptions();

		await Assert.That(options.Application).IsNotNullOrWhiteSpace();
		await Assert.That(options.ConsoleEnabled).IsTrue();
		await Assert.That(options.FileEnabled).IsFalse();
		await Assert.That(options.JsonFileOutput).IsFalse();
		await Assert.That(options.MinimumLevel).IsEqualTo(LogEventLevel.Information);
		await Assert.That(options.RollingInterval).IsEqualTo(RollingInterval.Day);
		await Assert.That(options.FileSizeLimitBytes).IsEqualTo(50L * 1024 * 1024);
		await Assert.That(options.RetainedFileCountLimit).IsEqualTo(31);
		await Assert.That(options.AsyncWrite).IsTrue();
		await Assert.That(options.BlockWhenQueueFull).IsFalse();
	}

	[Test]
	public async Task LevelOverrides_SilenceFrameworkNoiseByDefault()
	{
		var options = new FrameworkSerilogOptions();

		await Assert.That(options.LevelOverrides.ContainsKey("Microsoft")).IsTrue();
		await Assert.That(options.LevelOverrides["Microsoft"]).IsEqualTo(LogEventLevel.Warning);
	}

	[Test]
	public async Task Validate_RejectsEmptyApplication()
	{
		var options = new FrameworkSerilogOptions { Application = "   " };

		await Assert.That(options.Validate).Throws<ArgumentException>();
	}

	[Test]
	public async Task Validate_RejectsNonPositiveQueueCapacity()
	{
		var options = new FrameworkSerilogOptions { QueueCapacity = 0 };

		await Assert.That(options.Validate).Throws<ArgumentOutOfRangeException>();
	}

	[Test]
	public async Task Validate_RejectsZeroFileSizeLimit_WhenFileEnabled()
	{
		var options = new FrameworkSerilogOptions { FileEnabled = true, FileSizeLimitBytes = 0 };

		await Assert.That(options.Validate).Throws<ArgumentOutOfRangeException>();
	}

	[Test]
	public async Task Validate_AllowsFileDisabled()
	{
		var options = new FrameworkSerilogOptions { FileEnabled = false, FileSizeLimitBytes = 0 };

		// 不写文件时体积限制无意义，不应因此报错。
		await Assert.That(options.Validate).ThrowsNothing();
	}

	[Test]
	public async Task ResolveFilePath_DefaultsToLogsFolderUnderBaseDirectory()
	{
		var options = new FrameworkSerilogOptions { Application = "OrderService" };

		var path = options.ResolveFilePath();

		await Assert.That(Path.IsPathRooted(path)).IsTrue();
		await Assert.That(path).Contains("OrderService");
		await Assert.That(path.EndsWith("OrderService-.log", StringComparison.Ordinal)).IsTrue();
		await Assert.That(path.StartsWith(AppContext.BaseDirectory, StringComparison.Ordinal)).IsTrue();
	}

	[Test]
	public async Task ResolveFilePath_KeepsAbsolutePath()
	{
		var absolute = Path.Combine(Path.GetTempPath(), "app-.log");
		var options = new FrameworkSerilogOptions { FilePath = absolute };

		await Assert.That(options.ResolveFilePath()).IsEqualTo(absolute);
	}

	[Test]
	public async Task ResolveFilePath_ReplacesInvalidFileNameCharacters()
	{
		// 应用名里带路径分隔符时，直接拼进文件名会让写盘失败。
		var options = new FrameworkSerilogOptions { Application = "Svc/With:Bad*Name" };

		var fileName = Path.GetFileName(options.ResolveFilePath());

		await Assert.That(fileName).DoesNotContain("/");
		await Assert.That(fileName).DoesNotContain(":");
		await Assert.That(fileName).DoesNotContain("*");
	}

	[Test]
	public async Task UseFrameworkDefaults_DoesNotThrow_WhenBothBuiltInSinksDisabled()
	{
		// 关闭内置 sink 是为了让调用方挂载 Seq / OpenTelemetry 等自定义 sink。
		var options = new FrameworkSerilogOptions { ConsoleEnabled = false, FileEnabled = false, AsyncWrite = false };

		var configuration = new LoggerConfiguration().UseFrameworkDefaults(options);

		await Assert.That(configuration).IsNotNull();
	}
}
