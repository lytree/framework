using Framework.SlideCaptcha.Resources;

namespace Framework.SlideCaptcha
{
	/// <summary>
	/// 行为轨迹校验阈值。仅在注册 <see cref="Validator.BasicValidator"/> 时生效。
	/// </summary>
	public class CaptchaTrackOptions
	{
		/// <summary>
		/// 滑动最短时长(毫秒)。低于该值判定为机器行为。
		/// </summary>
		public int MinSlidingMilliseconds { get; set; } = 300;

		/// <summary>
		/// 轨迹点最少数量
		/// </summary>
		public int MinTrackCount { get; set; } = 10;

		/// <summary>
		/// 每单位背景图宽度允许的最大轨迹点数量(用于限制伪造的密集轨迹)
		/// </summary>
		public int MaxTrackCountPerWidth { get; set; } = 5;

		/// <summary>
		/// 起点允许的偏移量(像素)
		/// </summary>
		public int StartPointTolerance { get; set; } = 10;

		/// <summary>
		/// 相邻轨迹点 x 轴最大跳变(像素)
		/// </summary>
		public int MaxJumpX { get; set; } = 50;

		/// <summary>
		/// 相邻轨迹点 y 轴最大跳变(像素)
		/// </summary>
		public int MaxJumpY { get; set; } = 50;

		/// <summary>
		/// 允许的越界(track.X >= 背景图宽度)轨迹点数量上限
		/// </summary>
		public int MaxOutOfRangeTrackCount { get; set; } = 200;
	}

	public class CaptchaOptions
	{
		/// <summary>
		/// 过期时长
		/// </summary>
		public int ExpirySeconds { get; set; } = 60;

		/// <summary>
		/// 存储键前缀。
		/// 拼接规则：<c>KeyPrefix + key</c>，最终键以 <c>"slide-captcha"</c> 开头。
		/// </summary>
		public string StorageKeyPrefix { get; set; } = "slide-captcha";

		/// <summary>
		/// 存储键前缀（拼写错误，保留为 <see cref="StorageKeyPrefix"/> 的别名以兼容旧配置）。
		/// </summary>
		[Obsolete("StoreageKeyPrefix 拼写错误，请改用 StorageKeyPrefix。", false)]
		public string? StoreageKeyPrefix
		{
			get => StorageKeyPrefix;
			set
			{
				if (value != null)
					StorageKeyPrefix = value;
			}
		}

		/// <summary>
		/// 容错值(校验时用，缺口位置与实际滑动位置匹配容错范围)
		/// </summary>
		public float Tolerant { get; set; } = 0.02f;

		/// <summary>
		/// 行为轨迹校验阈值(BasicValidator 生效)
		/// </summary>
		public CaptchaTrackOptions Track { get; set; } = new();

		/// <summary>
		/// 干扰块数量：在背景图上额外绘制若干干扰凹槽(不参与校验)，用于提高机器识别难度。
		/// </summary>
		/// <remarks>
		/// 注意：该选项当前尚未在 <see cref="Generator.DefaultCaptchaImageGenerator"/> 中实现，
		/// 设置它不会产生任何效果。保留是为了不破坏既有配置。
		/// </remarks>
		[Obsolete("InterferenceCount 尚未实现，设置后不会生效。", false)]
		public int InterferenceCount { get; set; } = 0;

		/// <summary>
		/// 背景图
		/// </summary>
		public List<Resource> Backgrounds { get; set; } = new List<Resource>();

		/// <summary>
		/// 模板图(必须是slider,hole的顺序依次出现)
		/// </summary>
		public List<TemplatePair> Templates { get; set; } = new List<TemplatePair>();
	}
}