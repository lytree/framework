using Serilog;
using Serilog.Events;
using System.Reflection;

namespace Framework.Logging;

/// <summary>
/// Serilog 接入配置。可代码配置，也可从 <c>appsettings.json</c> 的 <c>Serilog</c> 节绑定。
///
/// <para>
/// 两者的关系是「配置优先」：只要 <c>IConfiguration</c> 中存在 <c>Serilog</c> 节，
/// 输出目标（WriteTo）就完全交给配置文件，本类的 sink 相关属性不再生效，
/// 避免代码与配置文件各配一份导致同一条日志被打印两次。
/// 与输出无关的项（应用名、版本、公共字段）仍然生效。
/// </para>
/// </summary>
public sealed class FrameworkSerilogOptions
{
	/// <summary>约定读取的配置节名。</summary>
	public const string SectionName = "Serilog";

	/// <summary>应用名，作为 <see cref="LogProperties.Application"/> 注入每条日志。默认取入口程序集名。</summary>
	public string Application { get; set; } = ResolveEntryAssemblyName();

	/// <summary>应用版本，作为 <see cref="LogProperties.ApplicationVersion"/> 注入。默认取入口程序集的 <c>InformationalVersion</c>。</summary>
	public string? ApplicationVersion { get; set; } = ResolveEntryAssemblyVersion();

	/// <summary>运行环境名。默认依次读 <c>DOTNET_ENVIRONMENT</c> / <c>ASPNETCORE_ENVIRONMENT</c>，都没有则为 <c>Production</c>。</summary>
	public string EnvironmentName { get; set; } = ResolveEnvironmentName();

	/// <summary>全局最低日志级别。</summary>
	public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;

	/// <summary>是否启用 <c>BeginScope</c> 作用域属性。关闭后作用域内的字段不会进入日志事件。</summary>
	public bool IncludeScopes { get; set; } = true;

	/// <summary>命名空间级别的级别覆盖，例如把 <c>Microsoft</c> 压到 Warning 以减少框架噪音。</summary>
	public IDictionary<string, LogEventLevel> LevelOverrides { get; } = new Dictionary<string, LogEventLevel>(StringComparer.Ordinal)
	{
		["Microsoft"] = LogEventLevel.Warning,
		["System.Net.Http.HttpClient"] = LogEventLevel.Warning
	};

	#region 控制台

	/// <summary>是否输出到控制台。生产环境跑在容器里时建议保留，由采集端统一收走。</summary>
	public bool ConsoleEnabled { get; set; } = true;

	/// <summary>控制台最低级别；为 <c>null</c> 时继承 <see cref="MinimumLevel"/>。</summary>
	public LogEventLevel? ConsoleMinimumLevel { get; set; }

	/// <summary>控制台模板；为 <c>null</c> 时用开发友好模板，非交互式输出时用纯文本模板。</summary>
	public string? ConsoleOutputTemplate { get; set; }

	#endregion

	#region 文件

	/// <summary>是否写文件。</summary>
	public bool FileEnabled { get; set; }

	/// <summary>
	/// 文件路径模板，交给 Serilog.Sinks.File 解析：
	/// 路径中的 <c>-</c> 由滚动周期替换（按天滚动时 <c>logs/app-.log</c> → <c>logs/app-20261031.log</c>）。
	/// 为 <c>null</c> 时使用 <c>logs/{Application}-.log</c>。相对路径基于 <see cref="AppContext.BaseDirectory"/>。
	/// </summary>
	public string? FilePath { get; set; }

	/// <summary>文件最低级别；为 <c>null</c> 时继承 <see cref="MinimumLevel"/>。</summary>
	public LogEventLevel? FileMinimumLevel { get; set; }

	/// <summary>
	/// 文件是否输出 CLEF（Compact Log Event Format）JSON。
	/// 开启后一行一条 JSON，可被 Seq / Loki / Elasticsearch 直接按字段解析；
	/// 关闭则输出人类可读的纯文本。
	/// </summary>
	public bool JsonFileOutput { get; set; }

	/// <summary>滚动周期。</summary>
	public RollingInterval RollingInterval { get; set; } = RollingInterval.Day;

	/// <summary>单文件体积上限（字节），超过后立即滚动。默认 50 MB。</summary>
	public long FileSizeLimitBytes { get; set; } = 50L * 1024 * 1024;

	/// <summary>保留的历史文件数量上限。默认 31 份，约一个月。</summary>
	public int RetainedFileCountLimit { get; set; } = 31;

	/// <summary>是否写入磁盘时先缓冲再批量落盘，显著降低 IO 次数。</summary>
	public bool BufferedFile { get; set; } = true;

	#endregion

	#region 异步

	/// <summary>
	/// 是否用 <c>Serilog.Sinks.Async</c> 包裹全部输出目标。
	/// 开启后业务线程不等待磁盘 / 控制台 IO；队列满时的行为由 <see cref="BlockWhenQueueFull"/> 决定。
	/// </summary>
	public bool AsyncWrite { get; set; } = true;

	/// <summary>异步队列容量。</summary>
	public int QueueCapacity { get; set; } = 10000;

	/// <summary>
	/// 队列满时是否阻塞生产者。
	/// <c>false</c>（默认）丢弃新日志保业务吞吐，<c>true</c> 保证不丢日志但可能拖慢业务。
	/// </summary>
	public bool BlockWhenQueueFull { get; set; }

	#endregion

	/// <summary>解析实际生效的文件路径模板。</summary>
	internal string ResolveFilePath()
	{
		var path = string.IsNullOrWhiteSpace(FilePath)
			? Path.Combine("logs", $"{SanitizeFileName(Application)}-.log")
			: FilePath!;

		return Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
	}

	/// <summary>启动期校验，尽早暴露配置错误而不是等到第一条日志写不出去。</summary>
	internal void Validate()
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(Application, nameof(Application));

		if (QueueCapacity < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(QueueCapacity), "QueueCapacity 必须大于 0。");
		}

		if (FileEnabled && FileSizeLimitBytes <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(FileSizeLimitBytes), "开启文件输出时 FileSizeLimitBytes 必须大于 0。");
		}

		if (FileEnabled && RetainedFileCountLimit < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(RetainedFileCountLimit), "开启文件输出时 RetainedFileCountLimit 必须大于 0。");
		}
	}

	/// <summary>去掉文件名里不能出现的字符，避免应用名里带路径分隔符时写文件失败。</summary>
	static string SanitizeFileName(string name)
	{
		var invalid = Path.GetInvalidFileNameChars();
		var builder = new System.Text.StringBuilder(name.Length);
		foreach (var character in name)
		{
			builder.Append(Array.IndexOf(invalid, character) >= 0 || character is ' ' ? '_' : character);
		}
		return builder.Length == 0 ? "app" : builder.ToString();
	}

	static string ResolveEntryAssemblyName()
		=> Assembly.GetEntryAssembly()?.GetName().Name ?? AppDomain.CurrentDomain.FriendlyName;

	static string? ResolveEntryAssemblyVersion()
		=> Assembly.GetEntryAssembly()?
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
			.InformationalVersion;

	static string ResolveEnvironmentName()
		=> Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
		   ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
		   ?? "Production";
}
