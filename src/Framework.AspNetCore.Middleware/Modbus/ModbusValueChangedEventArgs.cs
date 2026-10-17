using System;

namespace Middleware.Modbus
{
	/// <summary>
	/// 进程映像中的值发生变更时的事件参数
	/// </summary>
	public sealed class ModbusValueChangedEventArgs : EventArgs
	{
		/// <summary>
		/// 值变更事件参数
		/// </summary>
		/// <param name="area">数据区</param>
		/// <param name="address">地址（0 基）</param>
		/// <param name="oldValue">旧值</param>
		/// <param name="newValue">新值</param>
		/// <param name="source">写入来源</param>
		public ModbusValueChangedEventArgs(ModbusDataArea area, int address, ModbusValue oldValue, ModbusValue newValue, ModbusWriteSource source)
		{
			this.Area = area;
			this.Address = address;
			this.OldValue = oldValue;
			this.NewValue = newValue;
			this.Source = source;
		}

		/// <summary>
		/// 获取数据区
		/// </summary>
		public ModbusDataArea Area { get; }

		/// <summary>
		/// 获取地址（0 基）
		/// </summary>
		public int Address { get; }

		/// <summary>
		/// 获取旧值
		/// </summary>
		public ModbusValue OldValue { get; }

		/// <summary>
		/// 获取新值
		/// </summary>
		public ModbusValue NewValue { get; }

		/// <summary>
		/// 获取写入来源
		/// </summary>
		public ModbusWriteSource Source { get; }

		/// <inheritdoc/>
		public override string ToString() => $"{this.Area}[{this.Address}] {this.OldValue} -> {this.NewValue} ({this.Source})";
	}
}
