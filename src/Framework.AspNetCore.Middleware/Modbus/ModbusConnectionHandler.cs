using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Middleware.Telnet;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Net.Middleware.Modbus;

public class ModbusConnectionHandler : ConnectionHandler
{

	private readonly ILogger<ModbusConnectionHandler> _logger;


	public ModbusConnectionHandler()
	{
		_logger = NullLogger<ModbusConnectionHandler>.Instance;
	}
	public override async Task OnConnectedAsync(ConnectionContext context)
	{
		var input = context.Transport.Input;
		
		while (context.ConnectionClosed.IsCancellationRequested == false)
		{
			var result = await input.ReadAsync();
			if (TryReadRequest(result))
			{
				//input.AdvanceTo(consumed);

			}
			else
			{
				input.AdvanceTo(result.Buffer.Start, result.Buffer.End);
			}

			if (result.IsCompleted)
			{
				break;
			}
			if (result.IsCanceled)
			{
				break;
			}


			if (result.IsCompleted)
			{
				break;
			}
		}
	}
	private static bool TryReadRequest(ReadResult result)
	{
		var reader = new SequenceReader<byte>(result.Buffer);
		reader.TryRead(out byte span);

		//if (reader.TryReadTo(out ReadOnlySpan<byte> span, 2))
		//{
		//	request = Encoding.UTF8.GetString(span);
		//	consumed = reader.Position;
		//	return true;
		//}
		//else
		//{
		//	request = string.Empty;
		//	consumed = result.Buffer.Start;
		//	return false;
		//}
		return true;
	}
}