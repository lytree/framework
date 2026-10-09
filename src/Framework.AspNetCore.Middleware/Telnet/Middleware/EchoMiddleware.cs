using Framework.AspNetCore.Application;
using System.Threading.Tasks;

namespace Middleware.Telnet.Middleware
{
	/// <summary>
	/// Telnet 回显（Echo）中间件。<br/>
	/// 在 Telnet 应用中间件链中始终将客户端的请求内容回显一行确认消息，
	/// 不调用下游中间件，因此通常作为链路最末端（或单独使用时直接作为终态处理）。<br/>
	/// 主要用于演示与连通性测试。
	/// </summary>
	sealed class EchoMiddleware : IApplicationMiddleware<TelnetContext>
	{
		/// <summary>
		/// 向客户端回显当前请求内容。
		/// </summary>
		/// <param name="next">链路中的下一个应用中间件（本中间件不会调用）。</param>
		/// <param name="context">当前 Telnet 请求的应用上下文。</param>
		/// <returns>表示回显写入并刷新操作完成的 <see cref="Task"/>。</returns>
		public async Task InvokeAsync(ApplicationDelegate<TelnetContext> next, TelnetContext context)
		{
			await context.Response.WriteLineAsync($"Did you say '{context.Request}'?");
		}
	}
}
