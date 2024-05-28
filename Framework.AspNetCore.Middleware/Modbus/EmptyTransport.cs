using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Middleware.Modbus
{
	internal class EmptyTransport : ModbusTransport
	{
		public EmptyTransport(IModbusFactory modbusFactory)
			: base(modbusFactory, NullLogger<ModbusLogger>.Instance)
		{
		}

		public override byte[] ReadRequest()
		{
			throw new NotImplementedException();
		}

		public override IModbusMessage ReadResponse<T>()
		{
			throw new NotImplementedException();
		}

		public override byte[] BuildMessageFrame(IModbusMessage message)
		{
			throw new NotImplementedException();
		}

		public override void Write(IModbusMessage message)
		{
			throw new NotImplementedException();
		}

		internal override void OnValidateResponse(IModbusMessage request, IModbusMessage response)
		{
			throw new NotImplementedException();
		}




	}
}
