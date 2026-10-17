using System;

namespace Middleware.Modbus
{
	/// <summary>
	/// Modbus RTU 帧使用的 CRC-16 实现（多项式 0xA001，初值 0xFFFF）
	/// </summary>
	public static class ModbusCrc16
	{
		/// <summary>
		/// CRC 校验字段的字节长度
		/// </summary>
		public const int Length = 2;

		/// <summary>
		/// 计算 CRC16 校验值
		/// </summary>
		/// <param name="data">参与计算的数据（从站地址 + PDU）</param>
		/// <returns>CRC 值；写入帧时应使用小端（低字节在前）</returns>
		public static ushort Compute(ReadOnlySpan<byte> data)
		{
			ushort crc = 0xFFFF;
			foreach (var value in data)
			{
				crc ^= value;
				for (var i = 0; i < 8; i++)
				{
					crc = (crc & 0x0001) != 0
						? (ushort)((crc >> 1) ^ 0xA001)
						: (ushort)(crc >> 1);
				}
			}
			return crc;
		}
	}
}
