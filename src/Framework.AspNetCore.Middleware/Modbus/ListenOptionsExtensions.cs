using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using AspNetCore.Middleware.Modbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Hosting
{
	public partial class ListenOptionsExtensions
	{
		/// <summary>
		/// 使用EchoConnectionHandler
		/// </summary>
		/// <param name="listen"></param>
		public static void UseModbusTcp(this ListenOptions listen)
		{
			var service = listen.ApplicationServices;
			var loggerFactory = service.GetService<ILoggerFactory>()??NullLoggerFactory.Instance;
			var factory = new ModbusFactory(loggerFactory: loggerFactory);
			var slave = factory.CreateSlaveNetwork();

			for (byte i = 0; i < 255; i++) {
				var data = factory.CreateSlave(i);
				slave.AddSlave(data);
			}

			listen.UseConnectionHandler<ModbusTcpConnectionHandler>();
		}
		/// <summary>
		/// 使用EchoConnectionHandler
		/// </summary>
		/// <param name="listen"></param>
		public static void UseModbusRtuOverTcp(this ListenOptions listen)
		{
			var service = listen.ApplicationServices;
			var loggerFactory = service.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
			var factory = new ModbusFactory(loggerFactory: loggerFactory);
			var slave = factory.CreateSlaveNetwork();

			for (byte i = 0; i < 255; i++)
			{
				var data = factory.CreateSlave(i);
				slave.AddSlave(data);
			}

			listen.UseConnectionHandler<ModbusTcpConnectionHandler>();
		}
	}
}
