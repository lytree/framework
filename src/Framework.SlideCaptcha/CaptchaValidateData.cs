using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha
{
	/// <summary>
	/// 写入存储的验证码校验数据。用于在用户提交验证时比对「期望缺口位置」与「实际滑动位置」。
	/// </summary>
	public class CaptchaValidateData
	{
		/// <summary>
		/// 使用指定的缺口百分比与容错阈值初始化校验数据。
		/// </summary>
		/// <param name="percent">凹槽中心 x 坐标占背景图宽度的比例，取值范围 <c>(0, 1)</c>。</param>
		/// <param name="tolerant">位置容错范围（单位与 <paramref name="percent"/> 相同，按比例判定）；一般取 <c>0.01</c>~<c>0.05</c>。</param>
		public CaptchaValidateData(float percent, float tolerant)
		{
			Percent = percent;
			Tolerant = tolerant;
		}

		/// <summary>
		/// 凹槽在背景图上的横向百分比位置（<c>X / BackgroundImageWidth</c>），取值范围 <c>(0, 1)</c>。
		/// </summary>
		public float Percent { get; set; }
		/// <summary>
		/// 位置容错阈值。用户滑动位置的百分比与 <see cref="Percent"/> 的差值不超过该值即视为通过。
		/// </summary>
		public float Tolerant { get; set; }
	}
}
