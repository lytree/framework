using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Middleware.Modbus;

namespace Microsoft.AspNetCore.Hosting
{
	/// <summary>
	/// ListenOptions扩展
	/// </summary>
	public static partial class ListenOptionsExtensions
	{
		/// <summary>
		/// 使用Modbus/TCP从站（MBAP封装，标准端口502）
		/// </summary>
		/// <remarks>
		/// 需要先通过 <c>services.AddModbusSlave()</c> 注册从站与进程映像
		/// </remarks>
		/// <param name="listen"></param>
		/// <returns></returns>
		public static ListenOptions UseModbusTcp(this ListenOptions listen)
		{
			listen.UseConnectionHandler<ModbusTcpConnectionHandler>();
			return listen;
		}

		/// <summary>
		/// 使用Modbus RTU over TCP从站（串口RTU帧直接承载在TCP上）
		/// </summary>
		/// <remarks>
		/// 需要先通过 <c>services.AddModbusSlave()</c> 注册从站与进程映像
		/// </remarks>
		/// <param name="listen"></param>
		/// <returns></returns>
		public static ListenOptions UseModbusRtuOverTcp(this ListenOptions listen)
		{
			listen.UseConnectionHandler<ModbusRtuOverTcpConnectionHandler>();
			return listen;
		}
	}
}
