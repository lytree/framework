using Framework.Modbus.Abstractions;
using System;
using Abstractions = Framework.Modbus.Abstractions;

namespace Middleware.Modbus
{
	/// <summary>
	/// FC07 读异常状态的宿主：由业务提供从站的 8 位异常状态字。
	/// </summary>
	public interface IModbusSlaveExceptionStatusHost
	{
		/// <summary>
		/// 读取异常状态字，每位对应一个设备自定义的异常条件。
		/// </summary>
		/// <returns>异常状态字节。</returns>
		byte GetExceptionStatus();
	}

	/// <summary>
	/// FC08 诊断（Diagnostics）的宿主：提供诊断寄存器与各类诊断计数器。
	/// </summary>
	/// <remarks>
	/// 未注册该宿主时，只有子功能码 0x0000（返回查询数据）可用，其余子功能码返回
	/// <see cref="ModbusExceptionCode.IllegalFunction"/>。
	/// </remarks>
	public interface IModbusSlaveDiagnosticsHost
	{
		/// <summary>
		/// 读取诊断寄存器（子功能码 0x0002）。
		/// </summary>
		/// <returns>16 位诊断寄存器的值。</returns>
		ushort GetDiagnosticRegister();

		/// <summary>
		/// 读取指定子功能码对应的计数器（0x000B~0x0012）。
		/// </summary>
		/// <param name="subFunction">计数字功能码。</param>
		/// <returns>计数器当前值。</returns>
		ushort GetCounter(ushort subFunction);

		/// <summary>
		/// 清空全部计数器与诊断寄存器（子功能码 0x000A）。
		/// </summary>
		void ClearCounters();

		/// <summary>
		/// 清空字符溢出计数与标志（子功能码 0x0014）。
		/// </summary>
		void ClearOverrunCounter();
	}

	/// <summary>
	/// FC0B / FC0C 的宿主：提供通信事件计数器与通信事件记录。
	/// </summary>
	/// <remarks>
	/// 事件缓冲如何维护由业务决定；未注册该宿主时 FC0B / FC0C 返回
	/// <see cref="ModbusExceptionCode.IllegalFunction"/>，保持既有从站行为。
	/// </remarks>
	public interface IModbusSlaveCommunicationEventHost
	{
		/// <summary>
		/// 读取状态字：0x0000 表示就绪，0xFFFF 表示从站忙。
		/// </summary>
		/// <returns>状态字。</returns>
		ushort GetStatus();

		/// <summary>
		/// 读取事件计数。
		/// </summary>
		/// <returns>事件计数。</returns>
		ushort GetEventCount();

		/// <summary>
		/// 读取报文计数（FC0C 使用）。
		/// </summary>
		/// <returns>报文计数。</returns>
		ushort GetMessageCount();

		/// <summary>
		/// 读取事件字节，按时间顺序排列，每字节一个事件。
		/// </summary>
		/// <returns>事件字节。</returns>
		ReadOnlyMemory<byte> GetEvents();
	}

	/// <summary>
	/// FC11 报告从站标识的宿主：提供从站标识数据（通常以运行指示状态字节结尾）。
	/// </summary>
	public interface IModbusSlaveIdentityHost
	{
		/// <summary>
		/// 读取从站标识数据。
		/// </summary>
		/// <returns>从站标识数据。</returns>
		ReadOnlyMemory<byte> GetSlaveIdData();
	}

	/// <summary>
	/// FC14 / FC15 文件记录的宿主：以「文件号 + 记录号」为单位读写寄存器记录。
	/// </summary>
	public interface IModbusFileRecordStore
	{
		/// <summary>
		/// 读取指定文件的连续记录。
		/// </summary>
		/// <param name="fileNumber">文件号。</param>
		/// <param name="recordNumber">起始记录号。</param>
		/// <param name="recordLength">记录数。</param>
		/// <returns>读到的寄存器值；地址不存在时返回 <see langword="null"/>。</returns>
		ushort[]? Read(ushort fileNumber, ushort recordNumber, ushort recordLength);

		/// <summary>
		/// 从指定记录开始写入寄存器值。
		/// </summary>
		/// <param name="fileNumber">文件号。</param>
		/// <param name="startRecord">起始记录号。</param>
		/// <param name="values">要写入的寄存器值。</param>
		/// <returns>写入成功返回 <see langword="true"/>；地址不存在时返回 <see langword="false"/>。</returns>
		bool Write(ushort fileNumber, ushort startRecord, ReadOnlySpan<ushort> values);
	}

