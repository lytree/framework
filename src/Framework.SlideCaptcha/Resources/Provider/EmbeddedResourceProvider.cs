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
	/// <summary>
	/// 基于程序集内嵌资源的 <see cref="IResourceProvider"/> 实现：扫描当前程序集中所有
	/// <c>Framework.SlideCaptcha.templates.&lt;编号&gt;.slider.png</c> / <c>.hole.png</c> 形式的内嵌资源，
	/// 按编号两两配对为 <see cref="TemplatePair"/>。
	/// </summary>
	/// <remarks>
	/// 故意不提供 <see cref="Backgrounds"/>，仅承担「内置滑块/缺口模板」职责，
	/// 业务背景图通常由 <see cref="OptionsResourceProvider"/> 通过配置注入。
	/// </remarks>
	public class EmbeddedResourceProvider : IResourceProvider
	{
		// 内嵌资源的命名空间前缀；模板命名约定：<prefix><index>.slider.png / <prefix><index>.hole.png
		private const string TEMPLATE_PREFIX = "Framework.SlideCaptcha.templates.";

		/// <summary>
		/// 不提供背景图（保持空列表）；调用方应通过 <see cref="OptionsResourceProvider"/> 注入业务背景图。
		/// </summary>
		/// <returns>始终为空的 <see cref="Resource"/> 列表。</returns>
		public List<Resource> Backgrounds()
		{
			return new List<Resource>();
		}

		/// <summary>
		/// 扫描当前程序集中符合 <c>&lt;prefix&gt;&lt;index&gt;.slider.png</c> 与 <c>&lt;prefix&gt;&lt;index&gt;.hole.png</c> 命名约定的内嵌资源，
		/// 按 <c>index</c> 升序两两配对为 <see cref="TemplatePair"/>。
		/// </summary>
		/// <returns>按编号排序的模板对列表；若程序集中没有任何匹配资源，则返回空列表。</returns>
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