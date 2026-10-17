using System;

namespace Middleware.Modbus
{
	/// <summary>
	/// 表示 ADU 帧结构错误（如 RTU 的 CRC 校验失败、MBAP 头非法）。
	/// 抛出时该帧已被消费，调用方记录日志后继续读取下一帧即可。
	/// </summary>
	public sealed class ModbusFrameException : Exception
	{
		/// <summary>
		/// 帧结构错误
		/// </summary>
		/// <param name="message">描述</param>
		public ModbusFrameException(string message)
			: base(message)
		{
		}
	}
}