	/// <summary>
	/// FC18 读 FIFO 队列的宿主：按 FIFO 指针地址返回当前队列内容。
	/// </summary>
	public interface IModbusFifoQueueStore
	{
		/// <summary>
		/// 读取指定 FIFO 指针地址的队列内容，按先进先出顺序排列。
		/// </summary>
		/// <param name="address">FIFO 指针地址。</param>
		/// <returns>队列内容；地址不存在时返回 <see langword="null"/>。</returns>
		ushort[]? Read(ushort address);
	}

	/// <summary>
	/// 从站扩展功能码的宿主捆绑。
	/// </summary>
	/// <remarks>
	/// <para>
	/// 所有成员都是可选的扩展点：构造函数传入 <see langword="null"/> 时使用内置的安全兜底实现，
	/// 该实现在被调用时抛出 <see cref="ModbusSlaveException.IllegalFunction(byte)"/>，
	/// 从而保持「未实现 = 非法功能」的旧语义，而不是让从站崩溃。
	/// </para>
	/// <para>
	/// FC16 / FC17 完全基于 <see cref="IProcessImage"/>，不需要额外宿主。
	/// </para>
	/// </remarks>
	public sealed class ModbusSlaveHosts
	{
		/// <summary>
		/// 全部宿主都使用兜底实现的默认捆绑。
		/// </summary>
		public static ModbusSlaveHosts Default { get; } = new ModbusSlaveHosts();

		/// <summary>
		/// 创建宿主捆绑；未提供的宿主使用安全兜底实现。
		/// </summary>
		/// <param name="exceptionStatus">FC07 异常状态宿主。</param>
		/// <param name="diagnostics">FC08 诊断宿主。</param>
		/// <param name="communicationEvents">FC0B / FC0C 通信事件宿主。</param>
		/// <param name="identity">FC11 从站标识宿主。</param>
		/// <param name="fileRecords">FC14 / FC15 文件记录宿主。</param>
		/// <param name="fifoQueue">FC18 FIFO 队列宿主。</param>
		public ModbusSlaveHosts(
			IModbusSlaveExceptionStatusHost? exceptionStatus = null,
			IModbusSlaveDiagnosticsHost? diagnostics = null,
			IModbusSlaveCommunicationEventHost? communicationEvents = null,
			IModbusSlaveIdentityHost? identity = null,
			IModbusFileRecordStore? fileRecords = null,
			IModbusFifoQueueStore? fifoQueue = null)
		{
			this.ExceptionStatus = exceptionStatus ?? UnsupportedExceptionStatusHost.Instance;
			this.Diagnostics = diagnostics ?? UnsupportedDiagnosticsHost.Instance;
			this.CommunicationEvents = communicationEvents ?? UnsupportedCommunicationEventHost.Instance;
			this.Identity = identity ?? UnsupportedIdentityHost.Instance;
			this.FileRecords = fileRecords ?? UnsupportedFileRecordStore.Instance;
			this.FifoQueue = fifoQueue ?? UnsupportedFifoQueueStore.Instance;
		}

		/// <summary>
		/// 获取 FC07 异常状态宿主。
		/// </summary>
		public IModbusSlaveExceptionStatusHost ExceptionStatus { get; }

		/// <summary>
		/// 获取 FC08 诊断宿主。
		/// </summary>
		public IModbusSlaveDiagnosticsHost Diagnostics { get; }

		/// <summary>
		/// 获取 FC0B / FC0C 通信事件宿主。
		/// </summary>
		public IModbusSlaveCommunicationEventHost CommunicationEvents { get; }

		/// <summary>
		/// 获取 FC11 从站标识宿主。
		/// </summary>
		public IModbusSlaveIdentityHost Identity { get; }

		/// <summary>
		/// 获取 FC14 / FC15 文件记录宿主。
		/// </summary>
		public IModbusFileRecordStore FileRecords { get; }

		/// <summary>
		/// 获取 FC18 FIFO 队列宿主。
		/// </summary>
		public IModbusFifoQueueStore FifoQueue { get; }
	}

