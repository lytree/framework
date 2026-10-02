using Framework;
using System.Text;

namespace System;

/// <summary>
/// <see cref="DateTime"/> 专用扩展。命名上为单数 <c>Extension</c>（与其它
/// <c>Extensions</c> 部分类区分），通过扩展语法访问，对调用方无影响。
/// 依赖：<c>Helper.TimestampStart</c>（来自 <c>Helpers/Helper.DateTime.cs</c>）。
/// </summary>
public static partial class Extension
{
    /// <summary>
    /// 获取该时间相对于 1970-01-01T00:00:00Z 的毫秒数
    /// </summary>
    public static long ToMilliseconds(this in DateTime dt)
    {
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(TimeZoneInfo.ConvertTime(dt, TimeZoneInfo.Local));
        return Convert.ToInt64((utcDateTime - Helper.TimestampStart).TotalMilliseconds);
    }

    /// <summary>
    /// 获取该时间相对于 1970-01-01T00:00:00Z 的秒数
    /// </summary>
    public static long ToSeconds(this in DateTime dt)
    {
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(TimeZoneInfo.ConvertTime(dt, TimeZoneInfo.Local));
        return Convert.ToInt64((utcDateTime - Helper.TimestampStart).TotalSeconds);
    }

    /// <summary>
    /// 获取该时间相对于 1970-01-01T00:00:00Z 的微秒时间戳
    /// </summary>
    public static long ToMicroseconds(this in DateTime dt)
    {
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(TimeZoneInfo.ConvertTime(dt, TimeZoneInfo.Local));
        return (utcDateTime - Helper.TimestampStart).Ticks / 10;
    }

    /// <summary>
    /// 判断时间是否在区间内
    /// </summary>
    public static bool In(this in DateTime @this, DateTime start, DateTime end, RangeMode mode = RangeMode.Close)
    {
        return mode switch
        {
            RangeMode.Open => start < @this && end > @this,
            RangeMode.Close => start <= @this && end >= @this,
            RangeMode.OpenClose => start < @this && end >= @this,
            RangeMode.CloseOpen => start <= @this && end > @this,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    /// <summary>获取日期天的最小时间</summary>
    public static DateTime GetDayMinDate(this DateTime dt) => new(dt.Year, dt.Month, dt.Day, 0, 0, 0);

    /// <summary>获取日期天的最大时间</summary>
    public static DateTime GetDayMaxDate(this DateTime dt) => new(dt.Year, dt.Month, dt.Day, 23, 59, 59);

    /// <summary>区间模式</summary>
    public enum RangeMode
    {
        Open,
        Close,
        OpenClose,
        CloseOpen
    }
}
