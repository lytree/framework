using Microsoft.AspNetCore.Server.Kestrel.Core;
using Middleware.FlowAnalyze;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Hosting
{
	/// <summary>
	/// ListenOptions扩展
	/// </summary>
	public static partial class ListenOptionsExtensions
	{
		/// <summary>
		/// 使用流量分析中间件
		/// </summary>
		/// <param name="listen"></param>
		/// <returns></returns>
		public static ListenOptions UseFlowAnalyze(this ListenOptions listen)
		{
			listen.Use<FlowAnalyzeMiddleware>();
			return listen;
		}
	}
}
