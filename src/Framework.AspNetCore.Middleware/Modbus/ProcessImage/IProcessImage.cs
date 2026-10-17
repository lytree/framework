namespace Middleware.Modbus
{
	/// <summary>
	/// Modbus 从站进程映像：把总线上的四类数据区映射到从站内部数据
	/// </summary>
	/// <remarks>
	/// 其中线圈与保持寄存器对总线可读写（FC01/05/15 与 FC03/06/16），
	/// 离散输入与输入寄存器对总线只读（FC02/04），其值由应用侧写入。
	/// </remarks>
	public interface IProcessImage
	{
		/// <summary>
		/// 获取线圈数量（FC01/05/15）
		/// </summary>
		int CoilsCount { get; }

		/// <summary>
		/// 获取离散输入数量（FC02）
		/// </summary>
		int DiscreteInputsCount { get; }

		/// <summary>
		/// 获取保持寄存器数量（FC03/06/16）
		/// </summary>
		int HoldingRegistersCount { get; }

		/// <summary>
		/// 获取输入寄存器数量（FC04）
		/// </summary>
		int InputRegistersCount { get; }

		/// <summary>
		/// 读线圈
		/// </summary>
		/// <param name="address">地址</param>
		/// <returns></returns>
		bool ReadCoil(int address);

		/// <summary>
		/// 写线圈
		/// </summary>
		/// <param name="address">地址</param>
		/// <param name="value">值</param>
		void WriteCoil(int address, bool value);

		/// <summary>
		/// 读离散输入
		/// </summary>
		/// <param name="address">地址</param>
		/// <returns></returns>
		bool ReadDiscreteInput(int address);

		/// <summary>
		/// 写离散输入（由应用侧写入，总线侧只读）
		/// </summary>
		/// <param name="address">地址</param>
		/// <param name="value">值</param>
		void WriteDiscreteInput(int address, bool value);

		/// <summary>
		/// 读保持寄存器
		/// </summary>
		/// <param name="address">地址</param>
		/// <returns></returns>
		ushort ReadHoldingRegister(int address);

		/// <summary>
		/// 写保持寄存器
		/// </summary>
		/// <param name="address">地址</param>
		/// <param name="value">值</param>
		void WriteHoldingRegister(int address, ushort value);

		/// <summary>
		/// 读输入寄存器
		/// </summary>
		/// <param name="address">地址</param>
		/// <returns></returns>
		ushort ReadInputRegister(int address);

		/// <summary>
		/// 写输入寄存器（由应用侧写入，总线侧只读）
		/// </summary>
		/// <param name="address">地址</param>
		/// <param name="value">值</param>
		void WriteInputRegister(int address, ushort value);
	}
}
