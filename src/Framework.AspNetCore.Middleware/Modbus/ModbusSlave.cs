using Microsoft.Extensions.Logging;
using System;

namespace Middleware.Modbus
{
	/// <summary>
	/// Modbus 从站：把请求 PDU 分派到进程映像，并生成响应 PDU
	/// </summary>
	/// <remarks>
	/// 进程内直通访问（不经总线直接读写映像、值变更事件）见 ModbusSlave.DirectAccess.cs
	/// </remarks>
	public sealed partial class ModbusSlave
	{
		private readonly ModbusSlaveOptions options;
		private readonly IProcessImage processImage;
		private readonly ILogger<ModbusSlave> logger;

		/// <summary>
		/// Modbus 从站
		/// </summary>
		/// <param name="options">从站选项</param>
		/// <param name="processImage">进程映像</param>
		/// <param name="logger">日志</param>
		public ModbusSlave(ModbusSlaveOptions options, IProcessImage processImage, ILogger<ModbusSlave> logger)
		{
			this.options = options;
			this.processImage = processImage;
			this.logger = logger;
		}

		/// <summary>
		/// 获取从站选项
		/// </summary>
		public ModbusSlaveOptions Options => this.options;

		/// <summary>
		/// 获取进程映像
		/// </summary>
		public IProcessImage ProcessImage => this.processImage;

		/// <summary>
		/// 处理一个请求 PDU 并返回响应 PDU
		/// </summary>
		/// <param name="unitId">请求的单元标识</param>
		/// <param name="pdu">请求 PDU（含功能码）</param>
		/// <returns>响应 PDU；返回 null 表示本从站不应答（地址不匹配或广播被抑制）</returns>
		public ModbusResponse? Execute(byte unitId, ReadOnlySpan<byte> pdu)
		{
			if (pdu.IsEmpty)
			{
				return null;
			}

			var functionCode = pdu[0];
			var isBroadcast = unitId == 0;
			if (isBroadcast == false && this.options.UnitId != 0 && unitId != this.options.UnitId)
			{
				this.logger.LogDebug("忽略非本从站的请求：单元标识 {UnitId}，本机地址 {LocalUnitId}", unitId, this.options.UnitId);
				return null;
			}

			ModbusResponse response;
			try
			{
				var request = ModbusRequest.Create(pdu);
				response = this.Dispatch(request);
			}
			catch (ModbusSlaveException ex)
			{
				this.logger.LogWarning("功能码 0x{FunctionCode:X2} 处理失败，返回异常码 0x{ExceptionCode:X2}：{Reason}", functionCode, ex.ExceptionCode, ex.Message);
				response = new ModbusExceptionResponse(functionCode, ex.ExceptionCode);
			}
			catch (Exception ex)
			{
				this.logger.LogError(ex, "处理功能码 0x{FunctionCode:X2} 时发生内部错误", functionCode);
				response = new ModbusExceptionResponse(functionCode, ModbusExceptionCode.SlaveDeviceFailure);
			}

			if (isBroadcast && this.options.SuppressBroadcastResponse)
			{
				this.logger.LogDebug("收到广播（功能码 0x{FunctionCode:X2}），已抑制响应", functionCode);
				return null;
			}

			return response;
		}

		private ModbusResponse Dispatch(ModbusRequest request)
		{
			return request switch
			{
				ReadCoilsRequest r => this.ReadBits(r.FunctionCode, r.Address, r.Count, isCoil: true),
				ReadDiscreteInputsRequest r => this.ReadBits(r.FunctionCode, r.Address, r.Count, isCoil: false),
				ReadHoldingRegistersRequest r => this.ReadRegisters(r.FunctionCode, r.Address, r.Count, isHolding: true),
				ReadInputRegistersRequest r => this.ReadRegisters(r.FunctionCode, r.Address, r.Count, isHolding: false),
				WriteSingleCoilRequest r => this.WriteSingleCoil(r),
				WriteSingleRegisterRequest r => this.WriteSingleRegister(r),
				WriteMultipleCoilsRequest r => this.WriteMultipleCoils(r),
				WriteMultipleRegistersRequest r => this.WriteMultipleRegisters(r),
				_ => throw ModbusSlaveException.IllegalFunction(request.FunctionCode),
			};
		}

