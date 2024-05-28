using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Net.Middleware.Modbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Hosting
{
	public partial class ListenOptionsExtensions
	{
		/// <summary>
		/// 使用EchoConnectionHandler
		/// </summary>
		/// <param name="listen"></param>
		public static void UseModbus(this ListenOptions listen)
		{
			
			listen.UseConnectionHandler<ModbusConnectionHandler>();
		}
	}
}
