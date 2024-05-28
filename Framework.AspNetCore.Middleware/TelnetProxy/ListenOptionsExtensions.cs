using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Middleware.TelnetProxy;

namespace Microsoft.AspNetCore.Hosting
{
	/// <summary>
	///  ListenOptions扩展
	/// </summary>
	public static partial class ListenOptionsExtensions
	{
		/// <summary>
		/// 使用XorTelnetProxyHandler
		/// </summary>
		/// <param name="listen"></param>
		public static void UseXorTelnetProxy(this ListenOptions listen)
		{
			listen.UseConnectionHandler<XorTelnetProxyHandler>();
		}
	}
}
