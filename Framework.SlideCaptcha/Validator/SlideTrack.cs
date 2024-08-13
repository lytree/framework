
using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha;

/// <summary>
/// 滑动轨迹
/// </summary>
public class SlideTrack
{
	/// <summary>
	/// 背景图片宽度(可能经过缩放，不是原始图片宽高)
	/// </summary>
	public int BackgroundImageWidth { get; set; }
	/// <summary>
	/// 背景图片高度(可能经过缩放，不是原始图片宽高)
	/// </summary>
	public int BackgroundImageHeight { get; set; }
	/// <summary>
	/// 滑块图片宽度(可能经过缩放，不是原始图片宽高)
	/// </summary>
	public int SliderImageWidth { get; set; }
	/// <summary>
	/// 滑块图片高度(可能经过缩放，不是原始图片宽高)
	/// </summary>
	public int SliderImageHeight { get; set; }
	/// <summary>
	/// 滑动开始时间(可能经过缩放，不是原始图片宽高)
	/// </summary>
	public DateTime StartTime { get; set; }
	/// <summary>
	/// 滑动结束时间
	/// </summary>
	public DateTime EndTime { get; set; }
	/// <summary>
	/// 轨迹
	/// </summary>
	public List<Track> Tracks { get; set; } = new();

	/// <summary>
	/// 滑动比例
	/// </summary>
	public float Percent
	{
		get
		{
			if (BackgroundImageWidth <= 0) return -1;
			if (Tracks.Count <= 0) return -1;

			var lastTrack = Tracks[Tracks.Count - 1];
			return 1.0f * lastTrack.X / BackgroundImageWidth;
		}
	}

	public void Check()
	{
		// 校验
		if (BackgroundImageWidth <= 0) throw new SlideCaptchaException($"SlideTrack数据异常: {nameof(BackgroundImageWidth)}小于等于0");
		if (BackgroundImageHeight <= 0) throw new SlideCaptchaException($"SlideTrack数据异常: {nameof(BackgroundImageHeight)}小于等于0");
		if (SliderImageWidth <= 0) throw new SlideCaptchaException($"SlideTrack数据异常: {nameof(SliderImageWidth)}小于等于0");
		if (SliderImageHeight <= 0) throw new SlideCaptchaException($"SlideTrack数据异常: {nameof(SliderImageHeight)}小于等于0");
		if (StartTime == DateTime.MinValue) throw new SlideCaptchaException($"SlideTrack数据异常: {nameof(StartTime)}为空");
		if (EndTime == DateTime.MinValue) throw new SlideCaptchaException($"SlideTrack数据异常: {nameof(EndTime)}为空");
	}

	public void CheckTracks()
	{
		if (Tracks == null || Tracks.Count == 0) throw new SlideCaptchaException($"SlideTrack数据异常: {nameof(Tracks)}为空");
	}
}

public class Track
{
	public int X { get; set; }
	public int Y { get; set; }
	public int T { get; set; }
}
