using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Middleware.Modbus
{
	/// <summary>
	/// Modbus/TCP 帧编解码器：7 字节 MBAP 头 + PDU
	/// </summary>
	/// <remarks>
	/// MBAP 头结构：事务标识(2) + 协议标识(2，恒为 0) + 长度(2，其后字节数) + 单元标识(1)
	/// </remarks>
	public sealed class MbapFrameCodec : IModbusFrameCodec
	{
		/// <summary>
		/// MBAP 头长度
		/// </summary>
		public const int HeaderLength = 7;

		/// <summary>
		/// PDU 最大长度（Modbus 规范上限 253 字节）
		/// </summary>
		public const int MaxPduLength = 253;

		/// <inheritdoc/>
		public string Name => "Modbus/TCP";

		/// <inheritdoc/>
		public bool TryReadFrame(ref SequenceReader<byte> reader, out byte unitId, out ushort transactionId, out ReadOnlySequence<byte> pdu)
		{
			unitId = 0;
			transactionId = 0;
			pdu = default;

			if (reader.Remaining < HeaderLength)
			{
				return false;
			}

			// 先在副本上探测帧头，避免数据不足时提前消费
			var probe = reader;
			probe.TryReadBigEndian(out short transactionIdValue);
			probe.TryReadBigEndian(out short protocolIdValue);
			probe.TryReadBigEndian(out short lengthValue);
			probe.TryRead(out var unitIdValue);

			var protocolId = (ushort)protocolIdValue;
			var length = (ushort)lengthValue;
			if (protocolId != 0 || length < 2 || length > MaxPduLength + 1)
			{
				// 帧头非法：消费掉头部后报错，避免连接被脏数据卡死
				reader = probe;
				throw new ModbusFrameException($"非法的 MBAP 头：协议标识 {protocolId}，长度字段 {length}");
			}

			var pduLength = length - 1;
			if (probe.Remaining < pduLength)
			{
				return false;
			}

			probe.TryReadExact(pduLength, out var body);

			unitId = unitIdValue;
			transactionId = (ushort)transactionIdValue;
			pdu = body;
			reader = probe;
			return true;
		}

		/// <inheritdoc/>
		public void WriteFrame(IBufferWriter<byte> output, byte unitId, ushort transactionId, ModbusPdu pdu)
		{
			var total = HeaderLength + pdu.Length;
			var span = output.GetSpan(total);

			BinaryPrimitives.WriteUInt16BigEndian(span, transactionId);
			BinaryPrimitives.WriteUInt16BigEndian(span[2..], 0);
			BinaryPrimitives.WriteUInt16BigEndian(span[4..], (ushort)(pdu.Length + 1));
			span[6] = unitId;
			pdu.Encode(span[HeaderLength..]);

			output.Advance(total);
		}
	}
}
