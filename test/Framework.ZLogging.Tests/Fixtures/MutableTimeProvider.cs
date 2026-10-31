namespace Framework.ZLogging.Tests.Fixtures;

/// <summary>
/// 可控时间源：用于验证跨零点换文件这类与真实时钟强相关的行为。
/// 固定 <see cref="LocalTimeZone"/> 为 UTC，让本地日期在测试里完全确定。
/// </summary>
public sealed class MutableTimeProvider : TimeProvider
{
    DateTimeOffset now = new(2026, 3, 1, 23, 59, 0, TimeSpan.Zero);

    /// <summary>当前模拟时刻（UTC）。</summary>
    public DateTimeOffset Now => now;

    public override DateTimeOffset GetUtcNow() => now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    /// <summary>把模拟时钟向前推进。</summary>
    public void Advance(TimeSpan delta) => now = now.Add(delta);
}
