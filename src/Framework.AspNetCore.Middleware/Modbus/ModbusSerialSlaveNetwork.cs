using Microsoft.AspNetCore.Connections;
using System.Buffers;

namespace AspNetCore.Middleware.Modbus;

public class ModbusSerialSlaveNetwork : ModbusSlaveNetwork
{
	public ModbusSerialSlaveNetwork(IModbusFactory modbusFactory, ILoggerFactory logger)
		: base(new EmptyTransport(modbusFactory), modbusFactory, logger)
	{

	}
	private IModbusSerialTransport SerialTransport => _serialTransport;

	public override byte[] HandlerResquest(ConnectionContext context, ReadOnlySequence<byte> buffer, out SequencePosition consumed)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				// read request and build message
				byte[] frame = SerialTransport.ReadRequest();

				//Create the request
				IModbusMessage request = ModbusFactory.CreateModbusRequest(frame);

				//Check the message
				if (SerialTransport.CheckFrame && !SerialTransport.ChecksumsMatch(request, frame))
				{
					string msg = $"Checksums failed to match {string.Join(", ", request.MessageFrame)} != {string.Join(", ", frame)}.";
					Logger.LogWarning(msg);
					throw new IOException(msg);
				}

				//Apply the request
				IModbusMessage response = ApplyRequest(request);

				if (response == null)
				{
					_serialTransport.IgnoreResponse();
				}
				else
				{
					Transport.Write(response);
				}
			}
			catch (IOException ioe)
			{
				Logger.Warning($"IO Exception encountered while listening for requests - {ioe.Message}");
				SerialTransport.DiscardInBuffer();
			}
			catch (TimeoutException te)
			{
				Logger.Trace($"Timeout Exception encountered while listening for requests - {te.Message}");
				SerialTransport.DiscardInBuffer();
			}
			catch (InvalidOperationException)
			{
				// when the underlying transport is disposed
				break;
			}
			catch (Exception ex)
			{
				Logger.Error($"{GetType()}: {ex.Message}");
				SerialTransport.DiscardInBuffer();
			}
		}

		return Task.FromResult(0);
	}
}
}
