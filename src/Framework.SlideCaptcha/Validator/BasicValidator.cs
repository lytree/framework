using Framework.SlideCaptcha.Exceptions;
using Microsoft.Extensions.Options;

namespace Framework.SlideCaptcha.Validator
{
	/// <summary>
	/// 位置 + 行为轨迹校验器。
	/// 通过 <see cref="CaptchaBuilderExtensions.ReplaceValidator{TValidator}"/> 替换默认的
	/// <see cref="SimpleValidator"/> 启用；阈值可通过 <see cref="CaptchaOptions.Track"/> 配置。
	/// </summary>
	public class BasicValidator : BaseValidator, IValidator
	{
		private readonly IOptionsMonitor<CaptchaOptions> _options;

		public BasicValidator(IOptionsMonitor<CaptchaOptions> options)
		{
			_options = options;
		}

		public override bool ValidateCore(SlideTrack slideTrack, CaptchaValidateData captchaValidateData)
		{
			slideTrack.CheckTracks();

			var track = _options.CurrentValue.Track;
			var trackList = slideTrack.Tracks;
			var bgImageWidth = slideTrack.BackgroundImageWidth;

			// check1: 滑动总时长必须达到下限
			// 原实现用 DateTime.ToFileTimeUtc()，其单位是 100 纳秒，
			// 因此 "start + 300 > end" 实际只拦截了 30 微秒，等于形同虚设。
			// 这里直接用 TimeSpan 得到真实毫秒差。
			var elapsed = slideTrack.EndTime - slideTrack.StartTime;
			if (elapsed < TimeSpan.FromMilliseconds(track.MinSlidingMilliseconds))
			{
				return false;
			}

			// check2: 轨迹点数量需要在合理区间（过少说明采样异常，过多说明伪造）
			if (trackList.Count < track.MinTrackCount || trackList.Count > bgImageWidth * track.MaxTrackCountPerWidth)
			{
				return false;
			}

			// check3: 起点必须落在原点附近，防止一上来就乱跑
			var firstTrack = trackList[0];
			if (Math.Abs(firstTrack.X) > track.StartPointTolerance || Math.Abs(firstTrack.Y) > track.StartPointTolerance)
			{
				return false;
			}

			var sameYCount = 1;
			var outOfRangeCount = 0;

			for (int i = 1; i < trackList.Count; i++)
			{
				var current = trackList[i];
				var previous = trackList[i - 1];

				// check4: y 轴完全不动 → 机器行为
				if (firstTrack.Y == current.Y)
				{
					sameYCount++;
				}

				// check5: x 轴越过背景图宽度 → 机器行为
				if (current.X >= bgImageWidth)
				{
					outOfRangeCount++;
				}

				// check6: 相邻点位移跳变过大。
				// 原实现只判断正向跳变（cur - pre > 50），负向大幅跳跃（往回猛拉）可被绕过，
				// 这里对 x 方向取绝对值；y 方向保留正向判断，避免正常快速拖动被误杀。
				if (Math.Abs(current.X - previous.X) > track.MaxJumpX || current.Y - previous.Y > track.MaxJumpY)
				{
					return false;
				}
			}

			// check7: y 轴恒定 或 越界点过多 → 机器行为
			if (sameYCount == trackList.Count || outOfRangeCount > track.MaxOutOfRangeTrackCount)
			{
				return false;
			}

			return true;
		}
	}
}