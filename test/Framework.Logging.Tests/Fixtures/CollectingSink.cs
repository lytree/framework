using Serilog.Core;
using Serilog.Events;

namespace Framework.Logging.Tests.Fixtures;

/// <summary>
/// 把写入的日志事件收集到内存，供断言使用。
/// Serilog 的 sink 可能被异步包装，因此读取端必须加锁。
/// </summary>
public sealed class CollectingSink : ILogEventSink
{
	readonly List<LogEvent> events = [];

	/// <summary>当前已收集事件的快照。</summary>
	public IReadOnlyList<LogEvent> Events
	{
		get { lock (events) return events.ToArray(); }
	}

	/// <summary>最后一条事件；没有事件时为 <c>null</c>。</summary>
	public LogEvent? Last
	{
		get { lock (events) return events.Count == 0 ? null : events[^1]; }
	}

	public void Emit(LogEvent logEvent)
	{
		ArgumentNullException.ThrowIfNull(logEvent);
		lock (events) events.Add(logEvent);
	}
}
