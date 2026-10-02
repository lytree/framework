using Microsoft.Extensions.Logging;
using System;
using System.Buffers.Binary;

namespace Middleware.Modbus
{
	/// <summary>
	/// Modbus 从站的进程内直通访问：不经总线，直接读写进程映像
	/// </summary>
	/// <remarks>
	/// <para>
	/// 面向两类场景：
	/// </para>
	/// <list type="bullet">
	/// <item>数据供给方在进程内（采集程序、上位机、数据库轮询、仿真器），把数据直接灌进从站映像，供总线上的客户端读取；</item>
	/// <item>业务侧需要感知总线下发的写入（如 FC05/06/15/16 命令），通过 <see cref="ValueChanged"/> 订阅。</item>
	/// </list>
	/// <para>
	/// 与总线路径的差别：这里的越界一律抛 <see cref="ArgumentOutOfRangeException"/>（调用方是本地代码，
	/// 用异常更利于定位问题）；而总线路径把越界翻译成 Modbus 异常码 <c>IllegalDataAddress(0x02)</c>。
	/// </para>
	/// <para>
	/// 事件仅在值真正发生变化时触发；写入相同值不产生通知。
	/// </para>
	/// </remarks>
	public sealed partial class ModbusSlave
	{
		private EventHandler<ModbusValueChangedEventArgs>? valueChanged;

		/// <summary>
		/// 进程映像中的值发生变更时触发，总线写入与进程内赋值都会触发
		/// </summary>
		/// <remarks>
		/// 订阅者抛出的异常会被记录并吞掉，不影响从站与总线的正常工作
		/// </remarks>
		public event EventHandler<ModbusValueChangedEventArgs>? ValueChanged
		{
			add => this.valueChanged += value;
			remove => this.valueChanged -= value;
		}

		#region 线圈（FC01/05/15）

		/// <summary>
		/// 直接读取线圈
		/// </summary>
		/// <param name="address">地址（0 基）</param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException">地址越界</exception>
		public bool GetCoil(int address)
		{
			ValidateInternalRange(address, 1, this.processImage.CoilsCount, nameof(address));
			return this.processImage.ReadCoil(address);
		}

		/// <summary>
		/// 直接写入线圈
		/// </summary>
		/// <param name="address">地址（0 基）</param>
		/// <param name="value">值</param>
		/// <exception cref="ArgumentOutOfRangeException">地址越界</exception>
		public void SetCoil(int address, bool value)
		{
			ValidateInternalRange(address, 1, this.processImage.CoilsCount, nameof(address));
			var oldValue = this.processImage.ReadCoil(address);
			this.processImage.WriteCoil(address, value);
			this.RaiseValueChanged(ModbusDataArea.Coil, address, oldValue, value, ModbusWriteSource.Internal);
		}

		/// <summary>
		/// 直接批量写入线圈
		/// </summary>
		/// <param name="address">起始地址（0 基）</param>
		/// <param name="values">值序列</param>
		/// <exception cref="ArgumentOutOfRangeException">地址区间越界</exception>
		public void SetCoils(int address, ReadOnlySpan<bool> values)
		{
			ValidateInternalRange(address, values.Length, this.processImage.CoilsCount, nameof(values));
			for (var i = 0; i < values.Length; i++)
			{
				var current = address + i;
				var oldValue = this.processImage.ReadCoil(current);
				this.processImage.WriteCoil(current, values[i]);
				this.RaiseValueChanged(ModbusDataArea.Coil, current, oldValue, values[i], ModbusWriteSource.Internal);
			}
		}

		#endregion

		#region 离散输入（FC02）

		/// <summary>
		/// 直接读取离散输入
		/// </summary>
		/// <param name="address">地址（0 基）</param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException">地址越界</exception>
		public bool GetDiscreteInput(int address)
		{
			ValidateInternalRange(address, 1, this.processImage.DiscreteInputsCount, nameof(address));
			return this.processImage.ReadDiscreteInput(address);
		}

