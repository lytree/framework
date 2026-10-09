using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Framework.AspNetCore;
using System.Threading.Tasks;

namespace Middleware.FlowXor
{
	/// <summary>
	/// Kestrel 连接级 XOR 字节混淆中间件。<br/>
	/// 在连接管道的指定位置安装 <see cref="XorDuplexPipe"/> 替换 <c>ConnectionContext.Transport</c>，
	/// 使下游所有读/写都经过 XOR 处理；调用结束后通过 <c>try/finally</c> 恢复原始 Transport。
	/// </summary>
	sealed class XorMiddleware : IKestrelMiddleware
	{
		private readonly ILogger<XorMiddleware> logger;

		/// <summary>
		/// 构造一个 XOR 字节混淆中间件实例。
		/// </summary>
		/// <param name="logger">用于在 XOR 处理过程中记录诊断信息的日志器，通常由 DI 容器注入。</param>
		public XorMiddleware(ILogger<XorMiddleware> logger)
		{
			this.logger = logger;
		}

		/// <summary>
		/// 在连接管道入口处安装 XOR 处理用的双向管道，然后调用下游中间件。
		/// <para>使用 <c>try/finally</c> 保证即便下游抛出异常也能恢复 <c>context.Transport</c>，避免污染连接上下文。</para>
		/// </summary>
		/// <param name="next">管道中的下一个连接委托。</param>
		/// <param name="context">当前 Kestrel 连接上下文，其 <c>Transport</c> 会被临时替换为 <see cref="XorDuplexPipe"/>。</param>
		/// <returns>表示下游中间件执行完成的 <see cref="Task"/>。</returns>
		public async Task InvokeAsync(ConnectionDelegate next, ConnectionContext context)
		{
			var oldTransport = context.Transport;
			try
			{
				await using var duplexPipe = new XorDuplexPipe(context.Transport, logger);
				context.Transport = duplexPipe;
				await next(context);
			}
			finally
			{
				context.Transport = oldTransport;
			}
		}
	}
}
