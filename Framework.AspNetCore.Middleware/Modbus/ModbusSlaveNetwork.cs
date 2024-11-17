using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Middleware.Modbus
{
	public abstract class ModbusSlaveNetwork : ModbusDevice, IModbusSlaveNetwork
	{
		private readonly ILogger<ModbusLogger> _logger;
		private readonly IDictionary<byte, IModbusSlave> _slaves = new ConcurrentDictionary<byte, IModbusSlave>();

		protected ModbusSlaveNetwork(IModbusTransport transport, IModbusFactory modbusFactory, ILogger<ModbusLogger> logger)
			: base(transport)
		{
			ModbusFactory = modbusFactory;
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		protected IModbusFactory ModbusFactory { get; }

		protected ILogger Logger
		{
			get
			{
				return _logger;
			}
		}
		/// <summary>
		/// Start slave listening for requests.
		/// </summary>
		public abstract Task ListenAsync(ConnectionContext context, ReadOnlySequence<byte> buffer);

		/// <summary>
		/// Apply the request.
		/// </summary>
		/// <param name="request"></param>
		protected IModbusMessage ApplyRequest(IModbusMessage request)
		{
			//Check for broadcast requests
			if (request.SlaveAddress == 0)
			{
				//Grab each slave
				foreach (var slave in _slaves.Values)
				{
					try
					{
						//Apply the request
						slave.ApplyRequest(request);
					}
					catch (Exception ex)
					{
						_logger.LogError($"Error applying request to slave {slave.UnitId}: {ex.Message}");
					}
				}
			}
			else
			{
				//Attempt to find a slave for this address
				IModbusSlave slave = GetSlave(request.SlaveAddress);

				// only service requests addressed to our slaves
				if (slave == null)
				{
					Console.WriteLine($"NModbus Slave Network ignoring request intended for NModbus Slave {request.SlaveAddress}");
				}
				else
				{
					// perform action
					return slave.ApplyRequest(request);
				}
			}

			return null;
		}

		public void AddSlave(IModbusSlave slave)
		{
			if (slave == null) throw new ArgumentNullException(nameof(slave));

			_slaves.Add(slave.UnitId, slave);

			_logger.LogInformation($"Slave {slave.UnitId} added to slave network.");
		}

		public void RemoveSlave(byte unitId)
		{
			_slaves.Remove(unitId);

			_logger.LogInformation($"Slave {unitId} removed from slave network.");
		}

		public IModbusSlave GetSlave(byte unitId)
		{
			return _slaves.GetValueOrDefault(unitId);
		}
	}
}
