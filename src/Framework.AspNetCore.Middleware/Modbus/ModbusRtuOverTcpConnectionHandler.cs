using Microsoft.Extensions.Logging;

namespace Middleware.Modbus
{
	/// <summary>
	/// Modbus RTU over TCP 从站连接处理器：串口 RTU 帧直接承载在 TCP 上（常见于串口服务器）
	/// </summary>
	public sealed class ModbusRtuOverTcpConnectionHandler : ModbusConnectionHandler
	{
		private static readonly IModbusFrameCodec Codec = new RtuFrameCodec();

		/// <summary>
		/// Modbus RTU over TCP 从站连接处理器
		/// </summary>
		/// <param name="slave">Modbus 从站</param>
		/// <param name="logger">日志</param>
		public ModbusRtuOverTcpConnectionHandler(ModbusSlave slave, ILogger<ModbusRtuOverTcpConnectionHandler> logger)
			: base(slave, logger)
		{
		}

		/// <inheritdoc/>
		protected override IModbusFrameCodec CreateCodec()
		{
			return Codec;
		}
	}
}
