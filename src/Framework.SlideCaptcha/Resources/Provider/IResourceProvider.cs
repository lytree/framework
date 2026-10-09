using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha.Resources.Provider
{
	/// <summary>
	/// 资源提供者契约：向 <see cref="DefaultResourceManager"/> 注入可用的背景图与「滑块+缺口」模板对。
	/// 多个实现可并存（如一个来自配置、一个来自内嵌资源），<see cref="DefaultResourceManager"/> 会将它们合并。
	/// </summary>
	public interface IResourceProvider
	{
		/// <summary>
		/// 返回当前提供者贡献的全部背景图。
		/// </summary>
		/// <returns>背景图资源列表；若无背景图可返回空列表，不应返回 <c>null</c>。</returns>
		List<Resource> Backgrounds();

		/// <summary>
		/// 返回当前提供者贡献的全部滑块/缺口模板对。
		/// </summary>
		/// <returns>模板对列表；若无模板可返回空列表，不应返回 <c>null</c>。</returns>
		List<TemplatePair> Templates();
	}
}