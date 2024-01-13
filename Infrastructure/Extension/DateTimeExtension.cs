using Infrastructure.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Extension;

public static class DateTimeExtension
{
    public static long ToMilliseconds(this DateTime dateTime)
    {
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local));
        return (long)(utcDateTime - DateTimeHelper.TimestampStart).TotalMilliseconds;
    }

    /// <summary>
    /// 获取日期天的最小时间
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static DateTime GetDayMinDate(this DateTime dt)
    {
        DateTime min = new(dt.Year, dt.Month, dt.Day, 0, 0, 0);
        return min;
    }


    /// <summary>
    /// 获取日期天的最大时间
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static DateTime GetDayMaxDate(this DateTime dt)
    {
        DateTime max = new(dt.Year, dt.Month, dt.Day, 23, 59, 59);
        return max;
    }
}
