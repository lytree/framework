using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging.Abstractions;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;


namespace AspNetCore.Middleware.Modbus;

public class ModbusRtuOverTcpConnectionHandler : ConnectionHandler
{
	private readonly ILogger<ModbusRtuOverTcpConnectionHandler> _logger;
	public ModbusRtuOverTcpConnectionHandler()
	{
		_logger = NullLogger<ModbusRtuOverTcpConnectionHandler>.Instance;
	}
	public override async Task OnConnectedAsync(ConnectionContext context)
	{
		var input = context.Transport.Input;
		var output = context.Transport.Output;
		while (context.ConnectionClosed.IsCancellationRequested == false)
		{
			var result = await input.ReadAsync();
			if (result.IsCanceled)
			{
				break;
			}
			if (TryReadModbusRequest(result, out ReadOnlySequence<byte> mbapHeader, out ReadOnlySequence<byte> messageFrame, out var consumed))
			{
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
	private static bool TryReadModbusRequest(ReadResult result, out ReadOnlySequence<byte> mbapHeader, out ReadOnlySequence<byte> messageFrame, out SequencePosition consumed)
	{
		var reader = result.Buffer;
		// 检查是否有足够的数据用于解析
		if (reader.Length >= 6) // 最小帧长
		{
			mbapHeader = reader.Slice(0, 6);
			short frameLength = IPAddress.HostToNetworkOrder(BitConverter.ToInt16(mbapHeader.ToArray(), 4));
			messageFrame = reader.Slice(6);
			consumed = reader.GetPosition(6 + frameLength);
			return true;
		}
		else
		{
			consumed = default;
			mbapHeader = default;
			messageFrame = default;
			return false; // 等待更多数据
		}

	}
}
