using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha.Validator
{
	/// <summary>
	/// 滑块轨迹校验契约：对用户提交的 <see cref="SlideTrack"/> 与生成阶段保存的 <see cref="CaptchaValidateData"/>（缺口位置 + 容差）进行比较。
	/// 实现需要负责位置、轨迹形状、时间分布等防机器判断逻辑；当轨迹数据本身非法时，建议抛出 <see cref="Exceptions.SlideCaptchaException"/>，由上层转为「验证失败」。
	/// </summary>
	public interface IValidator
	{
		/// <summary>
		/// 校验用户提交的滑动轨迹是否通过。
		/// </summary>
		/// <param name="slideTrack">用户前端提交的轨迹数据（点位序列、时间戳等）。</param>
		/// <param name="captchaValidateData">生成阶段写入存储的预期位置与容差。</param>
		/// <returns>通过则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
		bool Validate(SlideTrack slideTrack, CaptchaValidateData captchaValidateData);
	}
}