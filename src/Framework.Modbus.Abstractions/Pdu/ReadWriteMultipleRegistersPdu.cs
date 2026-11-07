using System;
using System.Buffers.Binary;

namespace Framework.Modbus.Abstractions
{
	/// <summary>
	/// 读写多个寄存器请求（FC17），数据域为「读地址 + 读数量 + 写地址 + 写数量 + 字节计数 + 写数据」。
	/// </summary>
	public sealed class ReadWriteMultipleRegistersRequest : ModbusRequest
	{
		private readonly ushort[] writeValues;

		/// <summary>
		/// 读写多个寄存器请求。
		/// </summary>
		/// <param name="readAddress">要读取的起始地址。</param>
		/// <param name="readCount">要读取的寄存器数量。</param>
		/// <param name="writeAddress">要写入的起始地址。</param>
		/// <param name="writeValues">要写入的寄存器值。</param>
		/// <exception cref="ModbusSlaveException">读数量或写数量超出协议范围。</exception>
		public ReadWriteMultipleRegistersRequest(ushort readAddress, ushort readCount, ushort writeAddress, ushort[] writeValues)
		{
			if (readCount < 1 || readCount > ModbusConstants.MaxReadRegisters)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"读写多个寄存器请求的读数量 {readCount} 超出 1~{ModbusConstants.MaxReadRegisters} 范围");
			}

			if (writeValues is null || writeValues.Length < 1 || writeValues.Length > ModbusConstants.MaxWriteRegisters)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"读写多个寄存器请求的写数量超出 1~{ModbusConstants.MaxWriteRegisters} 范围");
			}

			this.ReadAddress = readAddress;
			this.ReadCount = readCount;
			this.WriteAddress = writeAddress;
			this.writeValues = writeValues;
		}

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.ReadWriteMultipleRegisters;

		/// <summary>
		/// 获取要读取的起始地址。
		/// </summary>
		public ushort ReadAddress { get; }

		/// <summary>
		/// 获取要读取的寄存器数量。
		/// </summary>
		public ushort ReadCount { get; }

		/// <summary>
		/// 获取要写入的起始地址。
		/// </summary>
		public ushort WriteAddress { get; }

		/// <summary>
		/// 获取要写入的寄存器值视图。
		/// </summary>
		public ReadOnlySpan<ushort> WriteValues => this.writeValues;

		/// <summary>
		/// 获取要写入的寄存器数量。
		/// </summary>
		public ushort WriteCount => (ushort)this.writeValues.Length;

		/// <inheritdoc/>
		public override int Length => 10 + this.writeValues.Length * 2;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			BinaryPrimitives.WriteUInt16BigEndian(destination[1..], this.ReadAddress);
			BinaryPrimitives.WriteUInt16BigEndian(destination[3..], this.ReadCount);
			BinaryPrimitives.WriteUInt16BigEndian(destination[5..], this.WriteAddress);
			BinaryPrimitives.WriteUInt16BigEndian(destination[7..], this.WriteCount);
			destination[9] = (byte)(this.WriteCount * 2);

			for (var i = 0; i < this.writeValues.Length; i++)
			{
				BinaryPrimitives.WriteUInt16BigEndian(destination[(10 + i * 2)..], this.writeValues[i]);
			}
		}

		/// <summary>
		/// 解析请求。
		/// </summary>
		/// <param name="data">数据域（不含功能码）。</param>
		/// <returns>解析出的请求。</returns>
		/// <exception cref="ModbusSlaveException">数据域非法。</exception>
		public static ReadWriteMultipleRegistersRequest Decode(ReadOnlySpan<byte> data)
		{
			if (data.Length < 9)
			{
				throw ModbusSlaveException.TruncatedPdu(ModbusFunctionCode.ReadWriteMultipleRegisters, 9, data.Length);
			}

			var readAddress = ReadUInt16(data, 0);
			var readCount = ReadUInt16(data, 2);
			var writeAddress = ReadUInt16(data, 4);
			var writeCount = ReadUInt16(data, 6);

			if (readCount < 1 || readCount > ModbusConstants.MaxReadRegisters)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"读写多个寄存器请求的读数量 {readCount} 超出 1~{ModbusConstants.MaxReadRegisters} 范围");
			}

			if (writeCount < 1 || writeCount > ModbusConstants.MaxWriteRegisters)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"读写多个寄存器请求的写数量 {writeCount} 超出 1~{ModbusConstants.MaxWriteRegisters} 范围");
			}

			var byteCount = data[8];
			var expected = writeCount * 2;
			if (byteCount != expected)
			{
				throw ModbusSlaveException.IllegalDataValue($"FC17 的字节数 {byteCount} 与写数量 {writeCount} 不匹配，应为 {expected}");
			}

			if (data.Length < 9 + byteCount)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"读写多个寄存器请求声明 {byteCount} 字节数据，但实际只有 {data.Length - 9} 字节");
			}

			if (data.Length != 9 + byteCount)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"读写多个寄存器请求的数据域长度应为 {9 + byteCount}，实际为 {data.Length}");
			}

			var writeValues = new ushort[writeCount];
			for (var i = 0; i < writeValues.Length; i++)
			{
				writeValues[i] = BinaryPrimitives.ReadUInt16BigEndian(data[(9 + i * 2)..]);
			}

			return new ReadWriteMultipleRegistersRequest(readAddress, readCount, writeAddress, writeValues);
		}
	}

	/// <summary>
	/// 读写多个寄存器响应（FC17），数据域为「字节计数 + 寄存器数据」。
	/// </summary>
	public sealed class ReadWriteMultipleRegistersResponse : ModbusResponse
	{
		private readonly ushort[] registers;

		/// <summary>
		/// 读写多个寄存器响应。
		/// </summary>
		/// <param name="registers">读到的寄存器数据。</param>
		public ReadWriteMultipleRegistersResponse(ushort[] registers)
		{
			this.registers = registers;
		}

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.ReadWriteMultipleRegisters;

		/// <summary>
		/// 获取数据域的字节数。
		/// </summary>
		public int ByteCount => this.registers.Length * 2;

		/// <summary>
		/// 获取读到的寄存器数据。
		/// </summary>
		public ReadOnlySpan<ushort> Registers => this.registers;

		/// <inheritdoc/>
		public override int Length => 2 + this.ByteCount;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			destination[1] = (byte)this.ByteCount;

			for (var i = 0; i < this.registers.Length; i++)
			{
				BinaryPrimitives.WriteUInt16BigEndian(destination[(2 + i * 2)..], this.registers[i]);
			}
		}

		/// <summary>
		/// 解析响应。
		/// </summary>
		/// <param name="pdu">响应 PDU（含功能码）。</param>
		/// <returns>解析出的响应。</returns>
		/// <exception cref="ModbusSlaveException">功能码不匹配或数据域非法。</exception>
		public static ReadWriteMultipleRegistersResponse Decode(ReadOnlySpan<byte> pdu)
		{
			var response = ReadRegistersResponse.Decode(ModbusFunctionCode.ReadWriteMultipleRegisters, pdu);
			return new ReadWriteMultipleRegistersResponse(response.Registers.ToArray());
		}
	}
}
