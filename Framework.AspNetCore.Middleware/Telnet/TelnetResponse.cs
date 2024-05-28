using System.Buffers;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

namespace Middleware.Telnet
{
	sealed class TelnetResponse
	{
		private readonly PipeWriter writer;

		public TelnetResponse(PipeWriter writer)
		{
			this.writer = writer;
		}

		public ValueTask<FlushResult> WriteLineAsync(ReadOnlySpan<char> text, Encoding? encoding = null)
		{
			WriteLine(text, encoding);
			return FlushAsync();
		}

		public TelnetResponse WriteLine(ReadOnlySpan<char> text, Encoding? encoding = null)
		{
			writer.Write(text, encoding ?? Encoding.UTF8);
			writer.WriteCRLF();
			return this;
		}

		public ValueTask<FlushResult> FlushAsync()
		{
			return writer.FlushAsync();
		}
	}
}
