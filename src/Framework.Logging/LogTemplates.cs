namespace Framework.Logging;

/// <summary>
/// 结构化日志的消息模板常量。
///
/// <para>
/// 这是本包的核心约定：模板中的每个 <c>{Name}</c> 占位符都会成为日志事件上的独立属性，
/// 因此占位符名必须取自 <see cref="LogProperties"/>，而不是随手拼写。
/// 模板里出现未登记的字段名会让日志平台无法聚合，单元测试会对此做静态校验。
/// </para>
///
/// <para><b>使用方式</b></para>
/// <code>
/// logger.LogInformation(LogTemplates.RequestCompleted, method, path, statusCode, elapsed);
/// // 或直接用强类型扩展：logger.LogRequestCompleted(method, path, statusCode, elapsed);
/// </code>
///
/// <para><b>编写规范</b></para>
/// <list type="bullet">
/// <item>模板是常量字符串，绝不能用字符串插值（<c>$"{x}"</c>）——那会立即求值，属性丢失。</item>
/// <item>占位符数量必须与实参数量严格一致，否则 Serilog 会把参数错位。</item>
/// <item>浮点测量值统一带 <c>:0.####</c> 格式，避免日志里出现 <c>12.300000000000001</c>。</item>
/// <item>消息正文描述「发生了什么」，字段承载「可用于查询的维度」，两者不要重复。</item>
/// </list>
/// </summary>
public static class LogTemplates
{
	#region 请求生命周期

	/// <summary>请求进入。<c>{Method} {Path}</c></summary>
	public const string RequestStarted = "开始处理 {Method} {Path}";

	/// <summary>请求正常结束。<c>{Method} {Path} {StatusCode} {Elapsed}</c></summary>
	public const string RequestCompleted = "请求完成 {Method} {Path} → {StatusCode}，耗时 {Elapsed:0.####} ms";

	/// <summary>请求以失败状态结束。<c>{Method} {Path} {StatusCode} {Elapsed}</c></summary>
	public const string RequestFailed = "请求失败 {Method} {Path} → {StatusCode}，耗时 {Elapsed:0.####} ms";

	/// <summary>请求耗时超过阈值。<c>{Method} {Path} {Elapsed}</c></summary>
	public const string RequestSlow = "慢请求 {Method} {Path}，耗时 {Elapsed:0.####} ms";

	/// <summary>客户端提前断开。<c>{Method} {Path} {Elapsed}</c></summary>
	public const string RequestAborted = "请求被取消 {Method} {Path}，耗时 {Elapsed:0.####} ms";

	#endregion

	#region 数据访问

	/// <summary>即将执行数据库命令。<c>{Command}</c></summary>
	public const string DatabaseCommandExecuting = "执行数据库命令 {Command}";

	/// <summary>数据库命令完成。<c>{Command} {Rows} {Elapsed}</c></summary>
	public const string DatabaseCommandCompleted = "数据库命令完成 {Command}，影响 {Rows} 行，耗时 {Elapsed:0.####} ms";

	/// <summary>数据库命令失败。<c>{Command}</c></summary>
	public const string DatabaseCommandFailed = "数据库命令执行失败 {Command}";

	/// <summary>慢查询。<c>{Command} {Elapsed}</c></summary>
	public const string DatabaseSlowCommand = "慢查询 {Command}，耗时 {Elapsed:0.####} ms";

	/// <summary>事务回滚。<c>{Database}</c></summary>
	public const string TransactionRolledBack = "事务已回滚，连接 {Database}";

	#endregion

	#region 缓存

	/// <summary>缓存命中。<c>{CacheKey}</c></summary>
	public const string CacheHit = "缓存命中 {CacheKey}";

	/// <summary>缓存未命中。<c>{CacheKey}</c></summary>
	public const string CacheMiss = "缓存未命中 {CacheKey}";

	/// <summary>缓存写入。<c>{CacheKey} {Elapsed}</c></summary>
	public const string CacheWritten = "缓存写入 {CacheKey}，耗时 {Elapsed:0.####} ms";

