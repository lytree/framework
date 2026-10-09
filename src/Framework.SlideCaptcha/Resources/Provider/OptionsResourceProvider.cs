using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha.Resources.Provider
{
	/// <summary>
	/// 基于 <see cref="CaptchaOptions"/> 的 <see cref="IResourceProvider"/> 实现：
	/// 在初始化时一次性读取 <see cref="IOptionsMonitor{T}.CurrentValue"/> 中的 <c>Backgrounds</c> 与 <c>Templates</c>，
	/// 后续修改配置需要重建实例才会被感知（不会监听变更）。
	/// </summary>
	public class OptionsResourceProvider : IResourceProvider
	{
		private readonly List<Resource> _backgrounds = new List<Resource>();
		private readonly List<TemplatePair> _templates = new List<TemplatePair>();

		/// <summary>
		/// 通过 <see cref="IOptionsMonitor{T}"/> 读取一次当前 <see cref="CaptchaOptions"/>，
		/// 把 <c>Backgrounds</c> 与 <c>Templates</c> 复制到内部列表。
		/// </summary>
		/// <param name="optionAccessor">用于访问 <see cref="CaptchaOptions"/> 的 options 监控器；仅取 <c>CurrentValue</c> 快照。</param>
		public OptionsResourceProvider(IOptionsMonitor<CaptchaOptions> optionAccessor)
		{
			var options = optionAccessor.CurrentValue;

			if (options.Backgrounds != null)
			{
				_backgrounds.AddRange(options.Backgrounds);
			}

			if (options.Templates != null)
			{
				_templates.AddRange(options.Templates);
			}
		}

		/// <summary>
		/// 返回构造期从配置中读到的全部背景图。注意返回的是内部列表引用，调用方不应修改。
		/// </summary>
		/// <returns>背景图资源列表（可能为空）。</returns>
		public List<Resource> Backgrounds()
		{
			return _backgrounds; // ?克隆
		}

		/// <summary>
		/// 返回构造期从配置中读到的全部模板对。注意返回的是内部列表引用，调用方不应修改。
		/// </summary>
		/// <returns>模板对列表（可能为空）。</returns>
		public List<TemplatePair> Templates()
		{
			return _templates;
		}
	}
}