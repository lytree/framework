using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Framework.AspNetCore;
using System.Threading.Tasks;

namespace Middleware.FlowXor
{
	sealed class XorMiddleware : IKestrelMiddleware
	{
		private readonly ILogger<XorMiddleware> logger;

		public XorMiddleware(ILogger<XorMiddleware> logger)
		{
			this.logger = logger;
		}

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
