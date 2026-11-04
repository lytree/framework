namespace Framework.Modbus.Abstractions
{
	/// <summary>
	/// Modbus 异常码定义（主站与从站共用）。
	/// </summary>
	public static class ModbusExceptionCode
	{
		/// <summary>非法功能（从站不支持该功能码）。</summary>
		public const byte IllegalFunction = 0x01;

		/// <summary>非法数据地址。</summary>
		public const byte IllegalDataAddress = 0x02;

		/// <summary>非法数据值。</summary>
		public const byte IllegalDataValue = 0x03;

		/// <summary>从站设备故障。</summary>
		public const byte SlaveDeviceFailure = 0x04;

		/// <summary>确认（长时间操作已被接受，正在处理中）。</summary>
		public const byte Acknowledge = 0x05;

		/// <summary>从站设备忙。</summary>
		public const byte SlaveDeviceBusy = 0x06;

		/// <summary>存储奇偶性差错。</summary>
		public const byte MemoryParityError = 0x08;

		/// <summary>网关路径不可用。</summary>
		public const byte GatewayPathUnavailable = 0x0A;

		/// <summary>网关目标设备未能响应。</summary>
		public const byte GatewayTargetDeviceFailedToRespond = 0x0B;

		/// <summary>取异常码的可读描述。</summary>
		/// <param name="exceptionCode">异常码。</param>
		public static string GetMessage(byte exceptionCode) => exceptionCode switch
		{
			IllegalFunction => "Illegal function",
			IllegalDataAddress => "Illegal data address",
			IllegalDataValue => "Illegal data value",
			SlaveDeviceFailure => "Slave device failure",
			Acknowledge => "Acknowledge",
			SlaveDeviceBusy => "Slave device busy",
			MemoryParityError => "Memory parity error",
			GatewayPathUnavailable => "Gateway path unavailable",
			GatewayTargetDeviceFailedToRespond => "Gateway target device failed to respond",
			_ => "Unknown exception code",
		};
	}
}
