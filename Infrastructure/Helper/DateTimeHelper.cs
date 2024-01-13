using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Infrastructure.Helper;

public static class DateTimeHelper
{
	/// <summary>
	/// 时间戳起始日期
	/// </summary>
	public static readonly DateTime TimestampStart = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

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
	public static Dictionary<DateTime, TimeRange> GetDateTimeRanges(DateTime startTime, DateTime endTime, int type)
	{
		Dictionary<DateTime, TimeRange> timeDic = new();
		switch (type)
		{
			case 0://月分割
				TimeRange timeRange = new();
				timeRange.StartTime = startTime;
				var nextTime = startTime.AddDays(1 - startTime.Day).AddMonths(1).AddHours(-startTime.Hour).AddMinutes(-startTime.Minute).AddSeconds(-startTime.Second);
				timeRange.EndTime = nextTime;
				timeDic.Add(startTime, timeRange);
				while (DateTime.Compare(startTime, endTime) <= 0)
				{
					TimeRange timeRange0 = new();
					var tmpTime = nextTime;
					timeRange0.StartTime = nextTime;
					nextTime = nextTime.AddDays(1 - startTime.Day).AddMonths(1).AddHours(-startTime.Hour).AddMinutes(-startTime.Minute).AddSeconds(-startTime.Second);
					timeRange0.EndTime = nextTime;
					timeDic.Add(tmpTime, timeRange0);
					startTime = nextTime;
				}
				break;
			case 1://季度分割
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
	public class TimeRange
	{
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
	}
}
