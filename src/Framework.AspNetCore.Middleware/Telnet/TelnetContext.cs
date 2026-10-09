using Microsoft.AspNetCore.Connections;
using Framework.AspNetCore.Application;

namespace Middleware.Telnet
{
	/// <summary>
	/// Telnet 连接的应用上下文。<br/>
	/// 在 Kestrel 的 Telnet 连接处理器中，每解析完一行客户端输入都会构造一个 <see cref="TelnetContext"/>，
	/// 交给应用中间件链（<see cref="IApplicationMiddleware{TContext}"/>）处理；
	/// 中间件可通过 <see cref="Request"/> 读取输入、<see cref="Response"/> 写回响应，<see cref="Abort"/> 用于关闭连接。
	/// </summary>
	sealed class TelnetContext : ApplicationContext
	{
		// 底层 Kestrel 连接上下文，仅用于在 Abort() 中向底层传递中止信号。
		private readonly ConnectionContext context;

		/// <summary>
		/// 来自 Telnet 客户端的当前请求内容（已去除行结束符的一行字符串）。
		/// </summary>
		public string Request { get; }

		/// <summary>
		/// 用于向 Telnet 客户端写回响应的写入器封装。
		/// </summary>
		public TelnetResponse Response { get; }

		/// <summary>
		/// 使用给定的请求内容、响应写入器与底层 Kestrel 连接上下文构造 Telnet 应用上下文。
		/// </summary>
		/// <param name="request">来自客户端的当前行内容。</param>
		/// <param name="response">用于向客户端回写响应的封装对象。</param>
		/// <param name="context">底层 Kestrel 连接上下文，其 <see cref="ConnectionContext.Features"/> 会被透传到基类。</param>
		public TelnetContext(string request, TelnetResponse response, ConnectionContext context)
			: base(context.Features)
		{
			this.context = context;
			Request = request;
			Response = response;
		}

		/// <summary>
		/// 立即中止底层 Kestrel 连接。
		/// <para>调用后客户端将收到连接关闭；通常用于会话结束（如客户端输入 "bye"）时主动断开。</para>
		/// </summary>
		public void Abort()
		{
			context.Abort();
		}
	}
}
