using System;
using System.Buffers.Binary;

namespace Framework.Modbus.Abstractions
{
	/// <summary>
	/// 位读取响应（FC01/FC02），数据域为「字节数 + 位数据」。
	/// </summary>
	public sealed class ReadBitsResponse : ModbusResponse
	{
		private readonly byte[] data;

		/// <summary>
		/// 位读取响应。
		/// </summary>
		/// <param name="functionCode">响应功能码（与请求一致）。</param>
		/// <param name="data">位数据，每个字节承载 8 位，低位对齐。</param>
		public ReadBitsResponse(byte functionCode, ReadOnlySpan<byte> data)
		{
			this.FunctionCode = functionCode;
			this.data = data.ToArray();
		}

		/// <inheritdoc/>
		public override byte FunctionCode { get; }

		/// <summary>
		/// 获取数据域的字节数。
		/// </summary>
		public int ByteCount => this.data.Length;

		/// <summary>
		/// 获取位数据。
		/// </summary>
		public ReadOnlyMemory<byte> Data => this.data;

		/// <inheritdoc/>
		public override int Length => 2 + this.data.Length;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			destination[1] = (byte)this.data.Length;
			this.data.CopyTo(destination[2..]);
		}

		/// <summary>
		/// 解析位读取响应。
		/// </summary>
		/// <param name="expectedFunctionCode">期望的响应功能码。</param>
		/// <param name="pdu">响应 PDU（含功能码）。</param>
		/// <returns>解析出的响应。</returns>
		/// <exception cref="ModbusSlaveException">功能码不匹配或数据域非法。</exception>
		public static ReadBitsResponse Decode(byte expectedFunctionCode, ReadOnlySpan<byte> pdu)
		{
			if (pdu.IsEmpty)
			{
				throw ModbusSlaveException.IllegalDataValue("位读取响应 PDU 为空");
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
			if (pdu.Length < 2 + byteCount)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"位读取响应声明 {byteCount} 字节数据，但实际只有 {pdu.Length - 2} 字节");
			}

