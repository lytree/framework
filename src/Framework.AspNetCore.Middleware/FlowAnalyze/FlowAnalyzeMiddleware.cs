using Microsoft.AspNetCore.Connections;
using Framework.AspNetCore;
using System.Threading.Tasks;

namespace Middleware.FlowAnalyze
{
	/// <summary>
	/// Kestrel 连接级流量分析中间件。<br/>
	/// 位于管道最外层（在 <see cref="FlowXor.XorMiddleware"/> 之后接入），用 <see cref="FlowAnalyzeDuplexPipe"/>
	/// 替换 <c>ConnectionContext.Transport</c>，从而对每一次底层读写都进行字节数统计，调用结束后恢复原始 Transport。
	/// </summary>
	sealed class FlowAnalyzeMiddleware : IKestrelMiddleware
	{
		private readonly IFlowAnalyzer flowAnalyzer;

		/// <summary>
		/// 构造一个流量分析中间件实例。
		/// </summary>
		/// <param name="flowAnalyzer">用于接收每次读/写字节数回调的分析器实例；通常由 DI 容器注入。</param>
		public FlowAnalyzeMiddleware(IFlowAnalyzer flowAnalyzer)
		{
			this.flowAnalyzer = flowAnalyzer;
		}

		/// <summary>
		/// 在连接管道的入口处安装分析用的双向管道，然后调用下游中间件。<br/>
		/// 使用 <c>try/finally</c> 保证即便下游抛出异常也能恢复 <c>context.Transport</c>，避免污染连接上下文。
		/// </summary>
		/// <param name="next">管道中的下一个连接委托。</param>
		/// <param name="context">当前 Kestrel 连接上下文，其 <c>Transport</c> 会被临时替换为 <see cref="FlowAnalyzeDuplexPipe"/>。</param>
		/// <returns>表示下游中间件执行完成的 <see cref="Task"/>。</returns>
		public async Task InvokeAsync(ConnectionDelegate next, ConnectionContext context)
		{
			var oldTransport = context.Transport;
			try
			{
				await using var duplexPipe = new FlowAnalyzeDuplexPipe(context.Transport, flowAnalyzer);
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
