using Microsoft.AspNetCore.Connections;
using Framework.AspNetCore.Application;

namespace Middleware.Telnet
{
	sealed class TelnetContext : ApplicationContext
	{
		private readonly ConnectionContext context;

		public string Request { get; }

		public TelnetResponse Response { get; }

		public TelnetContext(string request, TelnetResponse response, ConnectionContext context)
			: base(context.Features)
		{
			this.context = context;
			Request = request;
			Response = response;
		}

		public void Abort()
		{
			context.Abort();
		}
	}
}
