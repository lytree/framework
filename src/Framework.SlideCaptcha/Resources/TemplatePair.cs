using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha.Resources
{
	/// <summary>
	/// 模板对：把一张滑块图与对应的缺口图绑定在一起，保证它们一定成对出现（例如 <c>1.slider.png</c> 必与 <c>1.hole.png</c> 同组）。
	/// 由 <see cref="Provider.IResourceProvider.Templates"/> 返回。
	/// </summary>
	public class TemplatePair
	{
		/// <summary>
		/// 初始化 <see cref="TemplatePair"/> 的新实例，所有字段均未设置。
		/// </summary>
		public TemplatePair() { }

		/// <summary>
		/// 使用指定的滑块图与缺口图资源初始化 <see cref="TemplatePair"/> 的新实例。
		/// </summary>
		/// <param name="slider">滑块图资源描述。</param>
		/// <param name="hole">对应的缺口图资源描述。</param>
		public TemplatePair(Resource slider, Resource hole)
		{
			Slider = slider;
			Hole = hole;
		}

		/// <summary>
		/// 滑块图资源描述（用户拖动的部分）。
		/// </summary>
		public Resource Slider { get; set; }

		/// <summary>
		/// 缺口图资源描述（背景中需要与滑块对齐的凹槽部分）。
		/// </summary>
		public Resource Hole { get; set; }

		/// <summary>
		/// 工厂方法：创建并返回一个新的 <see cref="TemplatePair"/>。
		/// </summary>
		/// <param name="slider">滑块图资源描述。</param>
		/// <param name="hole">对应的缺口图资源描述。</param>
		/// <returns>包装好 <paramref name="slider"/> 与 <paramref name="hole"/> 的 <see cref="TemplatePair"/>。</returns>
		public static TemplatePair Create(Resource slider, Resource hole)
		{
			return new TemplatePair(slider, hole);
		}
	}
}