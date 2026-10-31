namespace Framework.Logging;

/// <summary>
/// 结构化日志字段名的唯一来源。
///
/// <para>
/// Serilog 的消息模板会把 <c>{Name}</c> 占位符变成日志事件上的**属性名**，
/// 属性名一旦在不同调用点漂移（<c>UserId</c> / <c>user_id</c> / <c>Uid</c>），
/// 集中式日志平台就无法按同一字段做聚合与告警。
/// 因此本包内所有模板占位符都必须取自这里的常量，模板一致性由单元测试守护。
/// </para>
/// </summary>
public static class LogProperties
{
	#region 公共上下文（由 Enricher 注入，业务代码无需重复传参）

	/// <summary>应用名，来自 <see cref="FrameworkSerilogOptions.Application"/>。</summary>
	public const string Application = "Application";

	/// <summary>应用版本号。</summary>
	public const string ApplicationVersion = "ApplicationVersion";

	/// <summary>运行环境（Development / Staging / Production）。</summary>
	public const string EnvironmentName = "EnvironmentName";

	/// <summary>机器名，多实例部署时用于区分节点。</summary>
	public const string MachineName = "MachineName";

	/// <summary>W3C TraceId，与 OpenTelemetry / HttpClient 的链路追踪对齐。</summary>
	public const string TraceId = "TraceId";

	/// <summary>SpanId，缺省时与 <see cref="TraceId"/> 同源。</summary>
	public const string SpanId = "SpanId";

	#endregion

	#region 请求 / 响应

	/// <summary>HTTP 方法或 RPC 方法名。</summary>
	public const string Method = "Method";

	/// <summary>请求路径（不含查询串，避免把敏感参数写进日志）。</summary>
	public const string Path = "Path";

	/// <summary>响应状态码。</summary>
	public const string StatusCode = "StatusCode";

	/// <summary>耗时（毫秒）。</summary>
	public const string Elapsed = "Elapsed";

	/// <summary>客户端 IP。</summary>
	public const string ClientIp = "ClientIp";

	/// <summary>请求追踪标识，便于与前端 / 网关的 requestId 对齐。</summary>
	public const string RequestId = "RequestId";

	#endregion

	#region 业务 / 身份

	/// <summary>业务操作名（动词 + 名词，如 <c>CreateOrder</c>）。</summary>
	public const string Operation = "Operation";

	/// <summary>用户标识。</summary>
	public const string UserId = "UserId";

	/// <summary>租户标识，多租户系统必填。</summary>
	public const string TenantId = "TenantId";

	/// <summary>实体类型名（如 <c>Order</c>）。</summary>
	public const string EntityType = "EntityType";

	/// <summary>实体主键。</summary>
	public const string EntityId = "EntityId";

	/// <summary>业务错误码，用于告警规则命中。</summary>
	public const string ErrorCode = "ErrorCode";

	/// <summary>重试次数，从 1 开始。</summary>
	public const string RetryCount = "RetryCount";

	/// <summary>重试上限。</summary>
	public const string MaxRetry = "MaxRetry";

	#endregion

	#region 数据访问

	/// <summary>SQL 语句或命令文本。</summary>
	public const string Command = "Command";

	/// <summary>目标表名。</summary>
	public const string Table = "Table";

	/// <summary>影响行数。</summary>
	public const string Rows = "Rows";

	/// <summary>数据库名。</summary>
	public const string Database = "Database";

	#endregion

	#region 缓存 / 消息 / 后台任务

	/// <summary>缓存键。</summary>
	public const string CacheKey = "CacheKey";

	/// <summary>作业名。</summary>
	public const string JobName = "JobName";

	/// <summary>消息队列名。</summary>
	public const string QueueName = "QueueName";

	/// <summary>消息标识。</summary>
	public const string MessageId = "MessageId";

	/// <summary>外部服务名或下游地址。</summary>
	public const string Target = "Target";

	#endregion
}
