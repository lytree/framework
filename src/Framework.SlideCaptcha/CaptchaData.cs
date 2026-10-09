namespace Framework.SlideCaptcha
{
	/// <summary>
	/// 生成验证码后返回给前端的载荷：验证码 id、背景图与滑块图的 Base64 内容。
	/// </summary>
	public class CaptchaData
	{
		/// <summary>
		/// 使用指定的 id、背景图与滑块图初始化验证码数据。
		/// </summary>
		/// <param name="id">验证码唯一标识，后续校验时必须原样回传。</param>
		/// <param name="backgroundImage">背景图（含凹槽）的 Base64 字符串。</param>
		/// <param name="sliderImage">滑块图的 Base64 字符串。</param>
		public CaptchaData(string id, string backgroundImage, string sliderImage)
		{
			Id = id;
			BackgroundImage = backgroundImage;
			SliderImage = sliderImage;
		}

		/// <summary>
		/// 验证码唯一标识（一般为 Guid 字符串），客户端在提交验证时必须带回。
		/// </summary>
		public string Id { get; set; }
		/// <summary>
		/// 背景图（含凹槽）的 Base64 字符串，前端可直接作为 <c>img src</c> 使用。
		/// </summary>
		public string BackgroundImage { get; set; }
		/// <summary>
		/// 滑动块图的 Base64 字符串，前端叠加在背景图之上用于滑动验证。
		/// </summary>
		public string SliderImage { get; set; }
	}
}