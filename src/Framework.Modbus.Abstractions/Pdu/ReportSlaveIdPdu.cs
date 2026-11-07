using System;

namespace Framework.Modbus.Abstractions
{
	/// <summary>
	/// 报告从站标识请求（FC11），请求不携带数据域。
	/// </summary>
	public sealed class ReportSlaveIdRequest : ModbusRequest
	{
		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.ReportSlaveId;

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
		/// <param name="data">数据域（不含功能码），FC11 必须为空。</param>
		/// <returns>解析出的请求。</returns>
		/// <exception cref="ModbusSlaveException">数据域不为空。</exception>
		public static ReportSlaveIdRequest Decode(ReadOnlySpan<byte> data)
		{
			if (!data.IsEmpty)
			{
				throw ModbusSlaveException.IllegalDataValue($"FC11 请求不携带数据域，实际长度为 {data.Length}");
			}

			return new ReportSlaveIdRequest();
		}
	}

	/// <summary>
	/// 报告从站标识响应（FC11），数据域为「字节计数 + 从站标识数据」。
	/// </summary>
	public sealed class ReportSlaveIdResponse : ModbusResponse
	{
		private readonly byte[] data;

		/// <summary>
		/// 报告从站标识响应。
		/// </summary>
		/// <param name="data">从站标识数据，通常以运行指示状态字节结尾。</param>
		public ReportSlaveIdResponse(ReadOnlySpan<byte> data)
		{
			this.data = data.ToArray();
		}

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.ReportSlaveId;

		/// <summary>
		/// 获取从站标识数据。
		/// </summary>
		public ReadOnlyMemory<byte> Data => this.data;

		/// <summary>
		/// 获取数据域的字节数。
		/// </summary>
		public int ByteCount => this.data.Length;

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
		/// 解析报告从站标识响应。
		/// </summary>
		/// <param name="expectedFunctionCode">期望的响应功能码。</param>
		/// <param name="pdu">响应 PDU（含功能码）。</param>
		/// <returns>解析出的响应。</returns>
		/// <exception cref="ModbusSlaveException">功能码不匹配或 PDU 非法。</exception>
		public static ReportSlaveIdResponse Decode(byte expectedFunctionCode, ReadOnlySpan<byte> pdu)
		{
			if (pdu.IsEmpty)
			{
				throw ModbusSlaveException.IllegalDataValue("报告从站标识响应 PDU 为空");
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
					$"报告从站标识响应声明 {byteCount} 字节数据，但实际只有 {pdu.Length - 2} 字节");
			}

			return new ReportSlaveIdResponse(pdu.Slice(2, byteCount));
		}
	}
}
