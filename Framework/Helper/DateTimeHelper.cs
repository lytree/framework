using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Framework.Helper;
/// <summary>
/// 时间工具类
/// </summary>
public static class DateTimeHelper
{
    /// <summary>
    /// 时间戳起始日期
    /// </summary>
    public static readonly DateTime TimestampStart = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// 时间戳转换为DateTime 时区+8（Asia/Shanghai）
    /// </summary>
    /// <param name="timeStamp"></param>
    /// <returns></returns>
    public static DateTime ToDateTime(long timeStamp)
    {
        DateTimeOffset utcDateTime = DateTimeOffset.FromUnixTimeMilliseconds(timeStamp);
        var chTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");

        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime.UtcDateTime, chTimeZone); // 当地时区
    }
    /// <summary>
    /// 获取周几
    /// </summary>
    /// <param name="datetime"></param>
    /// <returns></returns>
    public static string GetWeekName(DateTime datetime)
    {
        var day = (int)datetime.DayOfWeek;
        var week = new string[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
        return week[day];
    }
    /// <summary>
    /// 等间隔拆分两个时段
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="type">拆分间隔</param>
    /// <returns></returns>
    public static Dictionary<DateTime, TimeRange> GetDateTimeRanges(DateTime startTime, DateTime endTime, TimeRangeType type)
    {
        Dictionary<DateTime, TimeRange> timeDic = new();
        switch (type)
        {
            case TimeRangeType.Day://天分割
                TimeRange timeRange = new();
                timeRange.StartTime = startTime;
                var nextTime = startTime.AddDays(1 - startTime.Day).AddDays(1).AddHours(-startTime.Hour).AddMinutes(-startTime.Minute).AddSeconds(-startTime.Second);
                timeRange.EndTime = nextTime;
                timeDic.Add(startTime, timeRange);
                while (DateTime.Compare(startTime, endTime) <= 0)
                {
                    TimeRange timeRange0 = new();
                    var tmpTime = nextTime;
                    timeRange0.StartTime = nextTime;
                    nextTime = nextTime.AddDays(1).AddHours(-startTime.Hour).AddMinutes(-startTime.Minute).AddSeconds(-startTime.Second);
                    timeRange0.EndTime = nextTime;
                    timeDic.Add(tmpTime, timeRange0);
                    startTime = nextTime;
                }
                break;
            case TimeRangeType.Month://月分割
                TimeRange timeRange2 = new();
                timeRange2.StartTime = startTime;
                var nextTime2 = startTime.AddDays(1 - startTime.Day).AddMonths(1).AddHours(-startTime.Hour).AddMinutes(-startTime.Minute).AddSeconds(-startTime.Second);
                timeRange2.EndTime = nextTime2;
                timeDic.Add(startTime, timeRange2);
                while (DateTime.Compare(startTime, endTime) <= 0)
                {
                    TimeRange timeRange0 = new();
                    var tmpTime = nextTime2;
                    timeRange0.StartTime = nextTime2;
                    nextTime2 = nextTime2.AddDays(1 - startTime.Day).AddMonths(1).AddHours(-startTime.Hour).AddMinutes(-startTime.Minute).AddSeconds(-startTime.Second);
                    timeRange0.EndTime = nextTime2;
                    timeDic.Add(tmpTime, timeRange0);
                    startTime = nextTime2;
                }
                break;
            case TimeRangeType.Quarter://季度分割
                TimeRange timeRange1 = new();
                timeRange1.StartTime = startTime;
                var nextTime1 = startTime.AddDays(1 - startTime.Day).AddMonths(3).AddHours(-startTime.Hour).AddMinutes(-startTime.Minute).AddSeconds(-startTime.Second);
                timeRange1.EndTime = nextTime1;
                timeDic.Add(startTime, timeRange1);
                while (DateTime.Compare(startTime, endTime) < 0)
                {
                    TimeRange timeRange0 = new();
                    var tmpTime = nextTime1;
                    timeRange0.StartTime = nextTime1;
                    nextTime1 = nextTime1.AddDays(1 - startTime.Day).AddMonths(3).AddHours(-startTime.Hour).AddMinutes(-startTime.Minute).AddSeconds(-startTime.Second);
                    timeRange0.EndTime = nextTime1;
                    timeDic.Add(tmpTime, timeRange0);
                    startTime = nextTime1;
                }
                break;


        }
        return timeDic;
    }
    /// <summary>
    /// 时间段
    /// </summary>
    public class TimeRange
    {
        /// <summary>
        /// 时间段开始时间
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 时间段结束时间
        /// </summary>
        public DateTime EndTime { get; set; }
    }
    /// <summary>
    /// 拆分间隔
    /// </summary>

    public enum TimeRangeType
    {
        /// <summary>
        /// 分钟
        /// </summary>
        Minutes= 0,
        /// <summary>
        /// 小时
        /// </summary>
        Hour = 1,
        /// <summary>
        /// 天
        /// </summary>
        Day = 2,
        /// <summary>
        /// 月
        /// </summary>
        Month = 3,
        /// <summary>
        /// 季
        /// </summary>
        Quarter = 4
    }
}