		/// <summary>
		/// 直接写入离散输入（总线侧只读，仅供本地填充数据）
		/// </summary>
		/// <param name="address">地址（0 基）</param>
		/// <param name="value">值</param>
		/// <exception cref="ArgumentOutOfRangeException">地址越界</exception>
		public void SetDiscreteInput(int address, bool value)
		{
			ValidateInternalRange(address, 1, this.processImage.DiscreteInputsCount, nameof(address));
			var oldValue = this.processImage.ReadDiscreteInput(address);
			this.processImage.WriteDiscreteInput(address, value);
			this.RaiseValueChanged(ModbusDataArea.DiscreteInput, address, oldValue, value, ModbusWriteSource.Internal);
		}

		/// <summary>
		/// 直接批量写入离散输入
		/// </summary>
		/// <param name="address">起始地址（0 基）</param>
		/// <param name="values">值序列</param>
		/// <exception cref="ArgumentOutOfRangeException">地址区间越界</exception>
		public void SetDiscreteInputs(int address, ReadOnlySpan<bool> values)
		{
			ValidateInternalRange(address, values.Length, this.processImage.DiscreteInputsCount, nameof(values));
			for (var i = 0; i < values.Length; i++)
			{
				var current = address + i;
				var oldValue = this.processImage.ReadDiscreteInput(current);
				this.processImage.WriteDiscreteInput(current, values[i]);
				this.RaiseValueChanged(ModbusDataArea.DiscreteInput, current, oldValue, values[i], ModbusWriteSource.Internal);
			}
		}

		#endregion

		#region 保持寄存器（FC03/06/16）

		/// <summary>
		/// 直接读取保持寄存器
		/// </summary>
		/// <param name="address">地址（0 基）</param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException">地址越界</exception>
		public ushort GetHoldingRegister(int address)
		{
			ValidateInternalRange(address, 1, this.processImage.HoldingRegistersCount, nameof(address));
			return this.processImage.ReadHoldingRegister(address);
		}

		/// <summary>
		/// 直接写入保持寄存器
		/// </summary>
		/// <param name="address">地址（0 基）</param>
		/// <param name="value">值</param>
		/// <exception cref="ArgumentOutOfRangeException">地址越界</exception>
		public void SetHoldingRegister(int address, ushort value)
		{
			ValidateInternalRange(address, 1, this.processImage.HoldingRegistersCount, nameof(address));
			var oldValue = this.processImage.ReadHoldingRegister(address);
			this.processImage.WriteHoldingRegister(address, value);
			this.RaiseValueChanged(ModbusDataArea.HoldingRegister, address, oldValue, value, ModbusWriteSource.Internal);
		}

		/// <summary>
		/// 直接批量写入保持寄存器
		/// </summary>
		/// <param name="address">起始地址（0 基）</param>
		/// <param name="values">值序列</param>
		/// <exception cref="ArgumentOutOfRangeException">地址区间越界</exception>
		public void SetHoldingRegisters(int address, ReadOnlySpan<ushort> values)
		{
			ValidateInternalRange(address, values.Length, this.processImage.HoldingRegistersCount, nameof(values));
			for (var i = 0; i < values.Length; i++)
			{
				var current = address + i;
				var oldValue = this.processImage.ReadHoldingRegister(current);
				this.processImage.WriteHoldingRegister(current, values[i]);
				this.RaiseValueChanged(ModbusDataArea.HoldingRegister, current, oldValue, values[i], ModbusWriteSource.Internal);
			}
		}

