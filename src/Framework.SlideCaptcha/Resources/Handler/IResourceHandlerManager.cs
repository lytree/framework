using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Framework.SlideCaptcha.Resources.Handler
{
	/// <summary>
	/// 资源处理器管理器契约：在多个 <see cref="IResourceHandler"/> 中路由请求并返回资源字节。
	/// 典型实现可在内部引入缓存以避免重复 IO。
	/// </summary>
	public interface IResourceHandlerManager
	{
		/// <summary>
		/// 根据 <paramref name="resource"/> 解析并返回字节内容。
		/// </summary>
		/// <param name="resource">待解析的资源描述。</param>
		/// <returns>资源字节内容。</returns>
		byte[] Handle(Resource resource);
	}
}
