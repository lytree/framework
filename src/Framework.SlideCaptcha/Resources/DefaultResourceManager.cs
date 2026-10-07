
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
	public class DefaultResourceManager : IResourceManager
	{
		// 本类以单例注册，不能持有非线程安全的 Random 实例字段，改用 Random.Shared
		private readonly IResourceHandlerManager _resourceProviderManager;
		private readonly List<Resource> _backgrounds = new();
		private readonly List<TemplatePair> _templates = new();

		public DefaultResourceManager(IEnumerable<IResourceProvider> resourceProviders, IResourceHandlerManager resourceProviderManager)
		{
			_resourceProviderManager = resourceProviderManager;

			foreach (var provider in resourceProviders)
			{
				_backgrounds.AddRange(provider.Backgrounds());
				_templates.AddRange(provider.Templates());
			}
		}

		public byte[] RandomBackground()
		{
			if (_backgrounds.Count == 0) throw new SlideCaptchaException("背景图不能为空");

			var background = _backgrounds[Random.Shared.Next(_backgrounds.Count)];
			return _resourceProviderManager.Handle(background);
		}

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
