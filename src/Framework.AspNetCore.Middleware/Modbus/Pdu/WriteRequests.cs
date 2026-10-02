using System;
using System.Buffers.Binary;

namespace Middleware.Modbus
{
	/// <summary>
	/// 写单个线圈请求（FC05），数据域为「地址 + 线圈值」
	/// </summary>
	public sealed class WriteSingleCoilRequest : ModbusRequest
	{
		/// <summary>
		/// 线圈置位值 0xFF00
		/// </summary>
		public const ushort CoilOn = 0xFF00;

		/// <summary>
		/// 线圈复位值 0x0000
		/// </summary>
		public const ushort CoilOff = 0x0000;

		/// <summary>
		/// 写单个线圈请求
		/// </summary>
		/// <param name="address">线圈地址</param>
		/// <param name="value">线圈值</param>
		public WriteSingleCoilRequest(ushort address, bool value)
		{
			this.Address = address;
			this.RawValue = value ? CoilOn : CoilOff;
		}

		/// <summary>
		/// 获取线圈地址
		/// </summary>
		public ushort Address { get; }

		/// <summary>
		/// 获取原始线圈值（0xFF00 或 0x0000），响应需原样回显
		/// </summary>
		public ushort RawValue { get; }

		/// <summary>
		/// 获取线圈值
		/// </summary>
		public bool Value => this.RawValue == CoilOn;

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.WriteSingleCoil;

		/// <inheritdoc/>
		public override int Length => 1 + AddressFieldLength;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			BinaryPrimitives.WriteUInt16BigEndian(destination[1..], this.Address);
			BinaryPrimitives.WriteUInt16BigEndian(destination[3..], this.RawValue);
		}

