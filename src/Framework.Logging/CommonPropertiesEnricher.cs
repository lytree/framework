using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;

namespace Framework.Logging;

/// <summary>
/// 为每条日志统一注入公共维度：应用名、版本、环境、机器名，以及链路追踪标识。
///
/// <para>
/// 使用 <see cref="LogEvent.AddPropertyIfAbsent(LogEventProperty)"/> 而不是直接赋值，
/// 这样即便调用方在模板里显式传了同名属性，也以调用方为准，不会被 enricher 覆盖。
/// </para>
/// <para>
/// TraceId / SpanId 取自 <see cref="Activity.Current"/>，与 OpenTelemetry、
/// HttpClient 诊断、ASP.NET Core 的分布式追踪天然对齐；没有活动链路时不注入。
/// </para>
/// </summary>
sealed class CommonPropertiesEnricher : ILogEventEnricher
{
	readonly LogEventProperty application;
	readonly LogEventProperty? applicationVersion;
	readonly LogEventProperty environment;
	readonly LogEventProperty machineName;

	public CommonPropertiesEnricher(FrameworkSerilogOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		// 固定值只构造一次：LogEventProperty 不可变，可在所有事件间安全复用。
		application = new LogEventProperty(LogProperties.Application, new ScalarValue(options.Application));
		environment = new LogEventProperty(LogProperties.EnvironmentName, new ScalarValue(options.EnvironmentName));
		machineName = new LogEventProperty(LogProperties.MachineName, new ScalarValue(Environment.MachineName));

		if (!string.IsNullOrWhiteSpace(options.ApplicationVersion))
		{
			applicationVersion = new LogEventProperty(
				LogProperties.ApplicationVersion, new ScalarValue(options.ApplicationVersion));
		}
	}

	public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
	{
		ArgumentNullException.ThrowIfNull(logEvent);

		logEvent.AddPropertyIfAbsent(application);
		logEvent.AddPropertyIfAbsent(environment);
		logEvent.AddPropertyIfAbsent(machineName);
		if (applicationVersion is not null) logEvent.AddPropertyIfAbsent(applicationVersion);

		if (Activity.Current is not { } activity) return;

		logEvent.AddPropertyIfAbsent(new LogEventProperty(
			LogProperties.TraceId, new ScalarValue(activity.TraceId.ToHexString())));
		logEvent.AddPropertyIfAbsent(new LogEventProperty(
			LogProperties.SpanId, new ScalarValue(activity.SpanId.ToHexString())));
	}
}
