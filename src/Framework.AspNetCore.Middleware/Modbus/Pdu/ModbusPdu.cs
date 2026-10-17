using System;

namespace Middleware.Modbus
{
	/// <summary>
	/// 表示 Modbus 协议数据单元（PDU）：功能码 + 数据域，不含传输层封装
	/// </summary>
	public abstract class ModbusPdu
	{
		/// <summary>
		/// 获取功能码
		/// </summary>
		public abstract byte FunctionCode { get; }

		/// <summary>
		/// 获取 PDU 编码后的字节长度（含功能码本身）
		/// </summary>
		public abstract int Length { get; }

		/// <summary>
		/// 把 PDU 编码到目标缓冲区
		/// </summary>
		/// <param name="destination">目标缓冲区，长度不得小于 <see cref="Length"/></param>
		public abstract void Encode(Span<byte> destination);

		/// <summary>
		/// 把 PDU 编码为字节数组
		/// </summary>
		/// <returns></returns>
		public byte[] ToByteArray()
		{
			var buffer = new byte[this.Length];
			this.Encode(buffer);
			return buffer;
		}
	}
}
