using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Middleware.Modbus
{
	/// <summary>
	/// Modbus RTU 帧编解码器：从站地址(1) + PDU + CRC16(2，低字节在前)
	/// </summary>
	/// <remarks>
	/// RTU 原本依靠 3.5 个字符时间的静默间隔划分帧；在 TCP 上承载（RTU over TCP，
	/// 常见于串口服务器）无法依赖时序，因此这里按功能码推断帧长。
	/// 未知功能码无法推断长度，按最短帧（5 字节 PDU）处理并由从站回非法功能异常，
	/// 此时客户端应重新同步。
	/// </remarks>
	public sealed class RtuFrameCodec : IModbusFrameCodec
	{
		/// <summary>
		/// 读、写单个量请求的 PDU 长度（功能码 1 + 地址 2 + 数量/数值 2）
		/// </summary>
		private const int FixedPduLength = 5;

		/// <inheritdoc/>
		public string Name => "Modbus RTU over TCP";

		/// <inheritdoc/>
		public bool TryReadFrame(ref SequenceReader<byte> reader, out byte unitId, out ushort transactionId, out ReadOnlySequence<byte> pdu)
		{
			unitId = 0;
			transactionId = 0;
			pdu = default;

			// 先在副本上探测，用于推断整帧长度
			var probe = reader;
			if (probe.TryRead(out var unitIdValue) == false)
			{
				return false;
			}
			if (probe.TryRead(out var functionCode) == false)
			{
				return false;
			}

			var pduLength = GetPduLength(probe, functionCode);
			if (pduLength < 0)
			{
				return false;
			}

			var totalLength = 1 + pduLength + ModbusCrc16.Length;
			if (reader.Remaining < totalLength)
			{
				return false;
			}

			var frameReader = reader;
			frameReader.TryReadExact(totalLength, out var frame);

			// 无论校验是否通过，整帧都已消费，避免脏数据反复触发同一错误
			reader = frameReader;

			var frameSpan = frame.IsSingleSegment ? frame.FirstSpan : frame.ToArray();
			var expected = ModbusCrc16.Compute(frameSpan[..^ModbusCrc16.Length]);
			var actual = BinaryPrimitives.ReadUInt16LittleEndian(frameSpan[^ModbusCrc16.Length..]);
			if (expected != actual)
			{
				throw new ModbusFrameException($"RTU 帧 CRC 校验失败：期望 0x{expected:X4}，实际 0x{actual:X4}");
			}

			unitId = unitIdValue;
			pdu = frame.Slice(1, pduLength);
			return true;
		}

		/// <inheritdoc/>
		public void WriteFrame(IBufferWriter<byte> output, byte unitId, ushort transactionId, ModbusPdu pdu)
		{
			var total = 1 + pdu.Length + ModbusCrc16.Length;
			var span = output.GetSpan(total);

			span[0] = unitId;
			pdu.Encode(span[1..]);

			var crc = ModbusCrc16.Compute(span[..(1 + pdu.Length)]);
			BinaryPrimitives.WriteUInt16LittleEndian(span[(1 + pdu.Length)..], crc);

			output.Advance(total);
		}

		/// <summary>
		/// 按功能码推断 PDU 长度
		/// </summary>
		/// <param name="reader">已消费「从站地址 + 功能码」的读取器副本</param>
		/// <param name="functionCode">功能码</param>
		/// <returns>PDU 长度；负数表示数据不足，需要等待更多字节</returns>
		private static int GetPduLength(in SequenceReader<byte> reader, byte functionCode)
		{
			switch (functionCode)
			{
				case ModbusFunctionCode.ReadCoils:
				case ModbusFunctionCode.ReadDiscreteInputs:
				case ModbusFunctionCode.ReadHoldingRegisters:
				case ModbusFunctionCode.ReadInputRegisters:
				case ModbusFunctionCode.WriteSingleCoil:
				case ModbusFunctionCode.WriteSingleRegister:
					return FixedPduLength;

				case ModbusFunctionCode.WriteMultipleCoils:
				case ModbusFunctionCode.WriteMultipleRegisters:
					// 功能码(1) + 地址(2) + 数量(2) + 字节数(1) + 数据(n)
					if (reader.Remaining < 5)
					{
						return -1;
					}

					var probe = reader;
					probe.Advance(4);
					if (probe.TryRead(out var byteCount) == false)
					{
						return -1;
					}
					return 6 + byteCount;

				default:
					return FixedPduLength;
			}
		}
	}
}
