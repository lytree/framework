using System;

namespace Framework.Modbus.Abstractions
{
	/// <summary>
	/// Modbus ASCII 帧使用的 LRC（纵向冗余校验，二进制补码）实现。
	/// </summary>
	public static class ModbusLrc
	{
		/// <summary>
		/// 计算 LRC 校验值。
		/// </summary>
		/// <param name="data">参与计算的数据（从站地址 + PDU）。</param>
		/// <returns>LRC 值。</returns>
		public static byte Compute(ReadOnlySpan<byte> data)
		{
			var sum = 0;
			foreach (var value in data)
			{
				sum += value;
			}
			return (byte)((-sum) & 0xFF);
		}
	}
}
