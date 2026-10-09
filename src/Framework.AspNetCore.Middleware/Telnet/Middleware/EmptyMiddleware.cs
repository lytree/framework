using Framework.AspNetCore.Application;
using System.Threading.Tasks;

namespace Middleware.Telnet.Middleware
{
	/// <summary>
	/// Telnet 空请求提示中间件。<br/>
	/// 当客户端只发送了空行（<see cref="string.IsNullOrEmpty"/> 命中）时，
	/// 回写一行提示语要求客户端重新输入，不再向下游传递；
	/// 否则将请求转发给下游中间件继续处理。<br/>
	/// 通常放置在 Telnet 链路的最前段，作为链路入口的输入合法性检查。
	/// </summary>
	sealed class EmptyMiddleware : IApplicationMiddleware<TelnetContext>
	{
		/// <summary>
		/// 处理一行 Telnet 请求：空请求时回写提示，非空时交由下游处理。
		/// </summary>
		/// <param name="next">链路中的下一个应用中间件，仅在请求非空时被调用。</param>
		/// <param name="context">当前 Telnet 请求的应用上下文。</param>
		/// <returns>表示本次中间件（含可能的下游调用）执行完成的 <see cref="Task"/>。</returns>
		public async Task InvokeAsync(ApplicationDelegate<TelnetContext> next, TelnetContext context)
		{
			if (string.IsNullOrEmpty(context.Request))
			{
				await context.Response.WriteLineAsync("Please type something.");
			}
			else
			{
				await next(context);
			}
		}
	}
}
