using Framework.SlideCaptcha.Exceptions;
using Framework.SlideCaptcha.Resources.Handler;
using Framework.SlideCaptcha.Resources.Provider;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Framework.SlideCaptcha.Resources
{
	/// <summary>
	/// 默认的 <see cref="IResourceManager"/> 实现：在构造时合并所有 <see cref="IResourceProvider"/> 提供的背景图与模板，
	/// 之后通过 <see cref="Random.Shared"/> 随机抽取并委托 <see cref="IResourceHandlerManager"/> 解析为字节。
	/// </summary>
	/// <remarks>
	/// 以单例生命周期注册，因此其内部只持有线程安全的 <see cref="Random.Shared"/> 与不可变列表，
	/// 不会引入 <see cref="Random"/> 实例级别的锁争用。
	/// </remarks>
	public class DefaultResourceManager : IResourceManager
	{
		// 本类以单例注册，不能持有非线程安全的 Random 实例字段，改用 Random.Shared
		private readonly IResourceHandlerManager _resourceProviderManager;
		private readonly List<Resource> _backgrounds = new();
		private readonly List<TemplatePair> _templates = new();

		/// <summary>
		/// 初始化 <see cref="DefaultResourceManager"/>：遍历所有 <see cref="IResourceProvider"/>，
		/// 把它们的背景图与模板合并到内部列表中（仅在构造期执行一次）。
		/// </summary>
		/// <param name="resourceProviders">注册到容器中的全部资源提供者（如基于配置的 <see cref="OptionsResourceProvider"/> 和内嵌资源的 <see cref="EmbeddedResourceProvider"/>）。</param>
		/// <param name="resourceProviderManager">负责把 <see cref="Resource"/> 解析为字节的资源处理器管理器。</param>
		public DefaultResourceManager(IEnumerable<IResourceProvider> resourceProviders, IResourceHandlerManager resourceProviderManager)
		{
			_resourceProviderManager = resourceProviderManager;

			foreach (var provider in resourceProviders)
			{
				_backgrounds.AddRange(provider.Backgrounds());
				_templates.AddRange(provider.Templates());
			}
		}

		/// <summary>
		/// 随机返回一张背景图的字节内容。
		/// </summary>
		/// <returns>随机选中的背景图字节。</returns>
		/// <exception cref="SlideCaptchaException">当没有任何 <see cref="IResourceProvider"/> 提供背景图时抛出。</exception>
		public byte[] RandomBackground()
		{
			if (_backgrounds.Count == 0) throw new SlideCaptchaException("背景图不能为空");

			var background = _backgrounds[Random.Shared.Next(_backgrounds.Count)];
			return _resourceProviderManager.Handle(background);
		}

		/// <summary>
		/// 随机返回一个模板对的字节内容：返回值为 <c>(slider, hole)</c>，分别对应滑块图与缺口图。
		/// </summary>
		/// <returns>二元组，第一个元素为滑块图字节，第二个元素为缺口图字节。</returns>
		/// <exception cref="SlideCaptchaException">当没有任何 <see cref="IResourceProvider"/> 提供模板时抛出。</exception>
		public (byte[], byte[]) RandomTemplate()
		{
			if (_templates.Count == 0) throw new SlideCaptchaException("模板不能为空");

			var template = _templates[Random.Shared.Next(_templates.Count)];
			var hole = _resourceProviderManager.Handle(template.Hole);
			var slider = _resourceProviderManager.Handle(template.Slider);
			return (slider, hole);
		}
	}
}