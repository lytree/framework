using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Framework.SlideCaptcha.Resources.Handler
{
	/// <summary>
	/// 文件系统资源处理器：把 <see cref="Resource.Data"/> 视为文件路径，通过 <see cref="File.ReadAllBytes(string)"/> 同步读取全部字节。
	/// 仅处理类型标识为 <see cref="TYPE"/> 的资源。
	/// </summary>
	public class FileResourceHandler : IResourceHandler
	{
		/// <summary>
		/// 该处理器对应的资源类型标识（与 <see cref="Resource.Type"/> 对应）。
		/// </summary>
		public const string TYPE = "file";

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
		/// 把 <paramref name="resource"/> 的 <c>Data</c> 视为文件路径并读取全部字节。
		/// </summary>
		/// <param name="resource">目标资源，其中 <c>Data</c> 为文件路径。</param>
		/// <returns>文件内容的字节数组。</returns>
		/// <exception cref="ArgumentNullException">当 <paramref name="resource"/> 为 <c>null</c> 时抛出。</exception>
		/// <exception cref="FileNotFoundException">当指定路径下找不到文件时抛出。</exception>
		/// <exception cref="IOException">读取过程中发生 IO 错误时抛出。</exception>
		public byte[] Handle(Resource resource)
		{
			if (resource == null) throw new ArgumentNullException(nameof(resource));
			return File.ReadAllBytes(resource.Data);
		}
	}
}