		/// <summary>
		/// 以「大端字节流」直接写入保持寄存器，便于把设备返回的原始字节直接灌入映像
		/// </summary>
		/// <param name="address">起始地址（0 基）</param>
		/// <param name="bigEndianBytes">大端字节序列，长度必须为偶数</param>
		/// <exception cref="ArgumentException">字节数不是偶数</exception>
		/// <exception cref="ArgumentOutOfRangeException">地址区间越界</exception>
		public void SetHoldingRegisters(int address, ReadOnlySpan<byte> bigEndianBytes)
		{
			var count = EnsureEvenLength(bigEndianBytes, nameof(bigEndianBytes));
			ValidateInternalRange(address, count, this.processImage.HoldingRegistersCount, nameof(bigEndianBytes));
			for (var i = 0; i < count; i++)
			{
				this.SetHoldingRegister(address + i, BinaryPrimitives.ReadUInt16BigEndian(bigEndianBytes.Slice(i * 2, 2)));
			}
		}

		#endregion

		#region 输入寄存器（FC04）

		/// <summary>
		/// 直接读取输入寄存器
		/// </summary>
		/// <param name="address">地址（0 基）</param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException">地址越界</exception>
		public ushort GetInputRegister(int address)
		{
			ValidateInternalRange(address, 1, this.processImage.InputRegistersCount, nameof(address));
			return this.processImage.ReadInputRegister(address);
		}

		/// <summary>
		/// 直接写入输入寄存器（总线侧只读，仅供本地填充数据）
		/// </summary>
		/// <param name="address">地址（0 基）</param>
		/// <param name="value">值</param>
		/// <exception cref="ArgumentOutOfRangeException">地址越界</exception>
		public void SetInputRegister(int address, ushort value)
		{
			ValidateInternalRange(address, 1, this.processImage.InputRegistersCount, nameof(address));
			var oldValue = this.processImage.ReadInputRegister(address);
			this.processImage.WriteInputRegister(address, value);
			this.RaiseValueChanged(ModbusDataArea.InputRegister, address, oldValue, value, ModbusWriteSource.Internal);
		}

		/// <summary>
		/// 直接批量写入输入寄存器
		/// </summary>
		/// <param name="address">起始地址（0 基）</param>
		/// <param name="values">值序列</param>
		/// <exception cref="ArgumentOutOfRangeException">地址区间越界</exception>
		public void SetInputRegisters(int address, ReadOnlySpan<ushort> values)
		{
			ValidateInternalRange(address, values.Length, this.processImage.InputRegistersCount, nameof(values));
			for (var i = 0; i < values.Length; i++)
			{
				var current = address + i;
				var oldValue = this.processImage.ReadInputRegister(current);
				this.processImage.WriteInputRegister(current, values[i]);
				this.RaiseValueChanged(ModbusDataArea.InputRegister, current, oldValue, values[i], ModbusWriteSource.Internal);
			}
		}

		/// <summary>
		/// 以「大端字节流」直接写入输入寄存器，便于把设备返回的原始字节直接灌入映像
		/// </summary>
		/// <param name="address">起始地址（0 基）</param>
		/// <param name="bigEndianBytes">大端字节序列，长度必须为偶数</param>
		/// <exception cref="ArgumentException">字节数不是偶数</exception>
		/// <exception cref="ArgumentOutOfRangeException">地址区间越界</exception>
		public void SetInputRegisters(int address, ReadOnlySpan<byte> bigEndianBytes)
		{
			var count = EnsureEvenLength(bigEndianBytes, nameof(bigEndianBytes));
			ValidateInternalRange(address, count, this.processImage.InputRegistersCount, nameof(bigEndianBytes));
			for (var i = 0; i < count; i++)
			{
				this.SetInputRegister(address + i, BinaryPrimitives.ReadUInt16BigEndian(bigEndianBytes.Slice(i * 2, 2)));
			}
		}

		#endregion

		#region 通用入口

