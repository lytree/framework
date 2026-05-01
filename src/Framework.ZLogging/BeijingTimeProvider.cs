using System;
namespace Framework.ZLogging;

internal sealed class BeijingTimeProvider : TimeProvider
{
    // 懒加载单例（线程安全）
    public static BeijingTimeProvider Instance { get; } = new();

    private static readonly TimeZoneInfo ChinaTimeZone = GetChinaTimeZone();

    private BeijingTimeProvider() { }

    private static TimeZoneInfo GetChinaTimeZone()
    {
        try
        {
            // Windows
            return TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
        }
        catch
        {
            // Linux / macOS
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");
        }
    }

    // 保持 UTC 语义（不要改！）
    public override DateTimeOffset GetUtcNow()
    {
        return TimeProvider.System.GetUtcNow();
    }

    // 👉 北京时间（推荐用这个）
    public DateTimeOffset Now
    {
        get
        {
            var utc = GetUtcNow();
            return TimeZoneInfo.ConvertTime(utc, ChinaTimeZone);
        }
    }

    public DateTime NowDateTime => Now.DateTime;
}