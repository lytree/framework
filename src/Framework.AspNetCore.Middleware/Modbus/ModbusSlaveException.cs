using System;

namespace Middleware.Modbus
{
	/// <summary>
	/// 表示从站在处理请求时产生的 Modbus 协议异常，携带一个异常响应码
	/// </summary>
	public sealed class ModbusSlaveException : Exception
	{
		/// <summary>
		/// 获取 Modbus 异常码
		/// </summary>
		public byte ExceptionCode { get; }

		/// <summary>
		/// 从站协议异常
		/// </summary>
		/// <param name="exceptionCode">Modbus 异常码</param>
		/// <param name="message">描述</param>
		public ModbusSlaveException(byte exceptionCode, string? message = null)
			: base(message ?? $"Modbus 异常，异常码 0x{exceptionCode:X2}")
		{
			this.ExceptionCode = exceptionCode;
		}

		/// <summary>
		/// 非法功能
		/// </summary>
		/// <param name="functionCode">功能码</param>
		/// <returns></returns>
		public static ModbusSlaveException IllegalFunction(byte functionCode)
		{
			return new ModbusSlaveException(ModbusExceptionCode.IllegalFunction, $"从站不支持功能码 0x{functionCode:X2}");
		}

		/// <summary>
		/// 非法数据地址
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="count">数量</param>
		/// <returns></returns>
		public static ModbusSlaveException IllegalDataAddress(int address, int count = 0)
		{
			var message = count > 0
				? $"非法的数据地址 {address}，数量 {count}"
				: $"非法的数据地址 {address}";
			return new ModbusSlaveException(ModbusExceptionCode.IllegalDataAddress, message);
		}

		/// <summary>
		/// 非法数据值
		/// </summary>
		/// <param name="message">描述</param>
		/// <returns></returns>
		public static ModbusSlaveException IllegalDataValue(string message)
		{
			return new ModbusSlaveException(ModbusExceptionCode.IllegalDataValue, message);
		}

		/// <summary>
		/// PDU 长度不足
		/// </summary>
		/// <param name="functionCode">功能码</param>
		/// <param name="expected">期望的最小数据域长度</param>
		/// <param name="actual">实际的数据域长度</param>
		/// <returns></returns>
		public static ModbusSlaveException TruncatedPdu(byte functionCode, int expected, int actual)
		{
			return IllegalDataValue($"功能码 0x{functionCode:X2} 的数据域长度至少为 {expected}，实际为 {actual}");
		}
	}
}