		private ReadBitsResponse ReadBits(byte functionCode, ushort address, ushort count, bool isCoil)
		{
			var total = isCoil ? this.processImage.CoilsCount : this.processImage.DiscreteInputsCount;
			ValidateRange(address, count, total);

			var data = new byte[(count + 7) / 8];
			for (var i = 0; i < count; i++)
			{
				var value = isCoil
					? this.processImage.ReadCoil(address + i)
					: this.processImage.ReadDiscreteInput(address + i);
				if (value)
				{
					data[i / 8] |= (byte)(1 << (i % 8));
				}
			}

			return new ReadBitsResponse(functionCode, data);
		}

		private ReadRegistersResponse ReadRegisters(byte functionCode, ushort address, ushort count, bool isHolding)
		{
			var total = isHolding ? this.processImage.HoldingRegistersCount : this.processImage.InputRegistersCount;
			ValidateRange(address, count, total);

			var registers = new ushort[count];
			for (var i = 0; i < count; i++)
			{
				registers[i] = isHolding
					? this.processImage.ReadHoldingRegister(address + i)
					: this.processImage.ReadInputRegister(address + i);
			}

			return new ReadRegistersResponse(functionCode, registers);
		}

		private WriteSingleResponse WriteSingleCoil(WriteSingleCoilRequest request)
		{
			ValidateRange(request.Address, 1, this.processImage.CoilsCount);
			var oldValue = this.processImage.ReadCoil(request.Address);
			this.processImage.WriteCoil(request.Address, request.Value);
			this.RaiseValueChanged(ModbusDataArea.Coil, request.Address, oldValue, request.Value, ModbusWriteSource.Bus);
			return new WriteSingleResponse(request.FunctionCode, request.Address, request.RawValue);
		}

		private WriteSingleResponse WriteSingleRegister(WriteSingleRegisterRequest request)
		{
			ValidateRange(request.Address, 1, this.processImage.HoldingRegistersCount);
			var oldValue = this.processImage.ReadHoldingRegister(request.Address);
			this.processImage.WriteHoldingRegister(request.Address, request.Value);
			this.RaiseValueChanged(ModbusDataArea.HoldingRegister, request.Address, oldValue, request.Value, ModbusWriteSource.Bus);
			return new WriteSingleResponse(request.FunctionCode, request.Address, request.Value);
		}

		private WriteMultipleResponse WriteMultipleCoils(WriteMultipleCoilsRequest request)
		{
			ValidateRange(request.Address, request.Count, this.processImage.CoilsCount);
			for (var i = 0; i < request.Count; i++)
			{
				var address = request.Address + i;
				var value = request.Values[i];
				var oldValue = this.processImage.ReadCoil(address);
				this.processImage.WriteCoil(address, value);
				this.RaiseValueChanged(ModbusDataArea.Coil, address, oldValue, value, ModbusWriteSource.Bus);
			}
			return new WriteMultipleResponse(request.FunctionCode, request.Address, request.Count);
		}

		private WriteMultipleResponse WriteMultipleRegisters(WriteMultipleRegistersRequest request)
		{
			ValidateRange(request.Address, request.Count, this.processImage.HoldingRegistersCount);
			for (var i = 0; i < request.Count; i++)
			{
				var address = request.Address + i;
				var value = request.Values[i];
				var oldValue = this.processImage.ReadHoldingRegister(address);
				this.processImage.WriteHoldingRegister(address, value);
				this.RaiseValueChanged(ModbusDataArea.HoldingRegister, address, oldValue, value, ModbusWriteSource.Bus);
			}
			return new WriteMultipleResponse(request.FunctionCode, request.Address, request.Count);
		}

		private static void ValidateRange(int address, int count, int total)
		{
			if (address < 0 || count < 1 || address + count > total)
			{
				throw ModbusSlaveException.IllegalDataAddress(address, count);
			}
		}
	}
}
