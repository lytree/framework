namespace Framework.Modbus.Abstractions
{
	/// <summary>
	/// Modbus 协议默认参数、尺寸约束与各类功能码的数量上限（主站与从站共用）。
	/// </summary>
	public static class ModbusConstants
	{
		/// <summary>默认从站地址（单元标识）。</summary>
		public const int DefaultUnitId = 1;

		/// <summary>默认事务标识（仅 TCP/UDP 有效）。</summary>
		public const int DefaultTransactionId = 0;

		/// <summary>默认重试次数。</summary>
		public const int DefaultRetries = 3;

		/// <summary>默认读写超时（毫秒）。</summary>
		public const int DefaultTimeout = 3000;

		/// <summary>默认发送间隔（毫秒），串口总线需要时可以拉大。</summary>
		public const int DefaultTransmitDelay = 0;

		/// <summary>Modbus TCP / UDP 默认端口。</summary>
		public const int DefaultPort = 502;

		/// <summary>PDU（功能码 + 数据）最大长度，Modbus 规范为 253 字节。</summary>
		public const int MaxPduSize = 253;

		/// <summary>串口帧最大长度（地址 + PDU + 校验）。</summary>
		public const int MaxSerialFrameSize = 256;

		/// <summary>Modbus TCP MBAP 报文头长度。</summary>
		public const int MbapHeaderSize = 7;

		/// <summary>MBAP 中的协议标识，Modbus 固定为 0。</summary>
		public const int TcpProtocolId = 0;

		/// <summary>FC01 单次最多读取的线圈数量。</summary>
		public const int MaxReadCoils = 2000;

		/// <summary>FC02 单次最多读取的离散输入数量。</summary>
		public const int MaxReadDiscretes = 2000;

		/// <summary>FC03 / FC04 单次最多读取的寄存器数量。</summary>
		public const int MaxReadRegisters = 125;

		/// <summary>FC15 单次最多写入的线圈数量。</summary>
		public const int MaxWriteCoils = 1968;

		/// <summary>FC16 单次最多写入的寄存器数量。</summary>
		public const int MaxWriteRegisters = 123;

		/// <summary>文件记录子请求的引用类型，Modbus 规范固定为 0x06。</summary>
		public const byte FileRecordReferenceType = 0x06;

		/// <summary>FC14 一次请求最多允许的文件记录子请求数（字节计数上限 245）。</summary>
		public const int MaxReadFileRecordSubRequests = 35;

		/// <summary>FC18 一次响应最多返回的 FIFO 寄存器数。</summary>
		public const int MaxFifoQueueCount = 31;

		/// <summary>ASCII 帧起始字符。</summary>
		public const char AsciiStart = ':';

		/// <summary>ASCII 帧结束符。</summary>
		public const string AsciiEnd = "\r\n";
	}
}
