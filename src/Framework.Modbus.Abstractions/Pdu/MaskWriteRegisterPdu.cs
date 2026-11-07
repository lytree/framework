using System;
using System.Buffers.Binary;

namespace Framework.Modbus.Abstractions
{
	/// <summary>
	/// 屏蔽写寄存器请求（FC16），数据域为「地址 + 与掩码 + 或掩码」。
	/// </summary>
	public sealed class MaskWriteRegisterRequest : ModbusRequest
	{
		/// <summary>
		/// 屏蔽写寄存器请求。
		/// </summary>
		/// <param name="address">寄存器地址。</param>
		/// <param name="andMask">与掩码。</param>
		/// <param name="orMask">或掩码。</param>
		public MaskWriteRegisterRequest(ushort address, ushort andMask, ushort orMask)
		{
			this.Address = address;
			this.AndMask = andMask;
			this.OrMask = orMask;
		}

		/// <summary>
		/// 获取寄存器地址。
		/// </summary>
		public ushort Address { get; }

		/// <summary>
		/// 获取与掩码。
		/// </summary>
		public ushort AndMask { get; }

		/// <summary>
		/// 获取或掩码。
		/// </summary>
		public ushort OrMask { get; }

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.MaskWriteRegister;

		/// <inheritdoc/>
		public override int Length => 7;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			BinaryPrimitives.WriteUInt16BigEndian(destination[1..], this.Address);
			BinaryPrimitives.WriteUInt16BigEndian(destination[3..], this.AndMask);
			BinaryPrimitives.WriteUInt16BigEndian(destination[5..], this.OrMask);
		}

		/// <summary>
		/// 解析请求。
		/// </summary>
		/// <param name="data">数据域（不含功能码）。</param>
		/// <returns>解析出的请求。</returns>
		/// <exception cref="ModbusSlaveException">数据域长度非法。</exception>
		public static MaskWriteRegisterRequest Decode(ReadOnlySpan<byte> data)
		{
			if (data.Length < 6)
			{
				throw ModbusSlaveException.TruncatedPdu(ModbusFunctionCode.MaskWriteRegister, 6, data.Length);
			}

			if (data.Length != 6)
			{
				throw ModbusSlaveException.IllegalDataValue($"屏蔽写寄存器请求的数据域长度应为 6，实际为 {data.Length}");
			}

			return new MaskWriteRegisterRequest(
				ReadUInt16(data, 0),
				ReadUInt16(data, 2),
				ReadUInt16(data, 4));
		}
	}

	/// <summary>
	/// 屏蔽写寄存器响应（FC16），回显请求中的地址与两个掩码。
	/// </summary>
	public sealed class MaskWriteRegisterResponse : ModbusResponse
	{
		/// <summary>
		/// 屏蔽写寄存器响应。
		/// </summary>
		/// <param name="address">寄存器地址。</param>
		/// <param name="andMask">与掩码。</param>
		/// <param name="orMask">或掩码。</param>
		public MaskWriteRegisterResponse(ushort address, ushort andMask, ushort orMask)
		{
			this.Address = address;
			this.AndMask = andMask;
			this.OrMask = orMask;
		}

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.MaskWriteRegister;

		/// <summary>
		/// 获取寄存器地址。
		/// </summary>
		public ushort Address { get; }

		/// <summary>
		/// 获取与掩码。
		/// </summary>
		public ushort AndMask { get; }

		/// <summary>
		/// 获取或掩码。
		/// </summary>
		public ushort OrMask { get; }

		/// <inheritdoc/>
		public override int Length => 7;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			BinaryPrimitives.WriteUInt16BigEndian(destination[1..], this.Address);
			BinaryPrimitives.WriteUInt16BigEndian(destination[3..], this.AndMask);
			BinaryPrimitives.WriteUInt16BigEndian(destination[5..], this.OrMask);
		}

		/// <summary>
		/// 解析响应。
		/// </summary>
		/// <param name="expectedFunctionCode">期望的响应功能码。</param>
		/// <param name="pdu">响应 PDU（含功能码）。</param>
		/// <returns>解析出的响应。</returns>
		/// <exception cref="ModbusSlaveException">功能码不匹配或 PDU 长度不足。</exception>
		public static MaskWriteRegisterResponse Decode(byte expectedFunctionCode, ReadOnlySpan<byte> pdu)
		{
			if (pdu.IsEmpty)
			{
				throw ModbusSlaveException.IllegalDataValue("屏蔽写寄存器响应 PDU 为空");
			}

			if (pdu[0] != expectedFunctionCode)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"功能码不匹配：期望 0x{expectedFunctionCode:X2}，实际 0x{pdu[0]:X2}");
			}

			if (pdu.Length < 7)
			{
				throw ModbusSlaveException.TruncatedPdu(expectedFunctionCode, 7, pdu.Length);
			}

			return new MaskWriteRegisterResponse(
				BinaryPrimitives.ReadUInt16BigEndian(pdu[1..]),
				BinaryPrimitives.ReadUInt16BigEndian(pdu[3..]),
				BinaryPrimitives.ReadUInt16BigEndian(pdu[5..]));
		}
	}
}
