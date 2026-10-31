using Microsoft.Extensions.Logging;

namespace Framework.Logging;

/// <summary>
/// 强类型结构化日志方法。
///
/// <para>
/// 直接写 <c>logger.LogInformation(LogTemplates.RequestCompleted, ...)</c> 有个经典陷阱：
/// 占位符与实参数量错位不会编译报错，Serilog 会把参数塞到错误的属性上，
/// 日志看起来正常、查询结果却是错的。这里把高频场景包装成方法，
/// 由编译器保证参数顺序与数量，调用点也不必再背模板文本。
/// </para>
/// </summary>
public static class StructuredLoggingExtensions
{
	#region 请求

	/// <summary>记录请求正常完成。<paramref name="statusCode"/> 为 5xx 时按 Error 记录，其余按 Information。</summary>
	public static void LogRequestCompleted(this ILogger logger,
		string method, string path, int statusCode, double elapsedMs)
	{
		ArgumentNullException.ThrowIfNull(logger);
		logger.Log(
			statusCode >= 500 ? LogLevel.Error : LogLevel.Information,
			LogTemplates.RequestCompleted, method, path, statusCode, elapsedMs);
	}

	/// <summary>记录请求异常结束。4xx 按 Warning、5xx 按 Error 记录。</summary>
	public static void LogRequestFailed(this ILogger logger,
		string method, string path, int statusCode, double elapsedMs, Exception? exception = null)
	{
		ArgumentNullException.ThrowIfNull(logger);
		logger.Log(
			statusCode >= 500 ? LogLevel.Error : LogLevel.Warning,
			exception, LogTemplates.RequestFailed, method, path, statusCode, elapsedMs);
	}

	/// <summary>记录超过阈值的慢请求。</summary>
	public static void LogRequestSlow(this ILogger logger,
		string method, string path, double elapsedMs, Exception? exception = null)
	{
		ArgumentNullException.ThrowIfNull(logger);
		logger.Log(LogLevel.Warning, exception, LogTemplates.RequestSlow, method, path, elapsedMs);
	}

	#endregion

	#region 数据访问

	/// <summary>记录数据库命令执行结果。默认 Debug 级别，避免生产环境被 SQL 淹没。</summary>
	public static void LogDatabaseCommandCompleted(this ILogger logger,
		string command, int rows, double elapsedMs, LogLevel level = LogLevel.Debug)
	{
		ArgumentNullException.ThrowIfNull(logger);
		logger.Log(level, LogTemplates.DatabaseCommandCompleted, command, rows, elapsedMs);
	}

	/// <summary>记录慢查询。</summary>
	public static void LogDatabaseSlowCommand(this ILogger logger,
		string command, double elapsedMs, Exception? exception = null)
	{
		ArgumentNullException.ThrowIfNull(logger);
		logger.Log(LogLevel.Warning, exception, LogTemplates.DatabaseSlowCommand, command, elapsedMs);
	}

	#endregion

	#region 缓存

	/// <summary>记录缓存命中。</summary>
	public static void LogCacheHit(this ILogger logger, string cacheKey)
	{
		ArgumentNullException.ThrowIfNull(logger);
		logger.Log(LogLevel.Debug, LogTemplates.CacheHit, cacheKey);
	}

	/// <summary>记录缓存未命中。</summary>
	public static void LogCacheMiss(this ILogger logger, string cacheKey)
	{
		ArgumentNullException.ThrowIfNull(logger);
		logger.Log(LogLevel.Debug, LogTemplates.CacheMiss, cacheKey);
	}

	#endregion

	#region 外部调用

	/// <summary>记录外部调用返回。</summary>
	public static void LogOutboundCallCompleted(this ILogger logger,
		string target, int statusCode, double elapsedMs, LogLevel level = LogLevel.Debug)
	{
		ArgumentNullException.ThrowIfNull(logger);
		logger.Log(level, LogTemplates.OutboundCallCompleted, target, statusCode, elapsedMs);
	}

	/// <summary>记录外部调用失败。</summary>
	public static void LogOutboundCallFailed(this ILogger logger,
		string target, int statusCode, double elapsedMs, Exception? exception = null)
	{
		ArgumentNullException.ThrowIfNull(logger);
		logger.Log(LogLevel.Warning, exception, LogTemplates.OutboundCallFailed, target, statusCode, elapsedMs);
	}

	/// <summary>记录外部调用重试。</summary>
	public static void LogOutboundCallRetrying(this ILogger logger,
		string target, int retryCount, int maxRetry, Exception? exception = null)
	{
		ArgumentNullException.ThrowIfNull(logger);
		logger.Log(LogLevel.Warning, exception, LogTemplates.OutboundCallRetrying, target, retryCount, maxRetry);
	}

	#endregion

	#region 后台任务

	/// <summary>记录作业完成。</summary>
	public static void LogJobCompleted(this ILogger logger, string jobName, double elapsedMs)
	{
		ArgumentNullException.ThrowIfNull(logger);
		logger.Log(LogLevel.Information, LogTemplates.JobCompleted, jobName, elapsedMs);
	}

	/// <summary>记录作业失败。</summary>
	public static void LogJobFailed(this ILogger logger, string jobName, double elapsedMs, Exception? exception = null)
	{
		ArgumentNullException.ThrowIfNull(logger);
		logger.Log(LogLevel.Error, exception, LogTemplates.JobFailed, jobName, elapsedMs);
	}

	#endregion

	#region 业务操作

	/// <summary>记录业务操作完成。</summary>
	public static void LogOperationCompleted(this ILogger logger, string operation, double elapsedMs)
	{
		ArgumentNullException.ThrowIfNull(logger);
		logger.Log(LogLevel.Information, LogTemplates.OperationCompleted, operation, elapsedMs);
	}

	/// <summary>记录业务操作失败。</summary>
	public static void LogOperationFailed(this ILogger logger, string operation, string errorCode, Exception? exception = null)
	{
		ArgumentNullException.ThrowIfNull(logger);
		logger.Log(LogLevel.Error, exception, LogTemplates.OperationFailed, operation, errorCode);
	}

	#endregion

	/// <summary>
	/// 开启一个带业务维度的作用域：作用域内的每一条日志都会自动携带
	/// <see cref="LogProperties.Operation"/> / <see cref="LogProperties.UserId"/> / <see cref="LogProperties.TenantId"/>，
	/// 无需在每次调用时重复传参。
	///
	/// <para>
	/// 必须用 <c>using</c> 包裹，否则维度会泄漏到后续无关日志上。
	/// 日志级别高于 <see cref="LogLevel.None"/> 时才创建作用域，避免无谓的状态推入。
	/// </para>
	/// </summary>
	/// <param name="logger">目标记录器。</param>
	/// <param name="operation">业务操作名。</param>
	/// <param name="userId">用户标识，可为 <c>null</c>。</param>
	/// <param name="tenantId">租户标识，可为 <c>null</c>。</param>
	public static IDisposable? BeginOperationScope(this ILogger logger,
		string operation, string? userId = null, string? tenantId = null)
	{
		ArgumentNullException.ThrowIfNull(logger);

		var properties = new Dictionary<string, object?>(3)
		{
			[LogProperties.Operation] = operation
		};
		if (userId is not null) properties[LogProperties.UserId] = userId;
		if (tenantId is not null) properties[LogProperties.TenantId] = tenantId;

		return logger.BeginScope(properties);
	}
}
