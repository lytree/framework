using System;

namespace Middleware.Modbus
{
	/// <summary>
	/// 进程映像中的单个值：既可能是位（线圈 / 离散输入），也可能是 16 位寄存器
	/// </summary>
	/// <remarks>
	/// 用结构体而不是 <see cref="object"/> 承载，避免变更事件在高频写入路径上产生装箱
	/// </remarks>
	public readonly struct ModbusValue : IEquatable<ModbusValue>
	{
		private readonly ushort raw;

		/// <summary>
		/// 用位值创建
		/// </summary>
		/// <param name="value">位值</param>
		public ModbusValue(bool value)
		{
			this.IsBit = true;
			this.raw = value ? (ushort)1 : (ushort)0;
		}

		/// <summary>
		/// 用寄存器值创建
		/// </summary>
		/// <param name="value">寄存器值</param>
		public ModbusValue(ushort value)
		{
			this.IsBit = false;
			this.raw = value;
		}

		/// <summary>
		/// 获取是否位值（线圈 / 离散输入）
		/// </summary>
		public bool IsBit { get; }

		/// <summary>
		/// 获取位值；若本值实际是寄存器，则非零即 true
		/// </summary>
		public bool AsBoolean => this.raw != 0;

		/// <summary>
		/// 获取寄存器值；若本值实际是位，则返回 0 或 1
		/// </summary>
		public ushort AsUInt16 => this.raw;

		/// <summary>
		/// 从位值隐式转换
		/// </summary>
		/// <param name="value">位值</param>
		public static implicit operator ModbusValue(bool value) => new(value);

		/// <summary>
		/// 从寄存器值隐式转换
		/// </summary>
		/// <param name="value">寄存器值</param>
		public static implicit operator ModbusValue(ushort value) => new(value);

		/// <inheritdoc/>
		public bool Equals(ModbusValue other) => this.IsBit == other.IsBit && this.raw == other.raw;

		/// <inheritdoc/>
		public override bool Equals(object? obj) => obj is ModbusValue other && this.Equals(other);

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(this.IsBit, this.raw);

		/// <summary>
		/// 相等比较
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		public static bool operator ==(ModbusValue left, ModbusValue right) => left.Equals(right);

		/// <summary>
		/// 不等比较
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		public static bool operator !=(ModbusValue left, ModbusValue right) => left.Equals(right) == false;

		/// <inheritdoc/>
		public override string ToString() => this.IsBit ? (this.raw == 0 ? "false" : "true") : this.raw.ToString();
	}
}
