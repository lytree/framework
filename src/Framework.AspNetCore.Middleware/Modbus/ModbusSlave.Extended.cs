using Framework.Modbus.Abstractions;
using System;
using System.Buffers.Binary;

namespace Middleware.Modbus
{
	/// <summary>
	/// Modbus 从站的扩展功能码处理（FC07 / 08 / 0B / 0C / 11 / 14 / 15 / 16 / 17 / 18）
	/// </summary>
	/// <remarks>
	/// <para>
	/// 需要外部状态的扩展功能码全部经由 <see cref="ModbusSlaveHosts"/> 的宿主接口获取，
	/// 未提供宿主时由内置兜底实现抛出 <see cref="ModbusExceptionCode.IllegalFunction"/>，
	/// 与扩展前的从站行为一致。
	/// </para>
	/// <para>
	/// FC16 / FC17 直接复用 <see cref="IProcessImage"/>，与核心功能码走同一条读写路径。
	/// </para>
	/// </remarks>
	public sealed partial class ModbusSlave
	{
		#region FC07 读异常状态

		private ReadExceptionStatusResponse ReadExceptionStatus(ReadExceptionStatusRequest request)
		{
			return new ReadExceptionStatusResponse(this.hosts.ExceptionStatus.GetExceptionStatus());
		}

		#endregion

		#region FC08 诊断

		private DiagnosticsResponse Diagnostics(DiagnosticsRequest request)
		{
			var subFunction = (int)request.SubFunction;
			switch (subFunction)
			{
				case ModbusDiagnosticsSubFunction.ReturnQueryData:
					// 回显请求数据，不依赖任何宿主状态
					return new DiagnosticsResponse(request.SubFunction, request.Data);

				case ModbusDiagnosticsSubFunction.ReturnDiagnosticRegister:
					return CreateCounterResponse(request.SubFunction, this.hosts.Diagnostics.GetDiagnosticRegister());

				case ModbusDiagnosticsSubFunction.ClearCountersAndDiagnosticRegister:
					this.hosts.Diagnostics.ClearCounters();
					return new DiagnosticsResponse(request.SubFunction, request.Data);

				case ModbusDiagnosticsSubFunction.ClearOverrunCounterAndFlag:
					this.hosts.Diagnostics.ClearOverrunCounter();
					return new DiagnosticsResponse(request.SubFunction, request.Data);

				default:
					if (subFunction >= ModbusDiagnosticsSubFunction.ReturnBusMessageCount &&
						subFunction <= ModbusDiagnosticsSubFunction.ReturnBusCharacterOverrunCount)
					{
						return CreateCounterResponse(request.SubFunction, this.hosts.Diagnostics.GetCounter(request.SubFunction));
					}

					// 0x0001 重启通信、0x0003 修改 ASCII 分隔符、0x0004 只听模式都需要传输层配合，
					// 当前从站不触碰 IO/传输层，按「不支持」返回非法功能。
					throw ModbusSlaveException.IllegalFunction(request.FunctionCode);
			}
		}

		private static DiagnosticsResponse CreateCounterResponse(ushort subFunction, ushort value)
		{
			var data = new byte[2];
			BinaryPrimitives.WriteUInt16BigEndian(data, value);
			return new DiagnosticsResponse(subFunction, data);
		}

		#endregion

		#region FC0B / FC0C 通信事件

		private GetCommEventCounterResponse GetCommEventCounter(GetCommEventCounterRequest request)
		{
			var host = this.hosts.CommunicationEvents;
			return new GetCommEventCounterResponse(host.GetStatus(), host.GetEventCount());
		}

		private GetCommEventLogResponse GetCommEventLog(GetCommEventLogRequest request)
		{
			var host = this.hosts.CommunicationEvents;
			var events = host.GetEvents();

			// 响应 PDU = 功能码 + 字节计数 + 6 字节固定域 + 事件字节
			if (8 + events.Length > ModbusConstants.MaxPduSize)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"通信事件记录长度 {events.Length} 使响应 PDU 超出上限 {ModbusConstants.MaxPduSize}");
			}

