
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Middleware.FlowAnalyze
{
	/// <summary>
	/// 流量分析专用的流包装器。<br/>
	/// 继承自 <c>DelegatingStream</c>，对每一次底层读/写都调用 <see cref="IFlowAnalyzer.OnFlow"/>
	/// 上报本次的字节数（<see cref="FlowType.Read"/> 或 <see cref="FlowType.Write"/>），从而按连接统计吞吐量。
	/// </summary>
	sealed class FlowAnalyzeStream : DelegatingStream
	{
		private readonly IFlowAnalyzer flowAnalyzer;

		/// <summary>
		/// 使用给定的底层流与流量分析器构造包装实例。
		/// </summary>
		/// <param name="inner">被包装的底层流（通常来自 <see cref="FlowAnalyzeDuplexPipe"/> 拆出的输入/输出流）。</param>
		/// <param name="flowAnalyzer">用于上报每次读/写字节数的分析器实例，不可为 <c>null</c>。</param>
		public FlowAnalyzeStream(Stream inner, IFlowAnalyzer flowAnalyzer)
			: base(inner)
		{
			this.flowAnalyzer = flowAnalyzer;
		}

		/// <summary>
		/// 同步读取：先调用底层流读取，再把实际读取到的字节数上报给分析器。
		/// </summary>
		/// <param name="buffer">接收数据的字节缓冲区。</param>
		/// <param name="offset">缓冲区起始写入偏移。</param>
		/// <param name="count">请求读取的最大字节数。</param>
		/// <returns>本次实际读取到的字节数（可能小于 <paramref name="count"/>）。</returns>
		public override int Read(byte[] buffer, int offset, int count)
		{
			int read = base.Read(buffer, offset, count);
			flowAnalyzer.OnFlow(FlowType.Read, read);
			return read;
		}

		/// <summary>
		/// 同步读取（基于 <see cref="Span{T}"/>）：先调用底层流读取，再上报实际字节数。
		/// </summary>
		/// <param name="destination">接收数据的目标缓冲区。</param>
		/// <returns>本次实际读取到的字节数。</returns>
		public override int Read(Span<byte> destination)
		{
			int read = base.Read(destination);
			flowAnalyzer.OnFlow(FlowType.Read, read);
			return read;
		}

		/// <summary>
		/// 异步读取：先调用底层流读取，再把实际字节数上报给分析器。
		/// </summary>
		/// <param name="buffer">接收数据的字节缓冲区。</param>
		/// <param name="offset">缓冲区起始写入偏移。</param>
		/// <param name="count">请求读取的最大字节数。</param>
		/// <param name="cancellationToken">用于取消异步读取的令牌。</param>
		/// <returns>表示读取操作的 <see cref="Task{TResult}"/>，结果为实际读取的字节数。</returns>
		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			int read = await base.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
			flowAnalyzer.OnFlow(FlowType.Read, read);
			return read;
		}

		/// <summary>
		/// 异步读取（基于 <see cref="Memory{T}"/>）：先调用底层流读取，再上报实际字节数。
		/// </summary>
		/// <param name="destination">接收数据的目标缓冲区。</param>
		/// <param name="cancellationToken">用于取消异步读取的令牌，默认值为 <see cref="CancellationToken.None"/>。</param>
		/// <returns>表示读取操作的 <see cref="ValueTask{TResult}"/>，结果为实际读取的字节数。</returns>
		public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
		{
			int read = await base.ReadAsync(destination, cancellationToken);
			flowAnalyzer.OnFlow(FlowType.Read, read);
			return read;
		}


		/// <summary>
		/// 同步写入：先按预期写入字节数上报，再调用底层流写入。
		/// <para>写入按"预期"字节数上报，而非"实际"字节数；底层流不会截断数据，因此两者等价。</para>
		/// </summary>
		/// <param name="buffer">待写入的字节缓冲区。</param>
		/// <param name="offset">缓冲区起始读取偏移。</param>
		/// <param name="count">写入的字节数。</param>
		public override void Write(byte[] buffer, int offset, int count)
		{
			flowAnalyzer.OnFlow(FlowType.Write, count);
			base.Write(buffer, offset, count);
		}

		/// <summary>
		/// 同步写入（基于 <see cref="ReadOnlySpan{T}"/>）：先按预期写入字节数上报，再调用底层流写入。
		/// </summary>
		/// <param name="source">待写入的数据源。</param>
		public override void Write(ReadOnlySpan<byte> source)
		{
			flowAnalyzer.OnFlow(FlowType.Write, source.Length);
			base.Write(source);
		}

		/// <summary>
		/// 异步写入：先按预期写入字节数上报，再返回底层异步写入任务。
		/// </summary>
		/// <param name="buffer">待写入的字节缓冲区。</param>
		/// <param name="offset">缓冲区起始读取偏移。</param>
		/// <param name="count">写入的字节数。</param>
		/// <param name="cancellationToken">用于取消异步写入的令牌。</param>
		/// <returns>表示底层写入操作的 <see cref="Task"/>。</returns>
		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			flowAnalyzer.OnFlow(FlowType.Write, count);
			return base.WriteAsync(buffer, offset, count, cancellationToken);
		}

		/// <summary>
		/// 异步写入（基于 <see cref="ReadOnlyMemory{T}"/>）：先按预期写入字节数上报，再返回底层异步写入任务。
		/// </summary>
		/// <param name="source">待写入的数据源。</param>
		/// <param name="cancellationToken">用于取消异步写入的令牌，默认值为 <see cref="CancellationToken.None"/>。</param>
		/// <returns>表示底层写入操作的 <see cref="ValueTask"/>。</returns>
		public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
		{
			flowAnalyzer.OnFlow(FlowType.Write, source.Length);
			return base.WriteAsync(source, cancellationToken);
		}
	}
}
