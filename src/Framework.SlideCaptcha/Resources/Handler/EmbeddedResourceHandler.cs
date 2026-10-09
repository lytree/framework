using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Framework.SlideCaptcha.Resources.Handler
{
	/// <summary>
	/// 内嵌资源（Assembly Manifest Resource）处理器：负责将 <c>Resource.Data</c> 作为内嵌资源名从当前类型所在程序集中读取字节。
	/// 仅处理类型标识为 <see cref="TYPE"/> 的资源。
	/// </summary>
	public class EmbeddedResourceHandler : IResourceHandler
	{
		/// <summary>
		/// 该处理器对应的资源类型标识（与 <see cref="Resource.Type"/> 对应）。
		/// </summary>
		public const string TYPE = "embedded";

		/// <summary>
		/// 当 <paramref name="handlerType"/> 等于 <see cref="TYPE"/> 时返回 <c>true</c>。
		/// </summary>
		/// <param name="handlerType">资源类型标识。</param>
		/// <returns>匹配则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
		public bool CanHandle(string handlerType)
		{
			return handlerType == TYPE;
		}

		/// <summary>
		/// 从 <see cref="EmbeddedResourceHandler"/> 所在程序集中按内嵌资源名读取并返回完整字节。
		/// </summary>
		/// <param name="resource">目标资源，其中 <c>Data</c> 为内嵌资源名（含命名空间前缀）。</param>
		/// <returns>资源的字节内容。</returns>
		/// <exception cref="InvalidOperationException">当程序集中找不到指定名称的内嵌资源时抛出。</exception>
		public byte[] Handle(Resource resource)
		{
			// 用类型所在程序集而非 GetExecutingAssembly()，
			// 否则本类型被其他程序集间接引用时会取到错误的程序集
			var assembly = typeof(EmbeddedResourceHandler).Assembly;
			using var stream = assembly.GetManifestResourceStream(resource.Data)
				?? throw new InvalidOperationException($"未找到内嵌资源: {resource.Data}");
			return StreamToBytes(stream);
		}

		// 私有工具方法：把可定位的 Stream 完整读取为字节数组并复位游标。
		private static byte[] StreamToBytes(Stream stream)
		{
			byte[] bytes = new byte[stream.Length];
			stream.ReadExactly(bytes);

			// 设置当前流的位置为流的开始
			stream.Seek(0, SeekOrigin.Begin);
			return bytes;
		}
	}
}
