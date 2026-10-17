using Microsoft.Extensions.Logging;

namespace Middleware.Modbus
{
	/// <summary>
	/// Modbus/TCP 从站连接处理器（MBAP 封装，标准端口 502）
	/// </summary>
	public sealed class ModbusTcpConnectionHandler : ModbusConnectionHandler
	{
		private static readonly IModbusFrameCodec Codec = new MbapFrameCodec();

		/// <summary>
		/// Modbus/TCP 从站连接处理器
		/// </summary>
		/// <param name="slave">Modbus 从站</param>
		/// <param name="logger">日志</param>
		public ModbusTcpConnectionHandler(ModbusSlave slave, ILogger<ModbusTcpConnectionHandler> logger)
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
