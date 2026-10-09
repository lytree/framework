
using System.IO.Pipelines;

namespace Middleware.FlowAnalyze
{
	/// <summary>
	/// 流量分析专用的双向管道包装器。<br/>
	/// 将底层 <see cref="IDuplexPipe"/> 的输入/输出流都包装为 <see cref="FlowAnalyzeStream"/>，
	/// 由 <see cref="IFlowAnalyzer"/> 在每次读写时统计字节数。<br/>
	/// 通常由 <see cref="FlowAnalyzeMiddleware"/> 在 Kestrel 连接管道中创建并接管 <c>ConnectionContext.Transport</c>。
	/// </summary>
	sealed class FlowAnalyzeDuplexPipe : DelegatingDuplexPipe<FlowAnalyzeStream>
	{
		/// <summary>
		/// 使用给定的双向管道与流量分析器构造包装实例。
		/// </summary>
		/// <param name="duplexPipe">被包装的底层双向管道（通常来自 Kestrel 的 <c>ConnectionContext.Transport</c>）。</param>
		/// <param name="flowAnalyzer">流量分析器，用于在每次读/写完成后回调上报字节数；不可为 <c>null</c>。</param>
		public FlowAnalyzeDuplexPipe(IDuplexPipe duplexPipe, IFlowAnalyzer flowAnalyzer) :
			base(duplexPipe, stream => new FlowAnalyzeStream(stream, flowAnalyzer))
		{
		}
	}
}
