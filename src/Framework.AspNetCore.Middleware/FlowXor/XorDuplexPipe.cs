using Microsoft.Extensions.Logging;
using System.IO.Pipelines;

namespace Middleware.FlowXor
{
	/// <summary>
	/// XOR 字节混淆专用的双向管道包装器。<br/>
	/// 将底层 <see cref="IDuplexPipe"/> 的输入/输出流都包装为 <see cref="XorStream"/>，
	/// 由 <see cref="XorStream"/> 对每个字节执行 XOR 操作以在 Kestrel 连接管道上做轻度混淆；
	/// 通常由 <see cref="XorMiddleware"/> 创建并临时替换 <c>ConnectionContext.Transport</c>。
	/// </summary>
	sealed class XorDuplexPipe : DelegatingDuplexPipe<XorStream>
	{
		/// <summary>
		/// 使用给定的双向管道与日志器构造包装实例。
		/// </summary>
		/// <param name="duplexPipe">被包装的底层双向管道（通常来自 Kestrel 的 <c>ConnectionContext.Transport</c>）。</param>
		/// <param name="logger">用于在 XOR 处理过程中记录诊断信息的日志器实例（如异常、读写字节数等）。</param>
		public XorDuplexPipe(IDuplexPipe duplexPipe, ILogger logger) :
			base(duplexPipe, stream => new XorStream(stream, logger))
		{
		}
	}
}
