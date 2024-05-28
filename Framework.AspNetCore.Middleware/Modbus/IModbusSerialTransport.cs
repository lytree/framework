using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Middleware.Modbus
{
	public interface IModbusSerialTransport : IModbusTransport
	{
		void DiscardInBuffer();

		bool CheckFrame { get; set; }

		bool ChecksumsMatch(IModbusMessage message, byte[] messageFrame);

		void IgnoreResponse();
	}
}
