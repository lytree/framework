
using Framework.AspNetCore.Application;
using System;
using System.Threading.Tasks;

namespace Middleware.Telnet.Middleware
{
	/// <summary>
	/// Telnet "bye" 结束会话中间件。<br/>
	/// 在 Telnet 应用中间件链中检测客户端输入是否为 <c>bye</c>（大小写不敏感）：
	/// 命中则向客户端回写告别语并中止底层连接；否则将请求转发给下游中间件。<br/>
	/// 通常作为 Telnet 链路的最末端中间件（处理完即终止会话）。
	/// </summary>
	sealed class ByeMiddleware : IApplicationMiddleware<TelnetContext>
	{
		/// <summary>
		/// 处理一行 Telnet 请求：若请求内容为 <c>bye</c> 则结束会话，否则继续传递。
		/// </summary>
		/// <param name="next">链路中的下一个应用中间件。</param>
		/// <param name="context">当前 Telnet 请求的应用上下文。</param>
		/// <returns>表示本次中间件（含可能的下游调用）执行完成的 <see cref="Task"/>。</returns>
		public async Task InvokeAsync(ApplicationDelegate<TelnetContext> next, TelnetContext context)
		{
			if (context.Request.Equals("bye", StringComparison.OrdinalIgnoreCase))
			{
				await context.Response.WriteLineAsync("Have a good day!");
				context.Abort();
			}
			else
			{
				await next(context);
			}
		}
	}
}
