using System;
using System.Buffers.Binary;

namespace Middleware.Modbus
{
	/// <summary>
	/// 位读取响应（FC01/FC02），数据域为「字节数 + 位数据」
	/// </summary>
	public sealed class ReadBitsResponse : ModbusResponse
	{
		private readonly byte[] data;

		/// <summary>
		/// 位读取响应
		/// </summary>
		/// <param name="functionCode">响应功能码（与请求一致）</param>
		/// <param name="data">位数据，每个字节承载 8 位，低位对齐</param>
		public ReadBitsResponse(byte functionCode, ReadOnlySpan<byte> data)
		{
			this.FunctionCode = functionCode;
			this.data = data.ToArray();
		}

		/// <inheritdoc/>
		public override byte FunctionCode { get; }

		/// <summary>
		/// 获取数据域的字节数
		/// </summary>
		public int ByteCount => this.data.Length;

		/// <summary>
		/// 获取位数据
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
	}

	/// <summary>
	/// 寄存器读取响应（FC03/FC04），数据域为「字节数 + 寄存器数据」
	/// </summary>
	public sealed class ReadRegistersResponse : ModbusResponse
	{
		private readonly ushort[] registers;

		/// <summary>
		/// 寄存器读取响应
		/// </summary>
		/// <param name="functionCode">响应功能码（与请求一致）</param>
		/// <param name="registers">寄存器数据</param>
		public ReadRegistersResponse(byte functionCode, ushort[] registers)
		{
			this.FunctionCode = functionCode;
			this.registers = registers;
		}

		/// <inheritdoc/>
		public override byte FunctionCode { get; }

		/// <summary>
		/// 获取数据域的字节数
		/// </summary>
		public int ByteCount => this.registers.Length * 2;

		/// <summary>
		/// 获取寄存器数据
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
	}

	/// <summary>
	/// 单个写入响应（FC05/FC06），回显地址与写入值
	/// </summary>
	public sealed class WriteSingleResponse : ModbusResponse
	{
		/// <summary>
		/// 单个写入响应
		/// </summary>
		/// <param name="functionCode">响应功能码（与请求一致）</param>
		/// <param name="address">地址</param>
		/// <param name="value">写入的原始值</param>
		public WriteSingleResponse(byte functionCode, ushort address, ushort value)
		{
			this.FunctionCode = functionCode;
			this.Address = address;
			this.Value = value;
		}

		/// <inheritdoc/>
		public override byte FunctionCode { get; }

		/// <summary>
		/// 获取地址
		/// </summary>
		public ushort Address { get; }

		/// <summary>
		/// 获取写入的原始值
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
	}

	/// <summary>
	/// 多个写入响应（FC15/FC16），回显起始地址与数量
	/// </summary>
	public sealed class WriteMultipleResponse : ModbusResponse
	{
		/// <summary>
		/// 多个写入响应
		/// </summary>
		/// <param name="functionCode">响应功能码（与请求一致）</param>
		/// <param name="address">起始地址</param>
		/// <param name="count">写入数量</param>
		public WriteMultipleResponse(byte functionCode, ushort address, ushort count)
		{
			this.FunctionCode = functionCode;
			this.Address = address;
			this.Count = count;
		}

		/// <inheritdoc/>
		public override byte FunctionCode { get; }

		/// <summary>
		/// 获取起始地址
		/// </summary>
		public ushort Address { get; }

		/// <summary>
		/// 获取写入数量
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
	}

	/// <summary>
	/// 异常响应，功能码为「请求功能码 | 0x80」，数据域为异常码
	/// </summary>
	public sealed class ModbusExceptionResponse : ModbusResponse
	{
		/// <summary>
		/// 异常响应
		/// </summary>
		/// <param name="functionCode">请求功能码</param>
		/// <param name="exceptionCode">异常码</param>
		public ModbusExceptionResponse(byte functionCode, byte exceptionCode)
		{
			this.FunctionCode = (byte)(functionCode | ModbusFunctionCode.ExceptionFlag);
			this.ExceptionCode = exceptionCode;
		}

		/// <inheritdoc/>
		public override byte FunctionCode { get; }

		/// <summary>
		/// 获取异常码
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
