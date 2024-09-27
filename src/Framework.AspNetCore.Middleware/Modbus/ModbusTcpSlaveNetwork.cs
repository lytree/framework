using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Net.Middleware.Modbus
{

	/// <summary>
	///     Modbus TCP slave device.
	/// </summary>
	public class ModbusTcpSlaveNetwork : ModbusSlaveNetwork, IModbusTcpSlaveNetwork
	{

		public ModbusTcpSlaveNetwork(IModbusFactory modbusFactory, ILogger<ModbusLogger> logger)
			: base(new EmptyTransport(modbusFactory), modbusFactory, logger)
		{

		}

		public ReadOnlyCollection<TcpClient> Masters => throw new NotImplementedException();


		public override async Task ListenAsync(ConnectionContext context, ReadOnlySequence<byte> buffer)
		{
			try
			{
				var mbapHeader = buffer.Slice(0, 6).ToArray();
				if (mbapHeader.Length != 6)
				{
					Logger.LogDebug("0 bytes read, Master at {EndPoint} has closed Socket connection.", context.RemoteEndPoint);
					return;
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
					await context.Transport.Output.WriteAsync(responseFrame);
				}
			}
			catch (Exception ex)
			{
				Logger.LogWarning("{Name} occured with Master at {EndPoint}. Closing connection.", ex.GetType().Name, context.RemoteEndPoint);
				return;
			}
		}

	}
}
