using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;

namespace Framework.Logging;

/// <summary>
/// 把 Serilog 接入 <see cref="ILoggingBuilder"/> 的唯一入口。
///
/// <para><b>两种用法</b></para>
/// <code>
/// // 1) 纯代码配置
/// builder.Logging.AddFrameworkSerilog(o =>
/// {
///     o.Application = "OrderService";
///     o.FileEnabled = true;
///     o.JsonFileOutput = true;
/// });
///
/// // 2) appsettings.json 驱动的输出目标 + 代码补充公共字段
/// builder.Logging.AddFrameworkSerilog(builder.Configuration, o => o.Application = "OrderService");
/// </code>
/// </summary>
public static class SerilogLoggingExtensions
{
	/// <summary>纯代码配置。</summary>
	public static ILoggingBuilder AddFrameworkSerilog(this ILoggingBuilder builder,
		Action<FrameworkSerilogOptions>? configure = null)
		=> builder.AddFrameworkSerilog(null, configure);

	/// <summary>
	/// 从 <paramref name="configuration"/> 的 <c>Serilog</c> 节绑定基础配置，
	/// 再应用 <paramref name="configure"/> 做代码侧的最终覆盖。
	/// </summary>
	public static ILoggingBuilder AddFrameworkSerilog(this ILoggingBuilder builder,
		IConfiguration? configuration, Action<FrameworkSerilogOptions>? configure = null)
	{
		var options = new FrameworkSerilogOptions();
		configuration?.GetSection(FrameworkSerilogOptions.SectionName).Bind(options);
		configure?.Invoke(options);
		return builder.AddFrameworkSerilog(options, configuration);
	}

	/// <summary>用已构造好的配置注册 Serilog。</summary>
	/// <param name="builder">日志构建器。</param>
	/// <param name="options">接入配置。</param>
	/// <param name="configuration">
	/// 完整配置对象。当其中存在 <c>Serilog</c> 节时，输出目标改由配置文件决定，
	/// <paramref name="options"/> 里的 sink 属性不再生效（详见 <see cref="FrameworkSerilogOptions"/>）。
	/// </param>
	public static ILoggingBuilder AddFrameworkSerilog(this ILoggingBuilder builder,
		FrameworkSerilogOptions options, IConfiguration? configuration = null)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(options);
		options.Validate();