	/// <summary>缓存失效。<c>{CacheKey}</c></summary>
	public const string CacheEvicted = "缓存失效 {CacheKey}";

	#endregion

	#region 外部调用

	/// <summary>发起外部调用。<c>{Target}</c></summary>
	public const string OutboundCallStarted = "调用外部服务 {Target}";

	/// <summary>外部调用返回。<c>{Target} {StatusCode} {Elapsed}</c></summary>
	public const string OutboundCallCompleted = "外部服务 {Target} 返回 {StatusCode}，耗时 {Elapsed:0.####} ms";

	/// <summary>外部调用失败。<c>{Target} {StatusCode} {Elapsed}</c></summary>
	public const string OutboundCallFailed = "外部服务 {Target} 调用失败 → {StatusCode}，耗时 {Elapsed:0.####} ms";

	/// <summary>外部调用准备重试。<c>{Target} {RetryCount} {MaxRetry}</c></summary>
	public const string OutboundCallRetrying = "外部服务 {Target} 第 {RetryCount}/{MaxRetry} 次重试";

	#endregion

	#region 后台任务与消息

	/// <summary>作业开始。<c>{JobName}</c></summary>
	public const string JobStarted = "作业 {JobName} 开始执行";

	/// <summary>作业完成。<c>{JobName} {Elapsed}</c></summary>
	public const string JobCompleted = "作业 {JobName} 执行完成，耗时 {Elapsed:0.####} ms";

	/// <summary>作业失败。<c>{JobName} {Elapsed}</c></summary>
	public const string JobFailed = "作业 {JobName} 执行失败，耗时 {Elapsed:0.####} ms";

	/// <summary>收到消息。<c>{QueueName} {MessageId}</c></summary>
	public const string MessageConsumed = "从 {QueueName} 消费消息 {MessageId}";

	/// <summary>消息处理完成。<c>{MessageId} {Elapsed}</c></summary>
	public const string MessageHandled = "消息 {MessageId} 处理完成，耗时 {Elapsed:0.####} ms";

	#endregion

	#region 认证与授权

	/// <summary>认证成功。<c>{UserId}</c></summary>
	public const string AuthenticationSucceeded = "用户 {UserId} 认证成功";

	/// <summary>认证失败。<c>{UserId} {ErrorCode}</c></summary>
	public const string AuthenticationFailed = "用户 {UserId} 认证失败，原因 {ErrorCode}";

	/// <summary>授权拒绝。<c>{UserId} {Path}</c></summary>
	public const string AuthorizationDenied = "用户 {UserId} 无权访问 {Path}";

	#endregion

	#region 业务操作

	/// <summary>业务操作开始。<c>{Operation}</c></summary>
	public const string OperationStarted = "开始 {Operation}";

	/// <summary>业务操作完成。<c>{Operation} {Elapsed}</c></summary>
	public const string OperationCompleted = "{Operation} 完成，耗时 {Elapsed:0.####} ms";

	/// <summary>业务操作失败。<c>{Operation} {ErrorCode}</c></summary>
	public const string OperationFailed = "{Operation} 失败，错误码 {ErrorCode}";

	/// <summary>实体变更。<c>{Operation} {EntityType} {EntityId}</c></summary>
	public const string EntityChanged = "{Operation} {EntityType}/{EntityId}";

	/// <summary>参数校验失败。<c>{Operation} {ErrorCode}</c></summary>
	public const string ValidationFailed = "{Operation} 参数校验失败，字段 {ErrorCode}";

	#endregion

	#region 应用生命周期

	/// <summary>应用启动。<c>{Application} {ApplicationVersion}</c></summary>
	public const string ApplicationStarting = "应用 {Application} {ApplicationVersion} 启动中";

	/// <summary>应用停止。<c>{Application}</c></summary>
	public const string ApplicationStopping = "应用 {Application} 正在停止";

	#endregion
}
