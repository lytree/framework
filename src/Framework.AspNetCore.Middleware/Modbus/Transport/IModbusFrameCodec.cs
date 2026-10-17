using System;
using System.Buffers;

namespace Middleware.Modbus
{
	/// <summary>
	/// Modbus 应用数据单元（ADU）帧编解码器：负责在字节流与 PDU 之间转换
	/// </summary>
	public interface IModbusFrameCodec
	{
		/// <summary>
		/// 获取编解码器名称，用于日志
		/// </summary>
		string Name { get; }

		/// <summary>
		/// 尝试从输入缓冲解析一个完整帧
		/// </summary>
		/// <param name="reader">输入缓冲读取器，解析成功时位置推进到帧尾</param>
		/// <param name="unitId">单元标识（从站地址）</param>
		/// <param name="transactionId">事务标识，仅 MBAP 使用，RTU 恒为 0</param>
		/// <param name="pdu">
		/// PDU 片段，指向缓冲区内部；调用方必须在 <c>AdvanceTo</c> 之前消费完，
		/// 跨多个内存段时可自行决定是否拷贝
		/// </param>
		/// <returns>true 表示已解析出一个完整帧；false 表示数据不足，需要等待更多字节</returns>
		/// <exception cref="ModbusFrameException">帧结构非法（如 CRC 校验失败）；此时该帧已被消费</exception>
		bool TryReadFrame(ref SequenceReader<byte> reader, out byte unitId, out ushort transactionId, out ReadOnlySequence<byte> pdu);

		/// <summary>
		/// 把响应 PDU 封装成帧写入输出
		/// </summary>
		/// <param name="output">输出缓冲</param>
		/// <param name="unitId">单元标识（从站地址）</param>
		/// <param name="transactionId">事务标识，仅 MBAP 使用</param>
		/// <param name="pdu">响应 PDU</param>
		void WriteFrame(IBufferWriter<byte> output, byte unitId, ushort transactionId, ModbusPdu pdu);
	}
}
