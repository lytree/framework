using Framework.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.System;
/// <summary>
/// 
/// </summary>
public static class DateTimeExtension
{
	/// <summary>
	/// 获取该时间相对于1970-01-01T00:00:00Z的毫秒数
	/// </summary>
	/// <param name="dt"></param>
	/// <returns></returns>
	public static long ToMilliseconds(this in DateTime dt)
	{
		var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(TimeZoneInfo.ConvertTime(dt, TimeZoneInfo.Local));
		return Convert.ToInt64((utcDateTime - DateTimeHelper.TimestampStart).TotalMilliseconds);
	}
	/// <summary>
	/// 获取该时间相对于1970-01-01T00:00:00Z的秒数
	/// </summary>
	/// <param name="dt"></param>
	/// <returns></returns>
	public static long ToSeconds(this in DateTime dt)
	{
		var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(TimeZoneInfo.ConvertTime(dt, TimeZoneInfo.Local));
		return Convert.ToInt64((utcDateTime - DateTimeHelper.TimestampStart).TotalSeconds);
	}

	/// <summary>
	/// 获取该时间相对于1970-01-01T00:00:00Z的微秒时间戳
	/// </summary>
	/// <param name="dt"></param>
	/// <returns></returns>
	public static long ToMicroseconds(this in DateTime dt)
	{
		var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(TimeZoneInfo.ConvertTime(dt, TimeZoneInfo.Local));

		return (utcDateTime - DateTimeHelper.TimestampStart).Ticks / 10;
	}
	/// <summary>
	/// 判断时间是否在区间内
	/// </summary>
	/// <param name="this"></param>
	/// <param name="start">开始</param>
	/// <param name="end">结束</param>
	/// <param name="mode">模式</param>
	/// <returns></returns>
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

	/// <summary>
	/// 区间模式
	/// </summary>
	public enum RangeMode
	{
		/// <summary>
		/// 开区间
		/// </summary>
		Open,

		/// <summary>
		/// 闭区间
		/// </summary>
		Close,

		/// <summary>
		/// 左开右闭区间
		/// </summary>
		OpenClose,

		/// <summary>
		/// 左闭右开区间
		/// </summary>
		CloseOpen
	}
}
