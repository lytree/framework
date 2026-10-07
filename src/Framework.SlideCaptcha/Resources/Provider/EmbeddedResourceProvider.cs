using Framework.SlideCaptcha.Resources;
using Framework.SlideCaptcha.Resources.Handler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Framework.SlideCaptcha.Resources.Provider
{
	public class EmbeddedResourceProvider : IResourceProvider
	{
		private const string TEMPLATE_PREFIX = "Framework.SlideCaptcha.templates.";

		public List<Resource> Backgrounds()
		{
			return new List<Resource>();
		}

		public List<TemplatePair> Templates()
		{
			var templatePairs = new List<TemplatePair>();

			var assembly = typeof(EmbeddedResourceProvider).Assembly;
			// 形如 Framework.SlideCaptcha.templates.1.slider.png / ....1.hole.png
			var templates = assembly.GetManifestResourceNames()
				.Where(name => name.StartsWith(TEMPLATE_PREFIX, StringComparison.Ordinal))
				.Select(name => name[TEMPLATE_PREFIX.Length..])
				.Where(name => name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
				.Select(name => name[..^".png".Length])
				// 以 <编号>.slider / <编号>.hole 成对出现
				.Where(name => name.EndsWith(".slider", StringComparison.Ordinal)
							|| name.EndsWith(".hole", StringComparison.Ordinal))
				.ToList();

			var indexes = templates
				.Select(name => name[..name.LastIndexOf('.')])
				.Distinct()
				.OrderBy(index => index, StringComparer.Ordinal);

			foreach (var index in indexes)
			{
				var sliderResourceName = $"{TEMPLATE_PREFIX}{index}.slider.png";
				var holeResourceName = $"{TEMPLATE_PREFIX}{index}.hole.png";

				var sliderResource = new Resource(EmbeddedResourceHandler.TYPE, sliderResourceName);
				var holeResource = new Resource(EmbeddedResourceHandler.TYPE, holeResourceName);
				templatePairs.Add(TemplatePair.Create(sliderResource, holeResource));
			}

			return templatePairs;
		}
	}
}
