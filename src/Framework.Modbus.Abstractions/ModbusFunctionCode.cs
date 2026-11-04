namespace Framework.Modbus.Abstractions
{
	/// <summary>
	/// Modbus 功能码定义（主站与从站共用的唯一取值来源）。
	/// </summary>
	/// <remarks>
	/// 同一功能码在主站侧沿用 jamod 的命名（如 <see cref="ReadInputDiscretes"/> / <see cref="WriteCoil"/>），
	/// 从站侧沿用协议描述式命名（如 <see cref="ReadDiscreteInputs"/> / <see cref="WriteSingleCoil"/>），两者值相同，互为别名。
	/// </remarks>
	public static class ModbusFunctionCode
	{
		/// <summary>读线圈（可读写的数字量输出），FC01。</summary>
		public const byte ReadCoils = 0x01;

		/// <summary>读离散输入（只读的数字量输入），FC02。</summary>
		public const byte ReadInputDiscretes = 0x02;

		/// <summary><see cref="ReadInputDiscretes"/> 的别名（从站侧命名）。</summary>
		public const byte ReadDiscreteInputs = ReadInputDiscretes;

		/// <summary>读保持寄存器，FC03。</summary>
		public const byte ReadMultipleRegisters = 0x03;

		/// <summary><see cref="ReadMultipleRegisters"/> 的别名（从站侧命名）。</summary>
		public const byte ReadHoldingRegisters = ReadMultipleRegisters;

		/// <summary>读输入寄存器，FC04。</summary>
		public const byte ReadInputRegisters = 0x04;

		/// <summary>写单个线圈，FC05。</summary>
		public const byte WriteCoil = 0x05;

		/// <summary><see cref="WriteCoil"/> 的别名（从站侧命名）。</summary>
		public const byte WriteSingleCoil = WriteCoil;

		/// <summary>写单个寄存器，FC06。</summary>
		public const byte WriteSingleRegister = 0x06;

		/// <summary>读异常状态，FC07（串行链路诊断）。</summary>
		public const byte ReadExceptionStatus = 0x07;

		/// <summary>诊断，FC08（串行链路诊断）。</summary>
		public const byte Diagnostics = 0x08;

		/// <summary>取通信事件计数器，FC0B。</summary>
		public const byte GetCommEventCounter = 0x0B;

		/// <summary>取通信事件记录，FC0C。</summary>
		public const byte GetCommEventLog = 0x0C;

		/// <summary>写多个线圈，FC0F。</summary>
		public const byte WriteMultipleCoils = 0x0F;

		/// <summary>写多个寄存器，FC10。</summary>
		public const byte WriteMultipleRegisters = 0x10;

		/// <summary>报告从站标识，FC11。</summary>
		public const byte ReportSlaveId = 0x11;

		/// <summary>读文件记录，FC14。</summary>
		public const byte ReadFileRecord = 0x14;

		/// <summary>写文件记录，FC15（0x15）。</summary>
		public const byte WriteFileRecord = 0x15;

		/// <summary>屏蔽写寄存器，FC16（0x16）。</summary>
		public const byte MaskWriteRegister = 0x16;

		/// <summary>读写多个寄存器，FC17。</summary>
		public const byte ReadWriteMultipleRegisters = 0x17;

		/// <summary>读 FIFO 队列，FC18。</summary>
		public const byte ReadFifoQueue = 0x18;

		/// <summary>异常响应功能码置位掩码：原功能码 | 0x80。</summary>
		public const byte ExceptionMask = 0x80;

		/// <summary><see cref="ExceptionMask"/> 的别名（从站侧命名）。</summary>
		public const byte ExceptionFlag = ExceptionMask;

		/// <summary>取功能码的可读名称。</summary>
		/// <param name="functionCode">功能码。</param>
		public static string GetName(byte functionCode) => functionCode switch
		{
			ReadCoils => "Read Coils",
			ReadInputDiscretes => "Read Input Discretes",
			ReadMultipleRegisters => "Read Multiple Registers",
			ReadInputRegisters => "Read Input Registers",
			WriteCoil => "Write Coil",
			WriteSingleRegister => "Write Single Register",
			ReadExceptionStatus => "Read Exception Status",
			Diagnostics => "Diagnostics",
			GetCommEventCounter => "Get Comm Event Counter",
			GetCommEventLog => "Get Comm Event Log",
			WriteMultipleCoils => "Write Multiple Coils",
			WriteMultipleRegisters => "Write Multiple Registers",
			ReportSlaveId => "Report Slave Id",
			ReadFileRecord => "Read File Record",
			WriteFileRecord => "Write File Record",
			MaskWriteRegister => "Mask Write Register",
			ReadWriteMultipleRegisters => "Read/Write Multiple Registers",
			ReadFifoQueue => "Read FIFO Queue",
			_ => (functionCode & ExceptionMask) != 0
				? $"Exception Response (0x{functionCode:X2})"
				: $"Unknown Function Code (0x{functionCode:X2})",
		};
	}
}
