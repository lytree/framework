using Framework.Logging.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using TUnit.Core;

// Serilog 与 Microsoft.Extensions.Logging 都定义了 ILogger，测试里一律指 MEL 的那个。
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Framework.Logging.Tests;

/// <summary>
/// 验证强类型日志方法与公共字段注入的行为：属性名、级别、作用域传播。
/// </summary>
public class StructuredLoggingTests
{
	[Test]
	public async Task Enricher_InjectsCommonContextFields()
	{
		var (logger, sink) = CreateLogger();
		logger.LogInformation("hello");

		var logEvent = sink.Last!;

		await Assert.That(ReadString(logEvent, LogProperties.Application)).IsEqualTo("TestApp");
		await Assert.That(ReadString(logEvent, LogProperties.ApplicationVersion)).IsEqualTo("1.2.3");
		await Assert.That(ReadString(logEvent, LogProperties.EnvironmentName)).IsEqualTo("Test");
		await Assert.That(ReadString(logEvent, LogProperties.MachineName)).IsEqualTo(Environment.MachineName);
	}

	[Test]
	public async Task Enricher_InjectsTraceId_WhenActivityIsActive()
	{
		var (logger, sink) = CreateLogger();

		using var activity = new System.Diagnostics.Activity("TestOperation").Start();
		logger.LogInformation("hello");

		var logEvent = sink.Last!;
		await Assert.That(ReadString(logEvent, LogProperties.TraceId)).IsEqualTo(activity.TraceId.ToHexString());
		await Assert.That(ReadString(logEvent, LogProperties.SpanId)).IsEqualTo(activity.SpanId.ToHexString());
	}

	[Test]
	public async Task Enricher_DoesNotOverrideExplicitProperty()
	{
		var (logger, sink) = CreateLogger();

		// 调用方显式传入同名属性时，以调用方为准。
		logger.LogInformation("{Application}", "ExplicitApp");

		await Assert.That(ReadString(sink.Last!, LogProperties.Application)).IsEqualTo("ExplicitApp");
	}

	[Test]
	public async Task LogRequestCompleted_EmitsAllStructuredFields()
	{
		var (logger, sink) = CreateLogger();
		logger.LogRequestCompleted("GET", "/api/orders", 200, 12.5);

		var logEvent = sink.Last!;

		await Assert.That(logEvent.Level).IsEqualTo(LogEventLevel.Information);
		await Assert.That(ReadString(logEvent, LogProperties.Method)).IsEqualTo("GET");
		await Assert.That(ReadString(logEvent, LogProperties.Path)).IsEqualTo("/api/orders");
		await Assert.That(ReadString(logEvent, LogProperties.StatusCode)).IsEqualTo("200");
		await Assert.That(ReadString(logEvent, LogProperties.Elapsed)).IsEqualTo("12.5");
	}

	[Test]
	[Arguments(500, LogEventLevel.Error)]
	[Arguments(503, LogEventLevel.Error)]
	[Arguments(400, LogEventLevel.Warning)]
	[Arguments(404, LogEventLevel.Warning)]
	public async Task LogRequestFailed_SelectsLevelByStatusCode(int statusCode, LogEventLevel expected)
	{
		var (logger, sink) = CreateLogger();
		logger.LogRequestFailed("POST", "/api/orders", statusCode, 3);

		await Assert.That(sink.Last!.Level).IsEqualTo(expected);
	}

	[Test]
	public async Task LogRequestCompleted_UsesErrorForServerError()
	{
		var (logger, sink) = CreateLogger();
		logger.LogRequestCompleted("GET", "/api/orders", 500, 1);

		await Assert.That(sink.Last!.Level).IsEqualTo(LogEventLevel.Error);
	}

	[Test]
	public async Task LowFrequencyHelper_DefaultsToDebug()
	{
		var (logger, sink) = CreateLogger();
		logger.LogCacheMiss("order:1");

		await Assert.That(sink.Last!.Level).IsEqualTo(LogEventLevel.Debug);
		await Assert.That(ReadString(sink.Last!, LogProperties.CacheKey)).IsEqualTo("order:1");
	}

	[Test]
	public async Task LogOperationFailed_AttachesException()
	{
		var (logger, sink) = CreateLogger();
		var error = new InvalidOperationException("boom");
		logger.LogOperationFailed("CreateOrder", "E1001", error);

		var logEvent = sink.Last!;
		await Assert.That(logEvent.Level).IsEqualTo(LogEventLevel.Error);
		await Assert.That(logEvent.Exception).IsSameReferenceAs(error);
		await Assert.That(ReadString(logEvent, LogProperties.ErrorCode)).IsEqualTo("E1001");
	}

	[Test]
	public async Task BeginOperationScope_AttachesFieldsToNestedLogs()
	{
		var (logger, sink) = CreateLogger();

		using (logger.BeginOperationScope("CreateOrder", userId: "u-42", tenantId: "t-1"))
		{
			logger.LogInformation("inside scope");
		}

		logger.LogInformation("outside scope");

		// 事件按写入顺序入队，第一条是作用域内、第二条是作用域外。
		var events = sink.Events;
		var inside = events[0];
		var outside = events[^1];

		await Assert.That(ReadString(inside, LogProperties.Operation)).IsEqualTo("CreateOrder");
		await Assert.That(ReadString(inside, LogProperties.UserId)).IsEqualTo("u-42");
		await Assert.That(ReadString(inside, LogProperties.TenantId)).IsEqualTo("t-1");
		await Assert.That(outside.Properties.ContainsKey("Operation")).IsFalse();
	}

	[Test]
	public async Task MessageTemplate_RendersCultureInvariant()
	{
		var (logger, sink) = CreateLogger();
		logger.LogRequestCompleted("GET", "/", 200, 1234.5678);

		// 小数分隔符必须是点号，否则同一条日志在不同区域设置下文本不一致。
		await Assert.That(sink.Last!.RenderMessage()).Contains("1234.5678");
	}

	static (ILogger Logger, CollectingSink Sink) CreateLogger(Action<FrameworkSerilogOptions>? configure = null)
	{
		var sink = new CollectingSink();
		var options = new FrameworkSerilogOptions
		{
			Application = "TestApp",
			ApplicationVersion = "1.2.3",
			EnvironmentName = "Test",
			ConsoleEnabled = false,
			FileEnabled = false,
			MinimumLevel = LogEventLevel.Debug,
			AsyncWrite = false
		};
		configure?.Invoke(options);

		var serilogLogger = new LoggerConfiguration()
			.UseFrameworkDefaults(options)
			.WriteTo.Sink(sink)
			.CreateLogger();

		var factory = LoggerFactory.Create(builder => builder.AddSerilog(serilogLogger, dispose: false));
		return (factory.CreateLogger("Tests.Category"), sink);
	}

	/// <summary>
	/// 读取属性的原始标量值。
	/// 不能用 <c>LogEventPropertyValue.ToString()</c>：它对字符串会返回带引号的字面量形式（"GET"），
	/// 而断言里比较的是裸值（GET）。
	/// </summary>
	static string? ReadString(LogEvent logEvent, string propertyName)
	{
		if (!logEvent.Properties.TryGetValue(propertyName, out var value)) return null;

		return value is ScalarValue { Value: { } raw }
			? Convert.ToString(raw, System.Globalization.CultureInfo.InvariantCulture)
			: value.ToString();
	}
}
