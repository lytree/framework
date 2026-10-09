using Framework.SlideCaptcha.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Framework.SlideCaptcha.Resources.Handler
{
	/// <summary>
	/// 带进程内缓存的 <see cref="IResourceHandlerManager"/> 实现：
	/// 按注册顺序遍历 <see cref="IResourceHandler"/>，由第一个 <see cref="IResourceHandler.CanHandle"/> 返回 <c>true</c> 的处理器解析资源，
	/// 并将结果以 <see cref="Resource"/> 为键写入字典，避免重复 IO（重复的资源应保证引用相等/同一实例才能命中缓存）。
	/// </summary>
	public class CachedResourceHandlerManager : IResourceHandlerManager
	{
		private readonly IEnumerable<IResourceHandler> _resourceHandlers;
		// 缓存以 Resource 引用作为键；调用方在注册时通常传入单例 Provider 提供的 Resource 实例，因此引用相等可命中。
		private readonly Dictionary<Resource, byte[]> _cache = new();

		/// <summary>
		/// 初始化 <see cref="CachedResourceHandlerManager"/>，注入全部 <see cref="IResourceHandler"/>。
		/// </summary>
		/// <param name="resourceHandlers">注册到容器的全部资源处理器（如 <see cref="FileResourceHandler"/>、<see cref="EmbeddedResourceHandler"/>）。</param>
		public CachedResourceHandlerManager(IEnumerable<IResourceHandler> resourceHandlers)
		{
			_resourceHandlers = resourceHandlers;
		}

		/// <summary>
		/// 根据 <paramref name="resource"/> 解析并返回字节，命中缓存时直接返回。
		/// </summary>
		/// <param name="resource">待解析的资源描述，必须非空。</param>
		/// <returns>资源的原始字节数组。</returns>
		/// <exception cref="ArgumentNullException">当 <paramref name="resource"/> 为 <c>null</c> 时抛出。</exception>
		/// <exception cref="SlideCaptchaException">当没有任何已注册的 <see cref="IResourceHandler"/> 能处理 <paramref name="resource"/> 的类型时抛出。</exception>
		public byte[] Handle(Resource resource)
		{
			if (resource == null) throw new ArgumentNullException(nameof(resource));

			if (_cache.ContainsKey(resource))
			{
				return _cache[resource];
			}

			foreach (var provider in _resourceHandlers)
			{
				if (provider.CanHandle(resource.Type))
				{
					var bytes = provider.Handle(resource);
					_cache.Add(resource, bytes);
					return bytes;
				}
			}

			throw new SlideCaptchaException("没有可用的资源提供者!");
		}
	}
}