		// 幂等保护：重复注册会让每条日志输出多次，这是接入日志时最难发现的问题之一。
		if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(FrameworkSerilogRegistration)))
		{
			return builder;
		}

		builder.Services.AddSingleton<FrameworkSerilogRegistration>();

		var loggerConfiguration = new LoggerConfiguration();

		// 只有配置文件里真的写了 Serilog 节才走配置驱动，否则代码里的默认 sink 会与配置文件叠加。
		if (configuration?.GetSection(FrameworkSerilogOptions.SectionName).Exists() == true)
		{
			// 配置驱动时 sink 由文件决定，但公共字段与业务模板仍需保持一致。
			loggerConfiguration
				.Enrich.With(new CommonPropertiesEnricher(options))
				.ReadFrom.Configuration(configuration);
		}
		else
		{
			if (!options.ConsoleEnabled && !options.FileEnabled)
			{
				throw new InvalidOperationException(
					"ConsoleEnabled 与 FileEnabled 同时为 false，且未提供 Serilog 配置节，日志将无处输出。" +
					"如需挂载自定义 Sink（Seq / OpenTelemetry 等），请改用 LoggerConfiguration.UseFrameworkDefaults 自行拼装管道。");
			}

			loggerConfiguration.UseFrameworkDefaults(options);
		}

		builder.AddSerilog(loggerConfiguration.CreateLogger(), dispose: true);
		return builder;
	}

	/// <summary>
	/// 把本包的默认约定（公共字段、级别过滤、控制台 / 文件 / 异步目标）应用到任意
	/// <see cref="LoggerConfiguration"/>，供需要自行拼装 Serilog 管道的场景复用。
	/// </summary>
	public static LoggerConfiguration UseFrameworkDefaults(this LoggerConfiguration configuration,
		FrameworkSerilogOptions options)
	{
		ArgumentNullException.ThrowIfNull(configuration);
		ArgumentNullException.ThrowIfNull(options);
		options.Validate();

		configuration
			.Enrich.With(new CommonPropertiesEnricher(options))
			.Enrich.FromLogContext();

		configuration.MinimumLevel.Is(options.MinimumLevel);
		foreach (var (source, level) in options.LevelOverrides)
		{
			configuration.MinimumLevel.Override(source, level);
		}

		if (!options.ConsoleEnabled && !options.FileEnabled)
		{
			// 调用方通常已自行挂载 sink（Seq / OpenTelemetry 等），此处不再兜底输出。
			return configuration;
		}

		if (!options.AsyncWrite)
		{
			WriteSinks(configuration.WriteTo, options);
			return configuration;
		}

		// Async 包装：业务线程只入队，磁盘与终端 IO 交给后台线程。
		configuration.WriteTo.Async(
			configure => WriteSinks(configure, options),
			bufferSize: options.QueueCapacity,
			blockWhenFull: options.BlockWhenQueueFull);

		return configuration;
	}

	/// <summary>按配置挂载控制台与文件两个目标。</summary>
	static void WriteSinks(LoggerSinkConfiguration sinks, FrameworkSerilogOptions options)
	{
		if (options.ConsoleEnabled)
		{
			sinks.Console(
				restrictedToMinimumLevel: options.ConsoleMinimumLevel ?? options.MinimumLevel,
				outputTemplate: options.ConsoleOutputTemplate ?? ResolveConsoleTemplate(),
				theme: ResolveConsoleTheme());
		}

		if (!options.FileEnabled) return;

		var path = options.ResolveFilePath();
		var level = options.FileMinimumLevel ?? options.MinimumLevel;

		if (options.JsonFileOutput)
		{
			// CLEF：一行一条 JSON，供 Seq / Loki / Elasticsearch 按字段检索。
			sinks.File(
				path: path,
				restrictedToMinimumLevel: level,
				formatter: new CompactJsonFormatter(),
				rollingInterval: options.RollingInterval,
				rollOnFileSizeLimit: true,
				fileSizeLimitBytes: options.FileSizeLimitBytes,
				retainedFileCountLimit: options.RetainedFileCountLimit,
				buffered: options.BufferedFile,
				flushToDiskInterval: TimeSpan.FromSeconds(1));
			return;
		}

		sinks.File(
			path: path,
			restrictedToMinimumLevel: level,
			outputTemplate: FileOutputTemplate,
			rollingInterval: options.RollingInterval,
			rollOnFileSizeLimit: true,
			fileSizeLimitBytes: options.FileSizeLimitBytes,
			retainedFileCountLimit: options.RetainedFileCountLimit,
			buffered: options.BufferedFile,
			flushToDiskInterval: TimeSpan.FromSeconds(1));
	}

	/// <summary>文件用纯文本模板：带完整时间戳、应用名与追踪标识，便于 grep 与人工比对。</summary>
	internal const string FileOutputTemplate =
		"{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Application} [{TraceId}] {SourceContext} {Message:lj}{NewLine}{Exception}";

	/// <summary>交互式终端模板：省略应用名等固定字段，突出消息本身。</summary>
	internal const string InteractiveConsoleTemplate =
		"[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

	/// <summary>重定向输出（容器 / CI）模板：补全时间戳与追踪标识，去掉颜色。</summary>
	internal const string PlainConsoleTemplate =
		"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Application} [{TraceId}] {SourceContext} {Message:lj}{NewLine}{Exception}";

	static string ResolveConsoleTemplate()
		=> IsInteractiveConsole ? InteractiveConsoleTemplate : PlainConsoleTemplate;

	static ConsoleTheme ResolveConsoleTheme()
		=> IsInteractiveConsole ? AnsiConsoleTheme.Code : ConsoleTheme.None;

	/// <summary>输出被重定向（管道、容器日志、CI）时不应输出 ANSI 颜色码。</summary>
	internal static bool IsInteractiveConsole
	{
		get
		{
			try
			{
				return !Console.IsOutputRedirected;
			}
			catch (IOException)
			{
				// 没有控制台句柄的环境（Windows 服务、部分容器）直接按非交互处理。
				return false;
			}
		}
	}
}

/// <summary>注册标记，仅用于让 <c>AddFrameworkSerilog</c> 幂等。</summary>
sealed class FrameworkSerilogRegistration;
