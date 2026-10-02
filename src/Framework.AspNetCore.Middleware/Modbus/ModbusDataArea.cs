namespace Middleware.Modbus
{
	/// <summary>
	/// Modbus 从站的四类数据区
	/// </summary>
	public enum ModbusDataArea
	{
		/// <summary>
		/// 线圈（FC01 读 / FC05、FC15 写），总线侧可读写
		/// </summary>
		Coil = 0,

		/// <summary>
		/// 离散输入（FC02 读），总线侧只读
		/// </summary>
		DiscreteInput = 1,

		/// <summary>
		/// 保持寄存器（FC03 读 / FC06、FC16 写），总线侧可读写
		/// </summary>
		HoldingRegister = 2,

		/// <summary>
		/// 输入寄存器（FC04 读），总线侧只读
		/// </summary>
		InputRegister = 3,
	}

	/// <summary>
	/// 进程映像写入的来源，用于区分总线请求与进程内直接赋值
	/// </summary>
	public enum ModbusWriteSource
	{
		/// <summary>
		/// 来自 Modbus 总线请求（客户端下发）
		/// </summary>
		Bus = 0,

		/// <summary>
		/// 来自进程内直接赋值（<see cref="ModbusSlave"/> 上的 Set 系列方法）
		/// </summary>
		Internal = 1,
	}
}
