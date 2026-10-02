using System;
using System.Buffers.Binary;

namespace Middleware.Modbus
{
	/// <summary>
	/// 读请求基类，数据域为「起始地址 + 数量」
	/// </summary>
	public abstract class ReadRequest : ModbusRequest
	{
		/// <summary>
		/// 读请求
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="count">数量</param>
		protected ReadRequest(ushort address, ushort count)
		{
			this.Address = address;
			this.Count = count;
		}

		/// <summary>
		/// 获取起始地址
		/// </summary>
		public ushort Address { get; }

		/// <summary>
		/// 获取数量
		/// </summary>
		public ushort Count { get; }

		/// <inheritdoc/>
		public override int Length => 1 + AddressFieldLength;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			BinaryPrimitives.WriteUInt16BigEndian(destination[1..], this.Address);
			BinaryPrimitives.WriteUInt16BigEndian(destination[3..], this.Count);
		}
	}

	/// <summary>
	/// 读线圈请求（FC01），对应可读写的数字量输出
	/// </summary>
	public sealed class ReadCoilsRequest : ReadRequest
	{
		/// <summary>
		/// 一次请求可读取的最大线圈数
		/// </summary>
		public const int MaxCount = 2000;

		/// <summary>
		/// 读线圈请求
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="count">线圈数量</param>
		public ReadCoilsRequest(ushort address, ushort count)
			: base(address, count)
		{
		}

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.ReadCoils;

		/// <summary>
		/// 解析请求
		/// </summary>
		/// <param name="data">数据域（不含功能码）</param>
		/// <returns></returns>
		public static ReadCoilsRequest Decode(ReadOnlySpan<byte> data)
		{
			var (address, count) = DecodeAddressAndCount(ModbusFunctionCode.ReadCoils, data, MaxCount);
			return new ReadCoilsRequest(address, count);
		}
	}

	/// <summary>
	/// 读离散输入请求（FC02），对应只读的数字量输入
	/// </summary>
	public sealed class ReadDiscreteInputsRequest : ReadRequest
	{
		/// <summary>
		/// 一次请求可读取的最大离散输入数
		/// </summary>
		public const int MaxCount = 2000;

		/// <summary>
		/// 读离散输入请求
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="count">离散输入数量</param>
		public ReadDiscreteInputsRequest(ushort address, ushort count)
			: base(address, count)
		{
		}

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.ReadDiscreteInputs;

		/// <summary>
		/// 解析请求
		/// </summary>
		/// <param name="data">数据域（不含功能码）</param>
		/// <returns></returns>
		public static ReadDiscreteInputsRequest Decode(ReadOnlySpan<byte> data)
		{
			var (address, count) = DecodeAddressAndCount(ModbusFunctionCode.ReadDiscreteInputs, data, MaxCount);
			return new ReadDiscreteInputsRequest(address, count);
		}
	}

	/// <summary>
	/// 读保持寄存器请求（FC03），对应可读写的寄存器
	/// </summary>
	public sealed class ReadHoldingRegistersRequest : ReadRequest
	{
		/// <summary>
		/// 一次请求可读取的最大寄存器数
		/// </summary>
		public const int MaxCount = 125;

		/// <summary>
		/// 读保持寄存器请求
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="count">寄存器数量</param>
		public ReadHoldingRegistersRequest(ushort address, ushort count)
			: base(address, count)
		{
		}

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.ReadHoldingRegisters;

		/// <summary>
		/// 解析请求
		/// </summary>
		/// <param name="data">数据域（不含功能码）</param>
		/// <returns></returns>
		public static ReadHoldingRegistersRequest Decode(ReadOnlySpan<byte> data)
		{
			var (address, count) = DecodeAddressAndCount(ModbusFunctionCode.ReadHoldingRegisters, data, MaxCount);
			return new ReadHoldingRegistersRequest(address, count);
		}
	}

	/// <summary>
	/// 读输入寄存器请求（FC04），对应只读的寄存器
	/// </summary>
	public sealed class ReadInputRegistersRequest : ReadRequest
	{
		/// <summary>
		/// 一次请求可读取的最大寄存器数
		/// </summary>
		public const int MaxCount = 125;

		/// <summary>
		/// 读输入寄存器请求
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="count">寄存器数量</param>
		public ReadInputRegistersRequest(ushort address, ushort count)
			: base(address, count)
		{
		}

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.ReadInputRegisters;

		/// <summary>
		/// 解析请求
		/// </summary>
		/// <param name="data">数据域（不含功能码）</param>
		/// <returns></returns>
		public static ReadInputRegistersRequest Decode(ReadOnlySpan<byte> data)
		{
			var (address, count) = DecodeAddressAndCount(ModbusFunctionCode.ReadInputRegisters, data, MaxCount);
			return new ReadInputRegistersRequest(address, count);
		}
	}
}
