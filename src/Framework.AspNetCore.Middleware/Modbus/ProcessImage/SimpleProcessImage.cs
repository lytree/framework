using System;

namespace Middleware.Modbus
{
	/// <summary>
	/// 基于定长数组的进程映像实现，默认覆盖 0~65535 的完整地址空间，可安全并发访问
	/// </summary>
	public sealed class SimpleProcessImage : IProcessImage
	{
		/// <summary>
		/// 默认的地址空间大小
		/// </summary>
		public const int DefaultSize = 65536;

		private readonly object syncRoot = new();
		private readonly bool[] coils;
		private readonly bool[] discreteInputs;
		private readonly ushort[] holdingRegisters;
		private readonly ushort[] inputRegisters;

		/// <summary>
		/// 使用默认地址空间大小创建进程映像
		/// </summary>
		public SimpleProcessImage()
			: this(DefaultSize, DefaultSize, DefaultSize, DefaultSize)
		{
		}

		/// <summary>
		/// 创建进程映像
		/// </summary>
		/// <param name="coils">线圈数量</param>
		/// <param name="discreteInputs">离散输入数量</param>
		/// <param name="holdingRegisters">保持寄存器数量</param>
		/// <param name="inputRegisters">输入寄存器数量</param>
		public SimpleProcessImage(int coils, int discreteInputs, int holdingRegisters, int inputRegisters)
		{
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(coils);
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(discreteInputs);
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(holdingRegisters);
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inputRegisters);

			this.coils = new bool[coils];
			this.discreteInputs = new bool[discreteInputs];
			this.holdingRegisters = new ushort[holdingRegisters];
			this.inputRegisters = new ushort[inputRegisters];
		}

		/// <inheritdoc/>
		public int CoilsCount => this.coils.Length;

		/// <inheritdoc/>
		public int DiscreteInputsCount => this.discreteInputs.Length;

		/// <inheritdoc/>
		public int HoldingRegistersCount => this.holdingRegisters.Length;

		/// <inheritdoc/>
		public int InputRegistersCount => this.inputRegisters.Length;

		/// <inheritdoc/>
		public bool ReadCoil(int address)
		{
			lock (this.syncRoot)
			{
				return this.coils[address];
			}
		}

		/// <inheritdoc/>
		public void WriteCoil(int address, bool value)
		{
			lock (this.syncRoot)
			{
				this.coils[address] = value;
			}
		}

		/// <inheritdoc/>
		public bool ReadDiscreteInput(int address)
		{
			lock (this.syncRoot)
			{
				return this.discreteInputs[address];
			}
		}

		/// <inheritdoc/>
		public void WriteDiscreteInput(int address, bool value)
		{
			lock (this.syncRoot)
			{
				this.discreteInputs[address] = value;
			}
		}

		/// <inheritdoc/>
		public ushort ReadHoldingRegister(int address)
		{
			lock (this.syncRoot)
			{
				return this.holdingRegisters[address];
			}
		}

		/// <inheritdoc/>
		public void WriteHoldingRegister(int address, ushort value)
		{
			lock (this.syncRoot)
			{
				this.holdingRegisters[address] = value;
			}
		}

		/// <inheritdoc/>
		public ushort ReadInputRegister(int address)
		{
			lock (this.syncRoot)
			{
				return this.inputRegisters[address];
			}
		}

		/// <inheritdoc/>
		public void WriteInputRegister(int address, ushort value)
		{
			lock (this.syncRoot)
			{
				this.inputRegisters[address] = value;
			}
		}
	}
}
