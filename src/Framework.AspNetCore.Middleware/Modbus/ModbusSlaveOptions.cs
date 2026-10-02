namespace Middleware.Modbus
{
	/// <summary>
	/// Modbus 从站选项
	/// </summary>
	public sealed class ModbusSlaveOptions
	{
		/// <summary>
		/// 获取或设置从站地址（单元标识）。
		/// 1~247 为常规从站地址；设为 0 表示不校验地址，接受任意单元标识
		/// </summary>
		public byte UnitId { get; set; } = 1;

		/// <summary>
		/// 获取或设置收到广播（单元标识 0）时是否抑制响应。
		/// 串行链路（RTU / ASCII）必须为 true，否则会破坏总线上的其他从站
		/// </summary>
		public bool SuppressBroadcastResponse { get; set; } = true;
	}
}
