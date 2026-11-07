using System;
using System.Buffers.Binary;

namespace Framework.Modbus.Abstractions
{
	/// <summary>
	/// 读 FIFO 队列请求（FC18），数据域为「FIFO 指针地址」。
	/// </summary>
	public sealed class ReadFifoQueueRequest : ModbusRequest
	{
		/// <summary>
		/// 读 FIFO 队列请求。
		/// </summary>
		/// <param name="address">FIFO 指针地址。</param>
		public ReadFifoQueueRequest(ushort address)
		{
			this.Address = address;
		}

		/// <summary>
		/// 获取 FIFO 指针地址。
		/// </summary>
		public ushort Address { get; }

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.ReadFifoQueue;

		/// <inheritdoc/>
		public override int Length => 3;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			BinaryPrimitives.WriteUInt16BigEndian(destination[1..], this.Address);
		}

		/// <summary>
		/// 解析请求。
		/// </summary>
		/// <param name="data">数据域（不含功能码）。</param>
		/// <returns>解析出的请求。</returns>
		/// <exception cref="ModbusSlaveException">数据域长度非法。</exception>
		public static ReadFifoQueueRequest Decode(ReadOnlySpan<byte> data)
		{
			if (data.Length < 2)
			{
				throw ModbusSlaveException.TruncatedPdu(ModbusFunctionCode.ReadFifoQueue, 2, data.Length);
			}

			if (data.Length != 2)
			{
				throw ModbusSlaveException.IllegalDataValue($"读 FIFO 队列请求的数据域长度应为 2，实际为 {data.Length}");
			}

			return new ReadFifoQueueRequest(ReadUInt16(data, 0));
		}
	}

	/// <summary>
	/// 读 FIFO 队列响应（FC18），数据域为「字节计数 + FIFO 数量 + FIFO 数据」。
	/// </summary>
	public sealed class ReadFifoQueueResponse : ModbusResponse
	{
		private readonly ushort[] values;

		/// <summary>
		/// 读 FIFO 队列响应。
		/// </summary>
		/// <param name="values">FIFO 中的寄存器值，按先进先出顺序排列。</param>
		/// <exception cref="ModbusSlaveException">值为空或数量超出协议上限。</exception>
		public ReadFifoQueueResponse(ushort[] values)
		{
			if (values is null)
			{
				throw ModbusSlaveException.IllegalDataValue("读 FIFO 队列响应的寄存器值不能为空");
			}

			if (values.Length > ModbusConstants.MaxFifoQueueCount)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"读 FIFO 队列响应的 FIFO 数量 {values.Length} 超出上限 {ModbusConstants.MaxFifoQueueCount}");
			}

			this.values = values;
		}

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.ReadFifoQueue;

		/// <summary>
		/// 获取 FIFO 中的寄存器值。
		/// </summary>
		public ReadOnlySpan<ushort> Values => this.values;

		/// <summary>
		/// 获取 FIFO 中的寄存器数量。
		/// </summary>
		public int FifoCount => this.values.Length;

		/// <summary>
		/// 获取字节计数域的值（FIFO 数量域 + FIFO 数据）。
		/// </summary>
		public int ByteCount => 2 + this.values.Length * 2;

		/// <inheritdoc/>
		public override int Length => 3 + this.ByteCount;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			BinaryPrimitives.WriteUInt16BigEndian(destination[1..], (ushort)this.ByteCount);
			BinaryPrimitives.WriteUInt16BigEndian(destination[3..], (ushort)this.values.Length);

			for (var i = 0; i < this.values.Length; i++)
			{
				BinaryPrimitives.WriteUInt16BigEndian(destination[(5 + i * 2)..], this.values[i]);
			}
		}

		/// <summary>
		/// 解析响应。
		/// </summary>
		/// <param name="expectedFunctionCode">期望的响应功能码。</param>
		/// <param name="pdu">响应 PDU（含功能码）。</param>
		/// <returns>解析出的响应。</returns>
		/// <exception cref="ModbusSlaveException">功能码不匹配或数据域非法。</exception>
		public static ReadFifoQueueResponse Decode(byte expectedFunctionCode, ReadOnlySpan<byte> pdu)
		{
			if (pdu.IsEmpty)
			{
				throw ModbusSlaveException.IllegalDataValue("读 FIFO 队列响应 PDU 为空");
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

			var byteCount = BinaryPrimitives.ReadUInt16BigEndian(pdu[1..]);
			if (byteCount < 2)
			{
				throw ModbusSlaveException.IllegalDataValue($"读 FIFO 队列响应的字节计数 {byteCount} 至少为 2");
			}

			if ((byteCount & 1) != 0)
			{
				throw ModbusSlaveException.IllegalDataValue($"读 FIFO 队列响应的字节计数 {byteCount} 不是偶数");
			}

			if (pdu.Length < 3 + byteCount)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"读 FIFO 队列响应声明 {byteCount} 字节数据，但实际只有 {pdu.Length - 3} 字节");
			}

			var fifoCount = BinaryPrimitives.ReadUInt16BigEndian(pdu[3..]);
			if (fifoCount > ModbusConstants.MaxFifoQueueCount)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"读 FIFO 队列响应的 FIFO 数量 {fifoCount} 超出上限 {ModbusConstants.MaxFifoQueueCount}");
			}

			if (byteCount != 2 + fifoCount * 2)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"读 FIFO 队列响应的字节计数 {byteCount} 与 FIFO 数量 {fifoCount} 不匹配，应为 {2 + fifoCount * 2}");
			}

			var values = new ushort[fifoCount];
			for (var i = 0; i < values.Length; i++)
			{
				values[i] = BinaryPrimitives.ReadUInt16BigEndian(pdu[(5 + i * 2)..]);
			}

			return new ReadFifoQueueResponse(values);
		}
	}
}
