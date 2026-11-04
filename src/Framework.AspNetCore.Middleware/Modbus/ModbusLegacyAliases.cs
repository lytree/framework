using System;

namespace Middleware.Modbus
{
	/// <summary>
	/// 旧版功能码常量的兼容层，取值转发到 <see cref="Framework.Modbus.Abstractions.ModbusFunctionCode"/>。
	/// </summary>
	[Obsolete("功能码已迁移到 Framework.Modbus.Abstractions.ModbusFunctionCode，请改用该类型。")]
	public static class ModbusFunctionCode
	{
		/// <summary>读线圈（可读写的数字量输出），FC01。</summary>
		public const byte ReadCoils = Framework.Modbus.Abstractions.ModbusFunctionCode.ReadCoils;

		/// <summary>读离散输入（只读的数字量输入），FC02。</summary>
		public const byte ReadDiscreteInputs = Framework.Modbus.Abstractions.ModbusFunctionCode.ReadDiscreteInputs;

		/// <summary>读保持寄存器，FC03。</summary>
		public const byte ReadHoldingRegisters = Framework.Modbus.Abstractions.ModbusFunctionCode.ReadHoldingRegisters;

		/// <summary>读输入寄存器，FC04。</summary>
		public const byte ReadInputRegisters = Framework.Modbus.Abstractions.ModbusFunctionCode.ReadInputRegisters;

		/// <summary>写单个线圈，FC05。</summary>
		public const byte WriteSingleCoil = Framework.Modbus.Abstractions.ModbusFunctionCode.WriteSingleCoil;

		/// <summary>写单个寄存器，FC06。</summary>
		public const byte WriteSingleRegister = Framework.Modbus.Abstractions.ModbusFunctionCode.WriteSingleRegister;

		/// <summary>写多个线圈，FC15。</summary>
		public const byte WriteMultipleCoils = Framework.Modbus.Abstractions.ModbusFunctionCode.WriteMultipleCoils;

		/// <summary>写多个寄存器，FC16。</summary>
		public const byte WriteMultipleRegisters = Framework.Modbus.Abstractions.ModbusFunctionCode.WriteMultipleRegisters;

		/// <summary>异常响应的功能码掩码，响应功能码 = 请求功能码 | 0x80。</summary>
		public const byte ExceptionFlag = Framework.Modbus.Abstractions.ModbusFunctionCode.ExceptionMask;
	}

	/// <summary>
	/// 旧版异常码常量的兼容层，取值转发到 <see cref="Framework.Modbus.Abstractions.ModbusExceptionCode"/>。
	/// </summary>
	[Obsolete("异常码已迁移到 Framework.Modbus.Abstractions.ModbusExceptionCode，请改用该类型。")]
	public static class ModbusExceptionCode
	{
		/// <summary>非法功能（从站不支持该功能码）。</summary>
		public const byte IllegalFunction = Framework.Modbus.Abstractions.ModbusExceptionCode.IllegalFunction;

		/// <summary>非法数据地址。</summary>
		public const byte IllegalDataAddress = Framework.Modbus.Abstractions.ModbusExceptionCode.IllegalDataAddress;

		/// <summary>非法数据值。</summary>
		public const byte IllegalDataValue = Framework.Modbus.Abstractions.ModbusExceptionCode.IllegalDataValue;

		/// <summary>从站设备故障。</summary>
		public const byte SlaveDeviceFailure = Framework.Modbus.Abstractions.ModbusExceptionCode.SlaveDeviceFailure;

		/// <summary>确认（长时间操作已被接受，正在处理中）。</summary>
		public const byte Acknowledge = Framework.Modbus.Abstractions.ModbusExceptionCode.Acknowledge;

		/// <summary>从站设备忙。</summary>
		public const byte SlaveDeviceBusy = Framework.Modbus.Abstractions.ModbusExceptionCode.SlaveDeviceBusy;

		/// <summary>存储奇偶性差错。</summary>
		public const byte MemoryParityError = Framework.Modbus.Abstractions.ModbusExceptionCode.MemoryParityError;

		/// <summary>网关路径不可用。</summary>
		public const byte GatewayPathUnavailable = Framework.Modbus.Abstractions.ModbusExceptionCode.GatewayPathUnavailable;

		/// <summary>网关目标设备未能响应。</summary>
		public const byte GatewayTargetDeviceFailedToRespond = Framework.Modbus.Abstractions.ModbusExceptionCode.GatewayTargetDeviceFailedToRespond;
	}

	/// <summary>
	/// 旧版 CRC16 的兼容层，实现转发到 <see cref="Framework.Modbus.Abstractions.ModbusCrc16"/>。
	/// </summary>
	[Obsolete("CRC16 已迁移到 Framework.Modbus.Abstractions.ModbusCrc16，请改用该类型。")]
	public static class ModbusCrc16
	{
		/// <summary>CRC 校验字段的字节长度。</summary>
		public const int Length = Framework.Modbus.Abstractions.ModbusCrc16.Length;

		/// <summary>计算 CRC16 校验值。</summary>
		/// <param name="data">参与计算的数据（从站地址 + PDU）。</param>
		public static ushort Compute(ReadOnlySpan<byte> data) => Framework.Modbus.Abstractions.ModbusCrc16.Compute(data);
	}
}