			return new GetCommEventLogResponse(host.GetStatus(), host.GetEventCount(), host.GetMessageCount(), events.Span);
		}

		#endregion

		#region FC11 报告从站标识

		private ReportSlaveIdResponse ReportSlaveId(ReportSlaveIdRequest request)
		{
			var data = this.hosts.Identity.GetSlaveIdData();

			// 响应 PDU = 功能码 + 字节计数 + 标识数据
			if (2 + data.Length > ModbusConstants.MaxPduSize)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"从站标识数据长度 {data.Length} 使响应 PDU 超出上限 {ModbusConstants.MaxPduSize}");
			}

			return new ReportSlaveIdResponse(data.Span);
		}

		#endregion

		#region FC14 / FC15 文件记录

		private ReadFileRecordResponse ReadFileRecord(ReadFileRecordRequest request)
		{
			var references = request.Records;
			var records = new ushort[references.Length][];
			var byteCount = 0;

			for (var i = 0; i < references.Length; i++)
			{
				var reference = references[i];
				var values = this.hosts.FileRecords.Read(reference.FileNumber, reference.RecordNumber, reference.RecordLength);
				if (values is null || values.Length != reference.RecordLength)
				{
					throw ModbusSlaveException.IllegalDataAddress(reference.RecordNumber, reference.RecordLength);
				}

				records[i] = values;
				byteCount += 2 + values.Length * 2;
			}

			// 响应 PDU = 功能码 + 字节计数 + 各子响应
			if (2 + byteCount > ModbusConstants.MaxPduSize)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"读文件记录响应长度 {2 + byteCount} 超出上限 {ModbusConstants.MaxPduSize}");
			}

			return new ReadFileRecordResponse(records);
		}

		private WriteFileRecordResponse WriteFileRecord(WriteFileRecordRequest request)
		{
			foreach (var record in request.Records)
			{
				if (this.hosts.FileRecords.Write(record.FileNumber, record.StartRecord, record.Values) == false)
				{
					throw ModbusSlaveException.IllegalDataAddress(record.StartRecord, record.Count);
				}
			}

			return new WriteFileRecordResponse(request.Records);
		}

		#endregion

		#region FC16 屏蔽写寄存器

		private MaskWriteRegisterResponse MaskWriteRegister(MaskWriteRegisterRequest request)
		{
			ValidateRange(request.Address, 1, this.processImage.HoldingRegistersCount);

			var oldValue = this.processImage.ReadHoldingRegister(request.Address);
			// 规范公式：结果 = (当前值 AND 与掩码) OR (或掩码 AND (NOT 与掩码))
			var newValue = (ushort)((oldValue & request.AndMask) | (request.OrMask & ~request.AndMask));
			this.processImage.WriteHoldingRegister(request.Address, newValue);
			this.RaiseValueChanged(ModbusDataArea.HoldingRegister, request.Address, oldValue, newValue, ModbusWriteSource.Bus);

			return new MaskWriteRegisterResponse(request.Address, request.AndMask, request.OrMask);
		}

		#endregion

		#region FC17 读写多个寄存器

		private ReadWriteMultipleRegistersResponse ReadWriteMultipleRegisters(ReadWriteMultipleRegistersRequest request)
		{
			// 规范要求先执行写操作，再执行读操作
			var writeValues = request.WriteValues;
			ValidateRange(request.WriteAddress, request.WriteCount, this.processImage.HoldingRegistersCount);
			for (var i = 0; i < writeValues.Length; i++)
			{
				var address = request.WriteAddress + i;
				var value = writeValues[i];
				var oldValue = this.processImage.ReadHoldingRegister(address);
				this.processImage.WriteHoldingRegister(address, value);
				this.RaiseValueChanged(ModbusDataArea.HoldingRegister, address, oldValue, value, ModbusWriteSource.Bus);
			}

			ValidateRange(request.ReadAddress, request.ReadCount, this.processImage.HoldingRegistersCount);
			var registers = new ushort[request.ReadCount];
			for (var i = 0; i < registers.Length; i++)
			{
				registers[i] = this.processImage.ReadHoldingRegister(request.ReadAddress + i);
			}

			return new ReadWriteMultipleRegistersResponse(registers);
		}

		#endregion

		#region FC18 读 FIFO 队列

		private ReadFifoQueueResponse ReadFifoQueue(ReadFifoQueueRequest request)
		{
			var values = this.hosts.FifoQueue.Read(request.Address);
			if (values is null)
			{
				throw ModbusSlaveException.IllegalDataAddress(request.Address, 1);
			}

			// 数量上限由 ReadFifoQueueResponse 构造校验（超出 31 个返回非法数据值）
			return new ReadFifoQueueResponse(values);
		}

		#endregion
	}
}
