using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Net.Middleware.Modbus
{
	/// <summary>
	///     Modbus TCP slave device.
	/// </summary>
	public interface IModbusTcpSlaveNetwork : IModbusSlaveNetwork
	{
		/// <summary>
		///     Gets the Modbus TCP Masters connected to this Modbus TCP Slave.
		/// </summary>
		ReadOnlyCollection<TcpClient> Masters { get; }
	}
}
