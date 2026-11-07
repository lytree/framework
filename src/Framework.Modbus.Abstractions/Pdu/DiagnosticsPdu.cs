using System;
using System.Buffers.Binary;

namespace Framework.Modbus.Abstractions
{
	/// <summary>
	/// 读异常状态请求（FC07），请求不携带数据域。
	/// </summary>
	public sealed class ReadExceptionStatusRequest : ModbusRequest
	{
		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.ReadExceptionStatus;

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
		/// <param name="data">数据域（不含功能码），FC07 必须为空。</param>
		/// <returns>解析出的请求。</returns>
		/// <exception cref="ModbusSlaveException">数据域不为空。</exception>
		public static ReadExceptionStatusRequest Decode(ReadOnlySpan<byte> data)
		{
			if (!data.IsEmpty)
			{
				throw ModbusSlaveException.IllegalDataValue($"FC07 请求不携带数据域，实际长度为 {data.Length}");
			}

			return new ReadExceptionStatusRequest();
		}
	}

	/// <summary>
	/// 读异常状态响应（FC07），数据域为单个状态字节。
	/// </summary>
	public sealed class ReadExceptionStatusResponse : ModbusResponse
	{
		/// <summary>
		/// 读异常状态响应。
		/// </summary>
		/// <param name="status">异常状态字节，每位对应一个异常标志。</param>
		public ReadExceptionStatusResponse(byte status)
		{
			this.Status = status;
		}

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.ReadExceptionStatus;

		/// <summary>
		/// 获取异常状态字节。
		/// </summary>
		public byte Status { get; }

		/// <inheritdoc/>
		public override int Length => 2;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			destination[1] = this.Status;
		}

		/// <summary>
		/// 解析读异常状态响应。
		/// </summary>
		/// <param name="expectedFunctionCode">期望的响应功能码。</param>
		/// <param name="pdu">响应 PDU（含功能码）。</param>
		/// <returns>解析出的响应。</returns>
		/// <exception cref="ModbusSlaveException">功能码不匹配或 PDU 非法。</exception>
		public static ReadExceptionStatusResponse Decode(byte expectedFunctionCode, ReadOnlySpan<byte> pdu)
		{
			if (pdu.IsEmpty)
			{
				throw ModbusSlaveException.IllegalDataValue("读异常状态响应 PDU 为空");
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

			return new ReadExceptionStatusResponse(pdu[1]);
		}
	}

	/// <summary>
	/// 诊断请求（FC08），数据域为「子功能码 + 诊断数据」。
	/// </summary>
	public sealed class DiagnosticsRequest : ModbusRequest
	{
		private readonly byte[] data;

		/// <summary>
		/// 诊断请求。
		/// </summary>
		/// <param name="subFunction">诊断子功能码。</param>
		/// <param name="data">诊断数据。</param>
		public DiagnosticsRequest(ushort subFunction, ReadOnlySpan<byte> data)
		{
			this.SubFunction = subFunction;
			this.data = data.ToArray();
		}

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.Diagnostics;

		/// <summary>
		/// 获取诊断子功能码。
		/// </summary>
		public ushort SubFunction { get; }

		/// <summary>
		/// 获取诊断数据。
		/// </summary>
		public ReadOnlySpan<byte> Data => this.data;

		/// <summary>
		/// 获取诊断数据的首个 16 位字（大端），数据不足 2 字节时返回 0。
		/// </summary>
		public ushort DataWord => this.data.Length < 2
			? (ushort)0
			: BinaryPrimitives.ReadUInt16BigEndian(this.data);

		/// <inheritdoc/>
		public override int Length => 3 + this.data.Length;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			BinaryPrimitives.WriteUInt16BigEndian(destination[1..], this.SubFunction);
			this.data.CopyTo(destination[3..]);
		}

		/// <summary>
		/// 解析请求。
		/// </summary>
		/// <param name="data">数据域（不含功能码）。</param>
		/// <returns>解析出的请求。</returns>
		/// <exception cref="ModbusSlaveException">数据域长度不足。</exception>
		public static DiagnosticsRequest Decode(ReadOnlySpan<byte> data)
		{
			EnsureDataLength(ModbusFunctionCode.Diagnostics, data, 4);

			return new DiagnosticsRequest(ReadUInt16(data, 0), data[2..]);
		}
	}

	/// <summary>
	/// 诊断响应（FC08），数据域为「子功能码 + 诊断数据」，布局与请求一致。
	/// </summary>
	public sealed class DiagnosticsResponse : ModbusResponse
	{
		private readonly byte[] data;

		/// <summary>
		/// 诊断响应。
		/// </summary>
		/// <param name="subFunction">诊断子功能码。</param>
		/// <param name="data">诊断数据。</param>
		public DiagnosticsResponse(ushort subFunction, ReadOnlySpan<byte> data)
		{
			this.SubFunction = subFunction;
			this.data = data.ToArray();
		}

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.Diagnostics;

		/// <summary>
		/// 获取诊断子功能码。
		/// </summary>
		public ushort SubFunction { get; }

		/// <summary>
		/// 获取诊断数据。
		/// </summary>
		public ReadOnlySpan<byte> Data => this.data;

		/// <summary>
		/// 获取诊断数据的首个 16 位字（大端），数据不足 2 字节时返回 0。
		/// </summary>
		public ushort DataWord => this.data.Length < 2
			? (ushort)0
			: BinaryPrimitives.ReadUInt16BigEndian(this.data);

		/// <inheritdoc/>
		public override int Length => 3 + this.data.Length;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			BinaryPrimitives.WriteUInt16BigEndian(destination[1..], this.SubFunction);
			this.data.CopyTo(destination[3..]);
		}

		/// <summary>
		/// 解析诊断响应。
		/// </summary>
		/// <param name="expectedFunctionCode">期望的响应功能码。</param>
		/// <param name="pdu">响应 PDU（含功能码）。</param>
		/// <returns>解析出的响应。</returns>
		/// <exception cref="ModbusSlaveException">功能码不匹配或 PDU 非法。</exception>
		public static DiagnosticsResponse Decode(byte expectedFunctionCode, ReadOnlySpan<byte> pdu)
		{
			if (pdu.IsEmpty)
			{
				throw ModbusSlaveException.IllegalDataValue("诊断响应 PDU 为空");
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

			return new DiagnosticsResponse(
				BinaryPrimitives.ReadUInt16BigEndian(pdu[1..]),
				pdu[3..]);
		}
	}
}
