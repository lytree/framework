using Microsoft.Extensions.Logging;
using System.IO.Pipelines;

namespace Middleware.FlowXor
{
	sealed class XorDuplexPipe : DelegatingDuplexPipe<XorStream>
	{
		public XorDuplexPipe(IDuplexPipe duplexPipe, ILogger logger) :
			base(duplexPipe, stream => new XorStream(stream, logger))
		{
		}
	}
}
