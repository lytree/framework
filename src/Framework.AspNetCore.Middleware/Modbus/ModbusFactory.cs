
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using AspNetCore.Middleware.Modbus.Data;
using AspNetCore.Middleware.Modbus.MessageHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using AspNetCore.Middleware.Modbus.Device;
using AspNetCore.Middleware.Modbus.IO;

namespace AspNetCore.Middleware.Modbus
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

		public ILoggerFactory LoggerFactory { get; }


		public ILogger<IModbusFactory> Logger { get; }
		/// <summary>
		/// Create a factory which uses the built in standard slave function handlers.
		/// </summary>
		public ModbusFactory()
		{
			_functionServices = BuiltInFunctionServices.ToDictionary(s => s.FunctionCode, s => s);

			LoggerFactory = NullLoggerFactory.Instance;
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
			ILoggerFactory loggerFactory = null)
		{
			Logger = loggerFactory == null ? NullLoggerFactory.Instance.CreateLogger<ModbusFactory>() : loggerFactory.CreateLogger<ModbusFactory>();

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

		public IModbusSlave CreateSlave(byte unitId, ISlaveDataStore dataStore = null)
		{
			if (dataStore == null)
				dataStore = new DefaultSlaveDataStore();

			return new ModbusSlave(unitId, dataStore, GetAllFunctionServices());
		}


		public IModbusTcpSlaveNetwork CreateSlaveNetwork()
		{
			return new ModbusTcpSlaveNetwork(this, LoggerFactory);
		}


		public IModbusRtuTransport CreateRtuTransport(IStreamResource streamResource)
		{
			return new ModbusRtuTransport(streamResource, this, LoggerFactory);
		}


		public IModbusTransport CreateIpTransport(IStreamResource streamResource)
		{
			return new ModbusIpTransport(streamResource, this, LoggerFactory);
		}


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

	}
}
