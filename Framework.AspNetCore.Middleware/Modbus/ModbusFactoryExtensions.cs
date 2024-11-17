using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Middleware.Modbus
{
	internal static class ModbusFactoryExtensions
	{
		private const int MinRequestFrameLength = 3;

		public static IModbusMessage CreateModbusRequest(this IModbusFactory factory, byte[] frame)
		{
			if (frame.Length < MinRequestFrameLength)
			{
				string msg = $"Argument 'frame' must have a length of at least {MinRequestFrameLength} bytes.";
				throw new FormatException(msg);
			}

			byte functionCode = frame[1];

			var functionService = factory.GetFunctionService(functionCode);

			return functionService.CreateRequest(frame);
		}

		public static IModbusFunctionService GetFunctionServiceOrThrow(this IModbusFactory factory, byte functionCode)
		{
			IModbusFunctionService service = factory.GetFunctionService(functionCode);

			if (service == null)
			{
				string msg = $"Function code {functionCode} not supported.";
				factory.Logger.LogWarning(msg);

				throw new NotImplementedException(msg);
			}

			return service;
		}
	}
}
