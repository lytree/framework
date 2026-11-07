namespace Framework.Modbus.Abstractions
{
	/// <summary>
	/// FC08 诊断功能的子功能码（串行链路诊断）。
	/// </summary>
	public static class ModbusDiagnosticsSubFunction
	{
		/// <summary>回送查询数据（0x0000），从站原样回送请求中的数据。</summary>
		public const int ReturnQueryData = 0x0000;

		/// <summary>重启通信选项（0x0001），用于清除端口或恢复出厂设置。</summary>
		public const int RestartCommunicationsOption = 0x0001;

		/// <summary>返回诊断寄存器（0x0002）。</summary>
		public const int ReturnDiagnosticRegister = 0x0002;

		/// <summary>修改 ASCII 输入分隔符（0x0003）。</summary>
		public const int ChangeAsciiInputDelimiter = 0x0003;

		/// <summary>强制进入只听模式（0x0004），从站不再产生响应。</summary>
		public const int ForceListenOnlyMode = 0x0004;

		/// <summary>清除计数器与诊断寄存器（0x000A）。</summary>
		public const int ClearCountersAndDiagnosticRegister = 0x000A;

		/// <summary>返回总线报文计数（0x000B）。</summary>
		public const int ReturnBusMessageCount = 0x000B;

		/// <summary>返回总线通信错误计数（0x000C）。</summary>
		public const int ReturnBusCommunicationErrorCount = 0x000C;

		/// <summary>返回总线异常错误计数（0x000D）。</summary>
		public const int ReturnBusExceptionErrorCount = 0x000D;

		/// <summary>返回从站报文计数（0x000E）。</summary>
		public const int ReturnSlaveMessageCount = 0x000E;

		/// <summary>返回从站无响应计数（0x000F）。</summary>
		public const int ReturnSlaveNoResponseCount = 0x000F;

		/// <summary>返回从站 NAK 计数（0x0010）。</summary>
		public const int ReturnSlaveNakCount = 0x0010;

		/// <summary>返回从站忙计数（0x0011）。</summary>
		public const int ReturnSlaveBusyCount = 0x0011;

		/// <summary>返回总线字符溢出错计数（0x0012）。</summary>
		public const int ReturnBusCharacterOverrunCount = 0x0012;

		/// <summary>清除溢出计数器与标志（0x0014）。</summary>
		public const int ClearOverrunCounterAndFlag = 0x0014;
	}
}