			return new ReadBitsResponse(expectedFunctionCode, pdu.Slice(2, byteCount));
		}
	}

	/// <summary>
	/// 寄存器读取响应（FC03/FC04），数据域为「字节数 + 寄存器数据」。
	/// </summary>
	public sealed class ReadRegistersResponse : ModbusResponse
	{
		private readonly ushort[] registers;

		/// <summary>
		/// 寄存器读取响应。
		/// </summary>
		/// <param name="functionCode">响应功能码（与请求一致）。</param>
		/// <param name="registers">寄存器数据。</param>
		public ReadRegistersResponse(byte functionCode, ushort[] registers)
		{
			this.FunctionCode = functionCode;
			this.registers = registers;
		}

		/// <inheritdoc/>
		public override byte FunctionCode { get; }

		/// <summary>
		/// 获取数据域的字节数。
		/// </summary>
		public int ByteCount => this.registers.Length * 2;

		/// <summary>
		/// 获取寄存器数据。
		/// </summary>
		public ReadOnlySpan<ushort> Registers => this.registers;

		/// <inheritdoc/>
		public override int Length => 2 + this.ByteCount;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			destination[1] = (byte)this.ByteCount;

			var payload = destination[2..];
			for (var i = 0; i < this.registers.Length; i++)
			{
				BinaryPrimitives.WriteUInt16BigEndian(payload[(i * 2)..], this.registers[i]);
			}
		}

		/// <summary>
		/// 解析寄存器读取响应。
		/// </summary>
		/// <param name="expectedFunctionCode">期望的响应功能码。</param>
		/// <param name="pdu">响应 PDU（含功能码）。</param>
		/// <returns>解析出的响应。</returns>
		/// <exception cref="ModbusSlaveException">功能码不匹配或数据域非法。</exception>
		public static ReadRegistersResponse Decode(byte expectedFunctionCode, ReadOnlySpan<byte> pdu)
		{
			if (pdu.IsEmpty)
			{
				throw ModbusSlaveException.IllegalDataValue("寄存器读取响应 PDU 为空");
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
			if ((byteCount & 1) != 0)
			{
				throw ModbusSlaveException.IllegalDataValue($"寄存器读取响应的字节计数 {byteCount} 不是偶数");
			}

			if (pdu.Length < 2 + byteCount)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"寄存器读取响应声明 {byteCount} 字节数据，但实际只有 {pdu.Length - 2} 字节");
			}

			var registers = new ushort[byteCount / 2];
			var payload = pdu.Slice(2, byteCount);
			for (var i = 0; i < registers.Length; i++)
			{
				registers[i] = BinaryPrimitives.ReadUInt16BigEndian(payload[(i * 2)..]);
			}

			return new ReadRegistersResponse(expectedFunctionCode, registers);
		}
	}

	/// <summary>
	/// 单个写入响应（FC05/FC06），回显地址与写入值。
	/// </summary>
	public sealed class WriteSingleResponse : ModbusResponse
	{
		/// <summary>
		/// 单个写入响应。
		/// </summary>
		/// <param name="functionCode">响应功能码（与请求一致）。</param>
		/// <param name="address">地址。</param>
		/// <param name="value">写入的原始值。</param>
		public WriteSingleResponse(byte functionCode, ushort address, ushort value)
		{
			this.FunctionCode = functionCode;
			this.Address = address;
			this.Value = value;
		}

		/// <inheritdoc/>
		public override byte FunctionCode { get; }

		/// <summary>
		/// 获取地址。
		/// </summary>
		public ushort Address { get; }

		/// <summary>
		/// 获取写入的原始值。
		/// </summary>
		public ushort Value { get; }

		/// <inheritdoc/>
		public override int Length => 5;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			BinaryPrimitives.WriteUInt16BigEndian(destination[1..], this.Address);
			BinaryPrimitives.WriteUInt16BigEndian(destination[3..], this.Value);
		}

		/// <summary>
		/// 解析单个写入响应。
		/// </summary>
		/// <param name="expectedFunctionCode">期望的响应功能码。</param>
		/// <param name="pdu">响应 PDU（含功能码）。</param>
		/// <returns>解析出的响应。</returns>
		/// <exception cref="ModbusSlaveException">功能码不匹配或数据域非法。</exception>
		public static WriteSingleResponse Decode(byte expectedFunctionCode, ReadOnlySpan<byte> pdu)
		{
			if (pdu.IsEmpty)
			{
				throw ModbusSlaveException.IllegalDataValue("单个写入响应 PDU 为空");
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

			return new WriteSingleResponse(
				expectedFunctionCode,
				BinaryPrimitives.ReadUInt16BigEndian(pdu[1..]),
				BinaryPrimitives.ReadUInt16BigEndian(pdu[3..]));
		}
	}

	/// <summary>
	/// 多个写入响应（FC15/FC16），回显起始地址与数量。
	/// </summary>
	public sealed class WriteMultipleResponse : ModbusResponse
	{
		/// <summary>
		/// 多个写入响应。
		/// </summary>
		/// <param name="functionCode">响应功能码（与请求一致）。</param>
		/// <param name="address">起始地址。</param>
		/// <param name="count">写入数量。</param>
		public WriteMultipleResponse(byte functionCode, ushort address, ushort count)
		{
			this.FunctionCode = functionCode;
			this.Address = address;
			this.Count = count;
		}

		/// <inheritdoc/>
		public override byte FunctionCode { get; }

		/// <summary>
		/// 获取起始地址。
		/// </summary>
		public ushort Address { get; }

		/// <summary>
		/// 获取写入数量。
		/// </summary>
		public ushort Count { get; }

		/// <inheritdoc/>
		public override int Length => 5;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			BinaryPrimitives.WriteUInt16BigEndian(destination[1..], this.Address);
			BinaryPrimitives.WriteUInt16BigEndian(destination[3..], this.Count);
		}

		/// <summary>
		/// 解析多个写入响应。
		/// </summary>
		/// <param name="expectedFunctionCode">期望的响应功能码。</param>
		/// <param name="pdu">响应 PDU（含功能码）。</param>
		/// <returns>解析出的响应。</returns>
		/// <exception cref="ModbusSlaveException">功能码不匹配或数据域非法。</exception>
		public static WriteMultipleResponse Decode(byte expectedFunctionCode, ReadOnlySpan<byte> pdu)
		{
			if (pdu.IsEmpty)
			{
				throw ModbusSlaveException.IllegalDataValue("多个写入响应 PDU 为空");
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

			return new WriteMultipleResponse(
				expectedFunctionCode,
				BinaryPrimitives.ReadUInt16BigEndian(pdu[1..]),
				BinaryPrimitives.ReadUInt16BigEndian(pdu[3..]));
		}
	}

	/// <summary>
	/// 异常响应，功能码为「请求功能码 | 0x80」，数据域为异常码。
	/// </summary>
	public sealed class ModbusExceptionResponse : ModbusResponse
	{
		/// <summary>
		/// 异常响应。
		/// </summary>
		/// <param name="functionCode">请求功能码。</param>
		/// <param name="exceptionCode">异常码。</param>
		public ModbusExceptionResponse(byte functionCode, byte exceptionCode)
		{
			this.FunctionCode = (byte)(functionCode | ModbusFunctionCode.ExceptionFlag);
			this.ExceptionCode = exceptionCode;
		}

		/// <inheritdoc/>
		public override byte FunctionCode { get; }

		/// <summary>
		/// 获取异常码。
		/// </summary>
		public byte ExceptionCode { get; }

		/// <inheritdoc/>
		public override int Length => 2;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			destination[1] = this.ExceptionCode;
		}
	}
}