		/// <summary>
		/// 解析请求
		/// </summary>
		/// <param name="data">数据域（不含功能码）</param>
		/// <returns></returns>
		public static WriteSingleCoilRequest Decode(ReadOnlySpan<byte> data)
		{
			EnsureDataLength(ModbusFunctionCode.WriteSingleCoil, data, AddressFieldLength);

			var address = ReadUInt16(data, 0);
			var value = ReadUInt16(data, 2);
			if (value != CoilOn && value != CoilOff)
			{
				throw ModbusSlaveException.IllegalDataValue($"FC05 的线圈值 0x{value:X4} 非法，仅允许 0xFF00 或 0x0000");
			}

			return new WriteSingleCoilRequest(address, value == CoilOn);
		}
	}

	/// <summary>
	/// 写单个寄存器请求（FC06），数据域为「地址 + 寄存器值」
	/// </summary>
	public sealed class WriteSingleRegisterRequest : ModbusRequest
	{
		/// <summary>
		/// 写单个寄存器请求
		/// </summary>
		/// <param name="address">寄存器地址</param>
		/// <param name="value">寄存器值</param>
		public WriteSingleRegisterRequest(ushort address, ushort value)
		{
			this.Address = address;
			this.Value = value;
		}

		/// <summary>
		/// 获取寄存器地址
		/// </summary>
		public ushort Address { get; }

		/// <summary>
		/// 获取寄存器值
		/// </summary>
		public ushort Value { get; }

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.WriteSingleRegister;

		/// <inheritdoc/>
		public override int Length => 1 + AddressFieldLength;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			BinaryPrimitives.WriteUInt16BigEndian(destination[1..], this.Address);
			BinaryPrimitives.WriteUInt16BigEndian(destination[3..], this.Value);
		}

		/// <summary>
		/// 解析请求
		/// </summary>
		/// <param name="data">数据域（不含功能码）</param>
		/// <returns></returns>
		public static WriteSingleRegisterRequest Decode(ReadOnlySpan<byte> data)
		{
			EnsureDataLength(ModbusFunctionCode.WriteSingleRegister, data, AddressFieldLength);
			return new WriteSingleRegisterRequest(ReadUInt16(data, 0), ReadUInt16(data, 2));
		}
	}

	/// <summary>
	/// 写多个线圈请求（FC15），数据域为「起始地址 + 数量 + 字节数 + 位数据」
	/// </summary>
	public sealed class WriteMultipleCoilsRequest : ModbusRequest
	{
		/// <summary>
		/// 一次请求可写入的最大线圈数
		/// </summary>
		public const int MaxCount = 1968;

		/// <summary>
		/// 写多个线圈请求
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="count">线圈数量</param>
		/// <param name="values">线圈值</param>
		public WriteMultipleCoilsRequest(ushort address, ushort count, bool[] values)
		{
			this.Address = address;
			this.Count = count;
			this.Values = values;
		}

		/// <summary>
		/// 获取起始地址
		/// </summary>
		public ushort Address { get; }

		/// <summary>
		/// 获取线圈数量
		/// </summary>
		public ushort Count { get; }

		/// <summary>
		/// 获取线圈值
		/// </summary>
		public bool[] Values { get; }

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.WriteMultipleCoils;

		/// <inheritdoc/>
		public override int Length => 1 + AddressFieldLength + 1 + this.ByteCount;

		private int ByteCount => (this.Count + 7) / 8;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			BinaryPrimitives.WriteUInt16BigEndian(destination[1..], this.Address);
			BinaryPrimitives.WriteUInt16BigEndian(destination[3..], this.Count);
			destination[5] = (byte)this.ByteCount;

			var payload = destination.Slice(6, this.ByteCount);
			payload.Clear();
			for (var i = 0; i < this.Count; i++)
			{
				if (this.Values[i])
				{
					payload[i / 8] |= (byte)(1 << (i % 8));
				}
			}
		}

		/// <summary>
		/// 解析请求
		/// </summary>
		/// <param name="data">数据域（不含功能码）</param>
		/// <returns></returns>
		public static WriteMultipleCoilsRequest Decode(ReadOnlySpan<byte> data)
		{
			var (address, count) = DecodeAddressAndCount(ModbusFunctionCode.WriteMultipleCoils, data, MaxCount);
			EnsureDataLength(ModbusFunctionCode.WriteMultipleCoils, data, AddressFieldLength + 1);

			var byteCount = data[AddressFieldLength];
			var expected = (count + 7) / 8;
			if (byteCount != expected)
			{
				throw ModbusSlaveException.IllegalDataValue($"FC15 的字节数 {byteCount} 与数量 {count} 不匹配，应为 {expected}");
			}
			EnsureDataLength(ModbusFunctionCode.WriteMultipleCoils, data, AddressFieldLength + 1 + byteCount);

			var payload = data.Slice(AddressFieldLength + 1, byteCount);
			var values = new bool[count];
			for (var i = 0; i < count; i++)
			{
				values[i] = (payload[i / 8] & (1 << (i % 8))) != 0;
			}

			return new WriteMultipleCoilsRequest(address, count, values);
		}
	}

	/// <summary>
	/// 写多个寄存器请求（FC16），数据域为「起始地址 + 数量 + 字节数 + 寄存器数据」
	/// </summary>
	public sealed class WriteMultipleRegistersRequest : ModbusRequest
	{
		/// <summary>
		/// 一次请求可写入的最大寄存器数
		/// </summary>
		public const int MaxCount = 123;

		/// <summary>
		/// 写多个寄存器请求
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="count">寄存器数量</param>
		/// <param name="values">寄存器值</param>
		public WriteMultipleRegistersRequest(ushort address, ushort count, ushort[] values)
		{
			this.Address = address;
			this.Count = count;
			this.Values = values;
		}

		/// <summary>
		/// 获取起始地址
		/// </summary>
		public ushort Address { get; }

		/// <summary>
		/// 获取寄存器数量
		/// </summary>
		public ushort Count { get; }

		/// <summary>
		/// 获取寄存器值
		/// </summary>
		public ushort[] Values { get; }

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.WriteMultipleRegisters;

		/// <inheritdoc/>
		public override int Length => 1 + AddressFieldLength + 1 + this.ByteCount;

		private int ByteCount => this.Count * 2;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			BinaryPrimitives.WriteUInt16BigEndian(destination[1..], this.Address);
			BinaryPrimitives.WriteUInt16BigEndian(destination[3..], this.Count);
			destination[5] = (byte)this.ByteCount;

			var payload = destination.Slice(6, this.ByteCount);
			for (var i = 0; i < this.Count; i++)
			{
				BinaryPrimitives.WriteUInt16BigEndian(payload[(i * 2)..], this.Values[i]);
			}
		}

		/// <summary>
		/// 解析请求
		/// </summary>
		/// <param name="data">数据域（不含功能码）</param>
		/// <returns></returns>
		public static WriteMultipleRegistersRequest Decode(ReadOnlySpan<byte> data)
		{
			var (address, count) = DecodeAddressAndCount(ModbusFunctionCode.WriteMultipleRegisters, data, MaxCount);
			EnsureDataLength(ModbusFunctionCode.WriteMultipleRegisters, data, AddressFieldLength + 1);

			var byteCount = data[AddressFieldLength];
			var expected = count * 2;
			if (byteCount != expected)
			{
				throw ModbusSlaveException.IllegalDataValue($"FC16 的字节数 {byteCount} 与数量 {count} 不匹配，应为 {expected}");
			}
			EnsureDataLength(ModbusFunctionCode.WriteMultipleRegisters, data, AddressFieldLength + 1 + byteCount);

			var payload = data.Slice(AddressFieldLength + 1, byteCount);
			var values = new ushort[count];
			for (var i = 0; i < count; i++)
			{
				values[i] = BinaryPrimitives.ReadUInt16BigEndian(payload[(i * 2)..]);
			}

			return new WriteMultipleRegistersRequest(address, count, values);
		}
	}
}
