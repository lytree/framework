using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Framework.SlideCaptcha.Resources.Handler
{
	public class EmbeddedResourceHandler : IResourceHandler
	{
		public const string TYPE = "embedded";

		public bool CanHandle(string handlerType)
		{
			return handlerType == TYPE;
		}

		public byte[] Handle(Resource resource)
		{
			// 用类型所在程序集而非 GetExecutingAssembly()，
			// 否则本类型被其他程序集间接引用时会取到错误的程序集
			var assembly = typeof(EmbeddedResourceHandler).Assembly;
			using var stream = assembly.GetManifestResourceStream(resource.Data)
				?? throw new InvalidOperationException($"未找到内嵌资源: {resource.Data}");
			return StreamToBytes(stream);
		}

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
