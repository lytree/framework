using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using System.Text;
using System.Text.Json;
using TUnit.Core;

namespace Framework.Logging.Tests.Integration;

/// <summary>
/// 端到端验证：DI 注册、幂等、以及文件输出真的能产出可被机器消费的内容。
/// </summary>
public class SerilogIntegrationTests
{
	[Test]
	public async Task AddFrameworkSerilog_IsIdempotent()
	{
		var directory = NewTempDirectory();
		try
		{
			var services = new ServiceCollection();
			AddFileOnlyLogging(services, directory);
			AddFileOnlyLogging(services, directory);

			using var provider = services.BuildServiceProvider();

			// 重复注册会让每条日志输出多次，这是最难发现的一类日志问题。
			var providerCount = services.Count(descriptor => descriptor.ServiceType == typeof(ILoggerProvider));
			await Assert.That(providerCount).IsEqualTo(1);
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[Test]
	public async Task AddFrameworkSerilog_ThrowsWhenNoSinkConfigured()
	{
		var services = new ServiceCollection();

		await Assert.That(() => services.AddLogging(builder => builder.AddFrameworkSerilog(options =>
			{
				options.ConsoleEnabled = false;
				options.FileEnabled = false;
			})))
			.Throws<InvalidOperationException>();
	}

	[Test]
	public async Task JsonFileOutput_WritesOneJsonObjectPerLine()
	{
		var directory = NewTempDirectory();
		try
		{
			var services = new ServiceCollection();
			services.AddLogging(builder => builder.AddFrameworkSerilog(options =>
			{
				options.Application = "JsonApp";
				options.ConsoleEnabled = false;
				options.FileEnabled = true;
				options.FilePath = Path.Combine(directory, "app-.log");
				options.JsonFileOutput = true;
				options.AsyncWrite = false;
				options.BufferedFile = false;
			}));

			var provider = services.BuildServiceProvider();
			var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Json.Test");

			// 用 Warning 而不是 Information：CLEF 会把默认级别 Information 的 @l 省略以压缩体积。
			logger.LogWarning("订单 {OrderId} 已创建", 12345);
			provider.Dispose();

			var file = SingleFile(directory);
			var firstLine = ReadAllLinesShared(file).First(line => !string.IsNullOrWhiteSpace(line));

			// 先确认确实是 CLEF 行，失败时断言消息会带出实际内容，便于定位格式问题。
			await Assert.That(firstLine).Contains("\"@mt\"");

			using var document = JsonDocument.Parse(firstLine);
			var root = document.RootElement;
			var propertyNames = root.EnumerateObject().Select(property => property.Name).ToArray();

			// CLEF 固定字段：@l 级别、@mt 原始模板、@t 时间戳。
			await Assert.That(propertyNames).Contains("@l");
			await Assert.That(propertyNames).Contains("@mt");
			await Assert.That(propertyNames).Contains("@t");

			await Assert.That(root.GetProperty("@l").GetString()).IsEqualTo("Warning");
			await Assert.That(root.GetProperty("@mt").GetString()).IsEqualTo("订单 {OrderId} 已创建");

			// 业务字段被提升为顶层属性，可直接按字段检索。
			await Assert.That(root.GetProperty("OrderId").GetInt32()).IsEqualTo(12345);
			await Assert.That(root.GetProperty(LogProperties.Application).GetString()).IsEqualTo("JsonApp");
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[Test]
	public async Task TextFileOutput_IncludesStructuredContext()
	{
		var directory = NewTempDirectory();
		try
		{
			var services = new ServiceCollection();
			services.AddLogging(builder => builder.AddFrameworkSerilog(options =>
			{
				options.Application = "TextApp";
				options.ConsoleEnabled = false;
				options.FileEnabled = true;
				options.FilePath = Path.Combine(directory, "app-.log");
				options.AsyncWrite = false;
				options.BufferedFile = false;
			}));

			var provider = services.BuildServiceProvider();
			var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Text.Test");
			logger.LogWarning("库存不足 {Sku}", "A-1");
			provider.Dispose();

			var content = string.Join('\n', ReadAllLinesShared(SingleFile(directory)));

			await Assert.That(content).Contains("[WRN]");
			await Assert.That(content).Contains("TextApp");
			await Assert.That(content).Contains("Text.Test");
			await Assert.That(content).Contains("库存不足 A-1");
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[Test]
	public async Task BundledConfigurationTemplate_IsParseable()
	{
		var path = Path.Combine(AppContext.BaseDirectory, "appsettings.serilog.json");
		await Assert.That(File.Exists(path)).IsTrue();

		// 模板里带 // 注释，必须能被 .NET 的 JSON 配置解析器接受。
		var configuration = new ConfigurationBuilder().AddJsonFile(path, optional: false).Build();

		await Assert.That(configuration.GetSection("Serilog").Exists()).IsTrue();
		await Assert.That(configuration["Serilog:MinimumLevel:Default"]).IsEqualTo("Information");

		var writeTo = configuration.GetSection("Serilog:WriteTo").GetChildren().ToArray();
		await Assert.That(writeTo.Length).IsEqualTo(1);
		await Assert.That(writeTo[0]["Name"]).IsEqualTo("Async");
	}

	[Test]
	public async Task ConfigurationBindsIntoOptions()
	{
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Serilog:Application"] = "FromConfig",
				["Serilog:FileEnabled"] = "true",
				["Serilog:MinimumLevel"] = "Debug",
				["Serilog:LevelOverrides:System"] = "Error"
			})
			.Build();

		var options = new FrameworkSerilogOptions();
		configuration.GetSection(FrameworkSerilogOptions.SectionName).Bind(options);

		await Assert.That(options.Application).IsEqualTo("FromConfig");
		await Assert.That(options.FileEnabled).IsTrue();
		await Assert.That(options.MinimumLevel).IsEqualTo(LogEventLevel.Debug);
		await Assert.That(options.LevelOverrides["System"]).IsEqualTo(LogEventLevel.Error);
	}

	#region 辅助

	static void AddFileOnlyLogging(IServiceCollection services, string directory)
		=> services.AddLogging(builder => builder.AddFrameworkSerilog(options =>
		{
			options.Application = "IdempotentApp";
			options.ConsoleEnabled = false;
			options.FileEnabled = true;
			options.FilePath = Path.Combine(directory, "app-.log");
			options.AsyncWrite = false;
		}));

	static string NewTempDirectory()
	{
		var path = Path.Combine(Path.GetTempPath(), "framework-logging-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(path);
		return path;
	}

	/// <summary>取目录中唯一的日志文件；按天滚动时文件名带日期，不能直接拼。</summary>
	static string SingleFile(string directory)
	{
		var files = Directory.GetFiles(directory, "*.log");
		if (files.Length != 1)
		{
			throw new InvalidOperationException($"预期 1 个日志文件，实际 {files.Length} 个：{string.Join(", ", files)}");
		}
		return files[0];
	}

	/// <summary>共享读：写入侧可能仍持有句柄，独占读会撞上文件占用。</summary>
	static string[] ReadAllLinesShared(string path)
	{
		using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		using var reader = new StreamReader(stream, Encoding.UTF8);
		var lines = new List<string>();
		while (reader.ReadLine() is { } line) lines.Add(line);
		return lines.ToArray();
	}

	#endregion
}
