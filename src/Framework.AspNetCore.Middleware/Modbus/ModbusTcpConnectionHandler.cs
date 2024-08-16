using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Middleware.Telnet;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace AspNetCore.Middleware.Modbus;

public class ModbusTcpConnectionHandler : ConnectionHandler
{

	private readonly ILogger<ModbusTcpConnectionHandler> _logger;

	private readonly IModbusSlaveNetwork _modbusSlaveNetwork;
	public ModbusTcpConnectionHandler(ModbusTcpSlaveNetwork modbusSlaveNetwork,ILogger<ModbusTcpConnectionHandler> logger)
	{
		_modbusSlaveNetwork = _modbusSlaveNetwork?? throw new ArgumentNullException(nameof(modbusSlaveNetwork));
		_logger = logger?? NullLogger<ModbusTcpConnectionHandler>.Instance;
	}
	public override async Task OnConnectedAsync(ConnectionContext context)
	{
		try
		{
			await HandleRequestsAsync(context);
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex.Message);
		}
		finally
		{
			await context.DisposeAsync();
		}
	}

	private async Task HandleRequestsAsync(ConnectionContext context)
	{
		var input = context.Transport.Input;
		var ouput = context.Transport.Output;
		while (context.ConnectionClosed.IsCancellationRequested == false)
		{
			var result = await input.ReadAsync();
			if (result.IsCanceled)
			{
				break;
			}

			var response = _modbusSlaveNetwork.HandlerRequest(context, result.Buffer, out var consumed);
			if (!consumed.IsDefaultValue())
			{
				await ouput.WriteAsync(response);
				await ouput.FlushAsync();
				input.AdvanceTo(consumed);
			}
			else
			{
				input.AdvanceTo(result.Buffer.Start, result.Buffer.End);
			}

			if (result.IsCompleted)
			{
				break;
			}
		}

	}
}