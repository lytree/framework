using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCore.Middleware.Modbus
{
	/// <summary>
	/// Container for modbus function services.
	/// </summary>
	public interface IModbusFactory
	{
		/// <summary>
		/// Get the service for a given function code.
		/// </summary>
		/// <param name="functionCode"></param>
		/// <returns></returns>
		IModbusFunctionService GetFunctionService(byte functionCode);

		/// <summary>
		/// Gets all of the services.
		/// </summary>
		/// <returns></returns>
		IModbusFunctionService[] GetAllFunctionServices();

		#region Slave

		/// <summary>
		/// Creates a Modbus Slave.
		/// </summary>
		/// <param name="unitId">The address of this slave on the Modbus network.</param>
		/// <param name="dataStore">Optionally specify a custom data store for the created slave.</param>
		/// <returns></returns>
		IModbusSlave CreateSlave(byte unitId, ISlaveDataStore dataStore = null);

		#endregion


		ILoggerFactory LoggerFactory { get; }
	}
}
