using System.Buffers;
using System;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

namespace Middleware.Telnet
{
	/// <summary>
	/// Telnet 应用的响应写入封装。<br/>
	/// 包装底层 Kestrel 管道的 <see cref="PipeWriter"/>，提供按行写入（自动追加 <c>CRLF</c>）与手动 Flush 的便捷方法，
	/// 供 <see cref="TelnetContext.Response"/> 暴露给应用中间件使用。
	/// </summary>
	sealed class TelnetResponse
	{
		private readonly PipeWriter writer;

		/// <summary>
		/// 使用给定的底层 <see cref="PipeWriter"/> 构造 Telnet 响应封装。
		/// </summary>
		/// <param name="writer">底层 Kestrel 管道的 <see cref="PipeWriter"/>，通常取自 <c>ConnectionContext.Transport.Output</c>。</param>
		public TelnetResponse(PipeWriter writer)
		{
			this.writer = writer;
		}

		/// <summary>
		/// 以一行文本形式写入并立即刷新到对端。<br/>
		/// 等价于先调用 <see cref="WriteLine"/> 再调用 <see cref="FlushAsync"/>。
		/// </summary>
		/// <param name="text">要写入的文本内容（不含行结束符）。</param>
		/// <param name="encoding">字符编码，默认值为 <see cref="Encoding.UTF8"/>。</param>
		/// <returns>表示底层刷新操作的 <see cref="ValueTask{TResult}"/>，结果包含管道是否已结束等信息。</returns>
		public ValueTask<FlushResult> WriteLineAsync(ReadOnlySpan<char> text, Encoding? encoding = null)
		{
			WriteLine(text, encoding);
			return FlushAsync();
		}

		/// <summary>
		/// 以一行文本形式写入（追加 <c>CRLF</c>），但不立即刷新；返回当前对象以支持链式调用。
		/// </summary>
		/// <param name="text">要写入的文本内容（不含行结束符）。</param>
		/// <param name="encoding">字符编码，默认值为 <see cref="Encoding.UTF8"/>。</param>
		/// <returns>当前 <see cref="TelnetResponse"/> 实例，便于链式调用。</returns>
		public TelnetResponse WriteLine(ReadOnlySpan<char> text, Encoding? encoding = null)
		{
			writer.Write(text, encoding ?? Encoding.UTF8);
			writer.WriteCRLF();
			return this;
		}

		/// <summary>
		/// 将已写入管道的字节异步刷新到对端。
		/// </summary>
		/// <returns>表示底层刷新操作的 <see cref="ValueTask{TResult}"/>，结果包含管道是否已结束等信息。</returns>
		public ValueTask<FlushResult> FlushAsync()
		{
			return writer.FlushAsync();
		}
	}
}
