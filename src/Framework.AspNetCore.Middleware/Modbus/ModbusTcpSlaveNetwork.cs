using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCore.Middleware.Modbus
{

	/// <summary>
	///     Modbus TCP slave device.
	/// </summary>
	public class ModbusTcpSlaveNetwork : ModbusSlaveNetwork, IModbusTcpSlaveNetwork
	{

		public ModbusTcpSlaveNetwork(IModbusFactory modbusFactory, ILoggerFactory logger)
			: base(new EmptyTransport(modbusFactory), modbusFactory, logger)
		{

		}


		public override byte[] HandlerRequest(ConnectionContext context, ReadOnlySequence<byte> buffer, out SequencePosition consumed)
		{
			try
			{
				var mbapHeader = buffer.Slice(0, 6).ToArray();
				if (mbapHeader.Length != 6)
				{
					Logger.LogDebug("0 bytes read, Master at {EndPoint} has closed Socket connection.", context.RemoteEndPoint);
					consumed = default;
					return Array.Empty<byte>();
				}
				ushort frameLength = (ushort)IPAddress.HostToNetworkOrder(BitConverter.ToInt16(mbapHeader.AsSpan()[4..6]));
				var messageFrame = buffer.Slice(6, frameLength).ToArray();
				if (messageFrame.Length != frameLength)
				{
					Logger.LogDebug("0 bytes read, Master at {EndPoint} has closed Socket connection.", context.RemoteEndPoint);
				}
				byte[] frame = mbapHeader.Concat(messageFrame).ToArray();
				var request = ModbusFactory.CreateModbusRequest(messageFrame);
				request.TransactionId = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(frame, 0));

				IModbusSlave slave = GetSlave(request.SlaveAddress);

				if (slave != null)
				{
					//TODO: Determine if this is appropriate

					// perform action and build response
					IModbusMessage response = slave.ApplyRequest(request);
					response.TransactionId = request.TransactionId;

					// write response
					byte[] responseFrame = Transport.BuildMessageFrame(response);
					Logger.LogInformation("TX to Master at {EndPoint}: {responseFrame}", context.RemoteEndPoint, string.Join(", ", responseFrame));
					consumed = buffer.GetPosition(frame.Length);
					return responseFrame;
				}
				consumed = default;
				return Array.Empty<byte>();

			}
			catch (Exception ex)
			{
				Logger.LogWarning("{Name} occured with Master at {EndPoint}. Closing connection.", ex.GetType().Name, context.RemoteEndPoint);
				consumed = default;
				return Array.Empty<byte>();
			}
		}

	}
}
