using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Repository.Data.Helper;

public class DateTimeHelper
{
	public static long ToMilliseconds(DateTime dateTime)
	{
		var chTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");
		var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(TimeZoneInfo.ConvertTime(dateTime, chTimeZone));
		return (long)(utcDateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
	}
	public static DateTime ToDateTime(long timeStamp)
	{
		DateTimeOffset utcDateTime = DateTimeOffset.FromUnixTimeMilliseconds(timeStamp);
		var chTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");

		return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime.UtcDateTime, chTimeZone); // 当地时区
	}
}
