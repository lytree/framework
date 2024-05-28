using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Net.Middleware.Modbus.Data;
using Net.Middleware.Modbus.MessageHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Middleware.Modbus
{
	public class ModbusFactory : IModbusFactory
	{
		/// <summary>
		/// The "built-in" message handlers.
		/// </summary>
		private static readonly IModbusFunctionService[] BuiltInFunctionServices =
		{
			new ReadCoilsService(),
			new ReadInputsService(),
			new ReadHoldingRegistersService(),
			new ReadInputRegistersService(),
			new DiagnosticsService(),
			new WriteSingleCoilService(),
			new WriteSingleRegisterService(),
			new WriteMultipleCoilsService(),
			new WriteMultipleRegistersService(),
			new WriteFileRecordService(),
			new ReadWriteMultipleRegistersService(),
		};

		private readonly IDictionary<byte, IModbusFunctionService> _functionServices;

		public ILogger<ModbusLogger> Logger { get; }
		/// <summary>
		/// Create a factory which uses the built in standard slave function handlers.
		/// </summary>
		public ModbusFactory()
		{
			_functionServices = BuiltInFunctionServices.ToDictionary(s => s.FunctionCode, s => s);

			Logger = NullLogger<ModbusLogger>.Instance;
		}

		/// <summary>
		/// Create a factory which optionally uses the built in function services and allows custom services to be added.
		/// </summary>
		/// <param name="functionServices">User provided function services.</param>
		/// <param name="includeBuiltIn">If true, the built in function services are included. Otherwise, all function services will come from the functionService parameter.</param>
		/// <param name="logger">Logger</param>
		public ModbusFactory(
			IEnumerable<IModbusFunctionService> functionServices = null,
			bool includeBuiltIn = true,
			ILogger<ModbusLogger> logger = null)
		{
			Logger = logger ?? NullLogger<ModbusLogger>.Instance;

			//Determine if we're including the built in services
			if (includeBuiltIn)
			{
				//Make a dictionary out of the built in services
				_functionServices = BuiltInFunctionServices
					.ToDictionary(s => s.FunctionCode, s => s);
			}
			else
			{
				//Create an empty dictionary
				_functionServices = new Dictionary<byte, IModbusFunctionService>();
			}

			if (functionServices != null)
			{
				//Add and replace the provided function services as necessary.
				foreach (IModbusFunctionService service in functionServices)
				{
					//This will add or replace the service.
					_functionServices[service.FunctionCode] = service;
				}
			}
		}

		//public IModbusSlave CreateSlave(byte unitId, ISlaveDataStore dataStore = null)
		//{
		//	if (dataStore == null)
		//		dataStore = new DefaultSlaveDataStore();

		//	return new ModbusSlave(unitId, dataStore, GetAllFunctionServices());
		//}

		//public IModbusSlaveNetwork CreateSlaveNetwork(IModbusRtuTransport transport)
		//{
		//	return new ModbusSerialSlaveNetwork(transport, this, Logger);
		//}

		//public IModbusSlaveNetwork CreateSlaveNetwork(IModbusAsciiTransport transport)
		//{
		//	return new ModbusSerialSlaveNetwork(transport, this, Logger);
		//}

		//public IModbusTcpSlaveNetwork CreateSlaveNetwork(TcpListener tcpListener)
		//{
		//	return new ModbusTcpSlaveNetwork(tcpListener, this, Logger);
		//}

		//public IModbusSlaveNetwork CreateSlaveNetwork(UdpClient client)
		//{
		//	return new ModbusUdpSlaveNetwork(client, this, Logger);
		//}

		//public IModbusRtuTransport CreateRtuTransport(IStreamResource streamResource)
		//{
		//	return new ModbusRtuTransport(streamResource, this, Logger);
		//}

		//public IModbusAsciiTransport CreateAsciiTransport(IStreamResource streamResource)
		//{
		//	return new ModbusAsciiTransport(streamResource, this, Logger);
		//}

		//public IModbusTransport CreateIpTransport(IStreamResource streamResource)
		//{
		//	return new ModbusIpTransport(streamResource, this, Logger);
		//}


		public IModbusFunctionService[] GetAllFunctionServices()
		{
			return _functionServices
				.Values
				.ToArray();
		}
		public IModbusFunctionService GetFunctionService(byte functionCode)
		{
			return _functionServices.GetValueOrDefault(functionCode);
		}

		public IModbusSlave CreateSlave(byte unitId, ISlaveDataStore dataStore = null)
		{
			throw new NotImplementedException();
		}

		public IModbusSlaveNetwork CreateSlaveNetwork(IModbusRtuTransport transport)
		{
			throw new NotImplementedException();
		}

		public IModbusSlaveNetwork CreateSlaveNetwork(IModbusAsciiTransport transport)
		{
			throw new NotImplementedException();
		}
	}
}
