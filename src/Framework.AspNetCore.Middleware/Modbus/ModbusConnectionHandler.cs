using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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

	private readonly ModbusSlaveNetwork _modbusSlave;

	public ModbusConnectionHandler(ModbusSlaveNetwork modbusSlave)
	{
		_modbusSlave = modbusSlave;
		_logger = NullLogger<ModbusConnectionHandler>.Instance;
	}
	public override async Task OnConnectedAsync(ConnectionContext context)
	{
		var input = context.Transport.Input;
		
		while (context.ConnectionClosed.IsCancellationRequested == false)
		{
			var result = await input.ReadAsync();
			if (result.IsCanceled)
			{
				break;
			}

			await _modbusSlave.ListenAsync(context,result.Buffer);


			if (result.IsCompleted)
			{
				break;
			}
		}
	}
}