		/// <summary>
		/// 按数据区直接写入一个值（四类数据区的统一入口）
		/// </summary>
		/// <param name="area">数据区</param>
		/// <param name="address">地址（0 基）</param>
		/// <param name="value">值；位区取 <see cref="ModbusValue.AsBoolean"/>，寄存器区取 <see cref="ModbusValue.AsUInt16"/></param>
		/// <exception cref="ArgumentOutOfRangeException">地址越界</exception>
		public void SetValue(ModbusDataArea area, int address, ModbusValue value)
		{
			switch (area)
			{
				case ModbusDataArea.Coil:
					this.SetCoil(address, value.AsBoolean);
					break;
				case ModbusDataArea.DiscreteInput:
					this.SetDiscreteInput(address, value.AsBoolean);
					break;
				case ModbusDataArea.HoldingRegister:
					this.SetHoldingRegister(address, value.AsUInt16);
					break;
				case ModbusDataArea.InputRegister:
					this.SetInputRegister(address, value.AsUInt16);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(area), area, "未知的数据区");
			}
		}

		/// <summary>
		/// 按数据区直接读取一个值（四类数据区的统一入口）
		/// </summary>
		/// <param name="area">数据区</param>
		/// <param name="address">地址（0 基）</param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException">地址越界</exception>
		public ModbusValue GetValue(ModbusDataArea area, int address)
		{
			return area switch
			{
				ModbusDataArea.Coil => new ModbusValue(this.GetCoil(address)),
				ModbusDataArea.DiscreteInput => new ModbusValue(this.GetDiscreteInput(address)),
				ModbusDataArea.HoldingRegister => new ModbusValue(this.GetHoldingRegister(address)),
				ModbusDataArea.InputRegister => new ModbusValue(this.GetInputRegister(address)),
				_ => throw new ArgumentOutOfRangeException(nameof(area), area, "未知的数据区"),
			};
		}

		/// <summary>
		/// 把某类数据区整体清零
		/// </summary>
		/// <param name="area">数据区</param>
		/// <param name="address">起始地址（0 基）</param>
		/// <param name="count">数量</param>
		/// <exception cref="ArgumentOutOfRangeException">地址区间越界</exception>
		public void Clear(ModbusDataArea area, int address, int count)
		{
			var total = GetAreaCount(this.processImage, area);
			ValidateInternalRange(address, count, total, nameof(count));

			for (var i = 0; i < count; i++)
			{
				// SetValue 会走 SetXxx 通路，值发生变化时自行触发变更事件
				this.SetValue(area, address + i, default);
			}
		}

		#endregion

		private static int GetAreaCount(IProcessImage image, ModbusDataArea area)
		{
			return area switch
			{
				ModbusDataArea.Coil => image.CoilsCount,
				ModbusDataArea.DiscreteInput => image.DiscreteInputsCount,
				ModbusDataArea.HoldingRegister => image.HoldingRegistersCount,
				ModbusDataArea.InputRegister => image.InputRegistersCount,
				_ => throw new ArgumentOutOfRangeException(nameof(area), area, "未知的数据区"),
			};
		}

		private static void ValidateInternalRange(int address, int count, int total, string paramName)
		{
			if (address < 0 || count < 0 || address + count > total)
			{
				throw new ArgumentOutOfRangeException(paramName, $"地址区间 [{address}, {address + count}) 超出数据区范围 [0, {total})");
			}
		}

		private static int EnsureEvenLength(ReadOnlySpan<byte> bytes, string paramName)
		{
			if (bytes.Length % 2 != 0)
			{
				throw new ArgumentException("字节数必须为偶数（每个寄存器占 2 字节）", paramName);
			}

			return bytes.Length / 2;
		}

		/// <summary>
		/// 触发值变更事件；值未变化时静默返回，订阅者的异常不会向外扩散
		/// </summary>
		private void RaiseValueChanged(ModbusDataArea area, int address, ModbusValue oldValue, ModbusValue newValue, ModbusWriteSource source)
		{
			if (oldValue == newValue)
			{
				return;
			}

			var handler = this.valueChanged;
			if (handler is null)
			{
				return;
			}

			try
			{
				handler(this, new ModbusValueChangedEventArgs(area, address, oldValue, newValue, source));
			}
			catch (Exception ex)
			{
				this.logger.LogError(ex, "处理进程映像变更事件失败：{Area}[{Address}] {OldValue} -> {NewValue}", area, address, oldValue, newValue);
			}
		}
	}
}