	/// <summary>
	/// 未提供 FC07 宿主时的兜底实现：始终返回非法功能。
	/// </summary>
	internal sealed class UnsupportedExceptionStatusHost : IModbusSlaveExceptionStatusHost
	{
		public static UnsupportedExceptionStatusHost Instance { get; } = new UnsupportedExceptionStatusHost();

		public byte GetExceptionStatus()
		{
			throw ModbusSlaveException.IllegalFunction(Abstractions.ModbusFunctionCode.ReadExceptionStatus);
		}
	}

	/// <summary>
	/// 未提供 FC08 宿主时的兜底实现：始终返回非法功能。
	/// </summary>
	internal sealed class UnsupportedDiagnosticsHost : IModbusSlaveDiagnosticsHost
	{
		public static UnsupportedDiagnosticsHost Instance { get; } = new UnsupportedDiagnosticsHost();

		public ushort GetDiagnosticRegister()
		{
			throw ModbusSlaveException.IllegalFunction(Abstractions.ModbusFunctionCode.Diagnostics);
		}

		public ushort GetCounter(ushort subFunction)
		{
			throw ModbusSlaveException.IllegalFunction(Abstractions.ModbusFunctionCode.Diagnostics);
		}

		public void ClearCounters()
		{
			throw ModbusSlaveException.IllegalFunction(Abstractions.ModbusFunctionCode.Diagnostics);
		}

		public void ClearOverrunCounter()
		{
			throw ModbusSlaveException.IllegalFunction(Abstractions.ModbusFunctionCode.Diagnostics);
		}
	}

	/// <summary>
	/// 未提供 FC0B / FC0C 宿主时的兜底实现：始终返回非法功能。
	/// </summary>
	internal sealed class UnsupportedCommunicationEventHost : IModbusSlaveCommunicationEventHost
	{
		public static UnsupportedCommunicationEventHost Instance { get; } = new UnsupportedCommunicationEventHost();

		public ushort GetStatus()
		{
			throw ModbusSlaveException.IllegalFunction(Abstractions.ModbusFunctionCode.GetCommEventCounter);
		}

		public ushort GetEventCount()
		{
			throw ModbusSlaveException.IllegalFunction(Abstractions.ModbusFunctionCode.GetCommEventCounter);
		}

		public ushort GetMessageCount()
		{
			throw ModbusSlaveException.IllegalFunction(Abstractions.ModbusFunctionCode.GetCommEventLog);
		}

		public ReadOnlyMemory<byte> GetEvents()
		{
			throw ModbusSlaveException.IllegalFunction(Abstractions.ModbusFunctionCode.GetCommEventLog);
		}
	}

	/// <summary>
	/// 未提供 FC11 宿主时的兜底实现：始终返回非法功能。
	/// </summary>
	internal sealed class UnsupportedIdentityHost : IModbusSlaveIdentityHost
	{
		public static UnsupportedIdentityHost Instance { get; } = new UnsupportedIdentityHost();

		public ReadOnlyMemory<byte> GetSlaveIdData()
		{
			throw ModbusSlaveException.IllegalFunction(Abstractions.ModbusFunctionCode.ReportSlaveId);
		}
	}

	/// <summary>
	/// 未提供 FC14 / FC15 宿主时的兜底实现：始终返回非法功能。
	/// </summary>
	internal sealed class UnsupportedFileRecordStore : IModbusFileRecordStore
	{
		public static UnsupportedFileRecordStore Instance { get; } = new UnsupportedFileRecordStore();

		public ushort[]? Read(ushort fileNumber, ushort recordNumber, ushort recordLength)
		{
			throw ModbusSlaveException.IllegalFunction(Abstractions.ModbusFunctionCode.ReadFileRecord);
		}

		public bool Write(ushort fileNumber, ushort startRecord, ReadOnlySpan<ushort> values)
		{
			throw ModbusSlaveException.IllegalFunction(Abstractions.ModbusFunctionCode.WriteFileRecord);
		}
	}

	/// <summary>
	/// 未提供 FC18 宿主时的兜底实现：始终返回非法功能。
	/// </summary>
	internal sealed class UnsupportedFifoQueueStore : IModbusFifoQueueStore
	{
		public static UnsupportedFifoQueueStore Instance { get; } = new UnsupportedFifoQueueStore();

		public ushort[]? Read(ushort address)
		{
			throw ModbusSlaveException.IllegalFunction(Abstractions.ModbusFunctionCode.ReadFifoQueue);
		}
	}
}
