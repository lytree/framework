using System;
using System.Buffers.Binary;

namespace Middleware.Modbus
{
	/// <summary>
	/// 表示 Modbus 请求（PDU）
	/// </summary>
	public abstract class ModbusRequest : ModbusPdu
	{
		/// <summary>
		/// 地址 + 数量/数值 组成的固定数据域长度
		/// </summary>
		protected const int AddressFieldLength = 4;

		/// <summary>
		/// 按功能码创建并解析请求 PDU
		/// </summary>
		/// <param name="pdu">请求 PDU（含功能码）</param>
		/// <returns></returns>
		/// <exception cref="ModbusSlaveException">功能码不支持或数据域非法</exception>
		public static ModbusRequest Create(ReadOnlySpan<byte> pdu)
		{
			if (pdu.IsEmpty)
			{
				throw ModbusSlaveException.IllegalDataValue("请求 PDU 为空");
			}

			var functionCode = pdu[0];
			var data = pdu[1..];
			return functionCode switch
			{
				ModbusFunctionCode.ReadCoils => ReadCoilsRequest.Decode(data),
				ModbusFunctionCode.ReadDiscreteInputs => ReadDiscreteInputsRequest.Decode(data),
				ModbusFunctionCode.ReadHoldingRegisters => ReadHoldingRegistersRequest.Decode(data),
				ModbusFunctionCode.ReadInputRegisters => ReadInputRegistersRequest.Decode(data),
				ModbusFunctionCode.WriteSingleCoil => WriteSingleCoilRequest.Decode(data),
				ModbusFunctionCode.WriteSingleRegister => WriteSingleRegisterRequest.Decode(data),
				ModbusFunctionCode.WriteMultipleCoils => WriteMultipleCoilsRequest.Decode(data),
				ModbusFunctionCode.WriteMultipleRegisters => WriteMultipleRegistersRequest.Decode(data),
				_ => throw ModbusSlaveException.IllegalFunction(functionCode),
			};
		}

		/// <summary>
		/// 校验数据域长度不小于指定值
		/// </summary>
		/// <param name="functionCode">功能码</param>
		/// <param name="data">数据域（不含功能码）</param>
		/// <param name="required">要求的最小长度</param>
		protected static void EnsureDataLength(byte functionCode, ReadOnlySpan<byte> data, int required)
		{
			if (data.Length < required)
			{
				throw ModbusSlaveException.TruncatedPdu(functionCode, required, data.Length);
			}
		}

		/// <summary>
		/// 读取数据域中指定偏移处的大端 UInt16
		/// </summary>
		/// <param name="data">数据域（不含功能码）</param>
		/// <param name="offset">偏移</param>
		/// <returns></returns>
		protected static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset)
		{
			return BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
		}

		/// <summary>
		/// 解析「起始地址 + 数量」并校验数量范围
		/// </summary>
		/// <param name="functionCode">功能码</param>
		/// <param name="data">数据域（不含功能码）</param>
		/// <param name="maxCount">一次请求允许的最大数量</param>
		/// <returns></returns>
		protected static (ushort Address, ushort Count) DecodeAddressAndCount(byte functionCode, ReadOnlySpan<byte> data, int maxCount)
		{
			EnsureDataLength(functionCode, data, AddressFieldLength);

			var address = ReadUInt16(data, 0);
			var count = ReadUInt16(data, 2);
			if (count < 1 || count > maxCount)
			{
				throw ModbusSlaveException.IllegalDataValue($"功能码 0x{functionCode:X2} 的数量 {count} 超出 1~{maxCount} 范围");
			}

			return (address, count);
		}
	}
}
