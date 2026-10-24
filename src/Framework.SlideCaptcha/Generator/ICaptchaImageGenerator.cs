using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha.Generator
{
	/// <summary>
	/// 验证码图像生成器契约：根据当前可用资源合成一张「背景图 + 滑块图 + 缺口位置」三元组。
	/// 实现负责从 <see cref="Resources.IResourceManager"/> 取资源并做异或/像素合成、缺口定位等图像处理。
	/// </summary>
	public interface ICaptchaImageGenerator
	{
		/// <summary>
		/// 生成一张新的验证码图像数据。
		/// </summary>
		/// <returns>
		/// 包含背景图 / 滑块图 Base64、缺口坐标 <see cref="CaptchaImageData.X"/> / <see cref="CaptchaImageData.Y"/>
		/// 与百分比 <see cref="CaptchaImageData.Percent"/> 的 <see cref="CaptchaImageData"/>。
		/// </returns>
		/// <exception cref="Exceptions.SlideCaptchaException">资源不可用、解码失败，或不满足实现自身的尺寸约束。</exception>
		CaptchaImageData Generate();
	}
}