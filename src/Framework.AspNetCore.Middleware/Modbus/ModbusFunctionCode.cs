namespace Middleware.Modbus
{
	/// <summary>
	/// Modbus 功能码定义
	/// </summary>
	public static class ModbusFunctionCode
	{
		/// <summary>
		/// 读线圈（可读写的数字量输出），FC01
		/// </summary>
		public const byte ReadCoils = 0x01;

		/// <summary>
		/// 读离散输入（只读的数字量输入），FC02
		/// </summary>
		public const byte ReadDiscreteInputs = 0x02;

		/// <summary>
		/// 读保持寄存器，FC03
		/// </summary>
		public const byte ReadHoldingRegisters = 0x03;

		/// <summary>
		/// 读输入寄存器，FC04
		/// </summary>
		public const byte ReadInputRegisters = 0x04;

		/// <summary>
		/// 写单个线圈，FC05
		/// </summary>
		public const byte WriteSingleCoil = 0x05;

		/// <summary>
		/// 写单个寄存器，FC06
		/// </summary>
		public const byte WriteSingleRegister = 0x06;

		/// <summary>
		/// 写多个线圈，FC15
		/// </summary>
		public const byte WriteMultipleCoils = 0x0F;

		/// <summary>
		/// 写多个寄存器，FC16
		/// </summary>
		public const byte WriteMultipleRegisters = 0x10;

		/// <summary>
		/// 异常响应的功能码掩码，响应功能码 = 请求功能码 | 0x80
		/// </summary>
		public const byte ExceptionFlag = 0x80;
	}
}
