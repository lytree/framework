using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Framework.SlideCaptcha.Resources.Handler
{
	/// <summary>
	/// 资源处理器契约：根据资源类型标记将 <see cref="Resource"/> 解析为原始字节。
	/// 多个实现可并存，<see cref="IResourceHandlerManager"/> 会按 <see cref="CanHandle"/> 顺序选择第一个能处理的处理器。
	/// </summary>
	public interface IResourceHandler
	{
		/// <summary>
		/// 判断当前处理器是否能处理指定类型的资源。
		/// </summary>
		/// <param name="handlerType">资源类型标识（与 <see cref="Resource.Type"/> 对应，例如 "embedded"、"file"）。</param>
		/// <returns>若能处理则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
		bool CanHandle(string handlerType);

		/// <summary>
		/// 读取并返回资源的完整字节内容。调用方需自行保证生命周期与缓存。
		/// </summary>
		/// <param name="resource">待解析的资源描述，<c>null</c> 由实现自行决定是否抛异常。</param>
		/// <returns>资源的原始字节数组。</returns>
		byte[] Handle(Resource resource);
	}
}
