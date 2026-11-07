using System;
using System.Buffers.Binary;

namespace Framework.Modbus.Abstractions
{
	/// <summary>
	/// 取通信事件计数器请求（FC0B），请求不携带数据域。
	/// </summary>
	public sealed class GetCommEventCounterRequest : ModbusRequest
	{
		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.GetCommEventCounter;

		/// <inheritdoc/>
		public override int Length => 1;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
		}

		/// <summary>
		/// 解析请求。
		/// </summary>
		/// <param name="data">数据域（不含功能码），FC0B 必须为空。</param>
		/// <returns>解析出的请求。</returns>
		/// <exception cref="ModbusSlaveException">数据域不为空。</exception>
		public static GetCommEventCounterRequest Decode(ReadOnlySpan<byte> data)
		{
			if (!data.IsEmpty)
			{
				throw ModbusSlaveException.IllegalDataValue($"FC0B 请求不携带数据域，实际长度为 {data.Length}");
			}

			return new GetCommEventCounterRequest();
		}
	}

	/// <summary>
	/// 取通信事件计数器响应（FC0B），数据域为「状态字 + 事件计数」。
	/// </summary>
	public sealed class GetCommEventCounterResponse : ModbusResponse
	{
		/// <summary>
		/// 取通信事件计数器响应。
		/// </summary>
		/// <param name="status">状态字，0xFFFF 表示从站忙，0x0000 表示就绪。</param>
		/// <param name="eventCount">事件计数。</param>
		public GetCommEventCounterResponse(ushort status, ushort eventCount)
		{
			this.Status = status;
			this.EventCount = eventCount;
		}

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.GetCommEventCounter;

		/// <summary>
		/// 获取状态字。
		/// </summary>
		public ushort Status { get; }

		/// <summary>
		/// 获取事件计数。
		/// </summary>
		public ushort EventCount { get; }

		/// <inheritdoc/>
		public override int Length => 5;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			BinaryPrimitives.WriteUInt16BigEndian(destination[1..], this.Status);
			BinaryPrimitives.WriteUInt16BigEndian(destination[3..], this.EventCount);
		}

		/// <summary>
		/// 解析取通信事件计数器响应。
		/// </summary>
		/// <param name="expectedFunctionCode">期望的响应功能码。</param>
		/// <param name="pdu">响应 PDU（含功能码）。</param>
		/// <returns>解析出的响应。</returns>
		/// <exception cref="ModbusSlaveException">功能码不匹配或 PDU 非法。</exception>
		public static GetCommEventCounterResponse Decode(byte expectedFunctionCode, ReadOnlySpan<byte> pdu)
		{
			if (pdu.IsEmpty)
			{
				throw ModbusSlaveException.IllegalDataValue("取通信事件计数器响应 PDU 为空");
			}

			if (pdu[0] != expectedFunctionCode)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"功能码不匹配：期望 0x{expectedFunctionCode:X2}，实际 0x{pdu[0]:X2}");
			}

			if (pdu.Length < 5)
			{
				throw ModbusSlaveException.TruncatedPdu(expectedFunctionCode, 5, pdu.Length);
			}

			return new GetCommEventCounterResponse(
				BinaryPrimitives.ReadUInt16BigEndian(pdu[1..]),
				BinaryPrimitives.ReadUInt16BigEndian(pdu[3..]));
		}
	}

	/// <summary>
	/// 取通信事件记录请求（FC0C），请求不携带数据域。
	/// </summary>
	public sealed class GetCommEventLogRequest : ModbusRequest
	{
		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.GetCommEventLog;

		/// <inheritdoc/>
		public override int Length => 1;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
		}

		/// <summary>
		/// 解析请求。
		/// </summary>
		/// <param name="data">数据域（不含功能码），FC0C 必须为空。</param>
		/// <returns>解析出的请求。</returns>
		/// <exception cref="ModbusSlaveException">数据域不为空。</exception>
		public static GetCommEventLogRequest Decode(ReadOnlySpan<byte> data)
		{
			if (!data.IsEmpty)
			{
				throw ModbusSlaveException.IllegalDataValue($"FC0C 请求不携带数据域，实际长度为 {data.Length}");
			}

			return new GetCommEventLogRequest();
		}
	}

	/// <summary>
	/// 取通信事件记录响应（FC0C），数据域为「字节计数 + 状态字 + 事件计数 + 报文计数 + 事件字节」。
	/// </summary>
	/// <remarks>
	/// 字节计数包含状态字（2）+ 事件计数（2）+ 报文计数（2）+ 事件字节，因此最小为 6。
	/// </remarks>
	public sealed class GetCommEventLogResponse : ModbusResponse
	{
		private readonly byte[] events;

		/// <summary>
		/// 取通信事件记录响应。
		/// </summary>
		/// <param name="status">状态字，0xFFFF 表示从站忙，0x0000 表示就绪。</param>
		/// <param name="eventCount">事件计数。</param>
		/// <param name="messageCount">报文计数。</param>
		/// <param name="events">事件字节，每字节一个事件。</param>
		public GetCommEventLogResponse(ushort status, ushort eventCount, ushort messageCount, ReadOnlySpan<byte> events)
		{
			this.Status = status;
			this.EventCount = eventCount;
			this.MessageCount = messageCount;
			this.events = events.ToArray();
		}

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.GetCommEventLog;

		/// <summary>
		/// 获取状态字。
		/// </summary>
		public ushort Status { get; }

		/// <summary>
		/// 获取事件计数。
		/// </summary>
		public ushort EventCount { get; }

		/// <summary>
		/// 获取报文计数。
		/// </summary>
		public ushort MessageCount { get; }

		/// <summary>
		/// 获取事件字节。
		/// </summary>
		public ReadOnlySpan<byte> Events => this.events;

		/// <summary>
		/// 获取字节计数（状态字 + 事件计数 + 报文计数 + 事件字节）。
		/// </summary>
		public int ByteCount => 6 + this.events.Length;

		/// <inheritdoc/>
		public override int Length => 2 + this.ByteCount;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			destination[1] = (byte)this.ByteCount;
			BinaryPrimitives.WriteUInt16BigEndian(destination[2..], this.Status);
			BinaryPrimitives.WriteUInt16BigEndian(destination[4..], this.EventCount);
			BinaryPrimitives.WriteUInt16BigEndian(destination[6..], this.MessageCount);
			this.events.CopyTo(destination[8..]);
		}

		/// <summary>
		/// 解析取通信事件记录响应。
		/// </summary>
		/// <param name="expectedFunctionCode">期望的响应功能码。</param>
		/// <param name="pdu">响应 PDU（含功能码）。</param>
		/// <returns>解析出的响应。</returns>
		/// <exception cref="ModbusSlaveException">功能码不匹配或 PDU 非法。</exception>
		public static GetCommEventLogResponse Decode(byte expectedFunctionCode, ReadOnlySpan<byte> pdu)
		{
			if (pdu.IsEmpty)
			{
				throw ModbusSlaveException.IllegalDataValue("取通信事件记录响应 PDU 为空");
			}

			if (pdu[0] != expectedFunctionCode)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"功能码不匹配：期望 0x{expectedFunctionCode:X2}，实际 0x{pdu[0]:X2}");
			}

			if (pdu.Length < 2)
			{
				throw ModbusSlaveException.TruncatedPdu(expectedFunctionCode, 2, pdu.Length);
			}

			var byteCount = pdu[1];
			if (byteCount < 6)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"取通信事件记录响应的字节计数 {byteCount} 非法，最小应为 6");
			}

			if (pdu.Length < 2 + byteCount)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"取通信事件记录响应声明 {byteCount} 字节数据，但实际只有 {pdu.Length - 2} 字节");
			}

			return new GetCommEventLogResponse(
				BinaryPrimitives.ReadUInt16BigEndian(pdu[2..]),
				BinaryPrimitives.ReadUInt16BigEndian(pdu[4..]),
				BinaryPrimitives.ReadUInt16BigEndian(pdu[6..]),
				pdu.Slice(8, byteCount - 6));
		}
	}
}
