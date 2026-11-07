using System;
using System.Buffers.Binary;

namespace Framework.Modbus.Abstractions
{
	/// <summary>
	/// 文件记录引用，描述 FC14 读文件记录请求中的一个子请求。
	/// </summary>
	public readonly struct FileRecordReference
	{
		/// <summary>
		/// 文件记录引用。
		/// </summary>
		/// <param name="fileNumber">文件号。</param>
		/// <param name="recordNumber">起始记录号。</param>
		/// <param name="recordLength">要读取的记录数。</param>
		public FileRecordReference(ushort fileNumber, ushort recordNumber, ushort recordLength)
		{
			this.FileNumber = fileNumber;
			this.RecordNumber = recordNumber;
			this.RecordLength = recordLength;
		}

		/// <summary>
		/// 获取文件号。
		/// </summary>
		public ushort FileNumber { get; }

		/// <summary>
		/// 获取起始记录号。
		/// </summary>
		public ushort RecordNumber { get; }

		/// <summary>
		/// 获取要读取的记录数。
		/// </summary>
		public ushort RecordLength { get; }
	}

	/// <summary>
	/// 文件记录写入，描述 FC15 写文件记录请求中的一个子记录。
	/// </summary>
	public readonly struct FileRecordWrite
	{
		private readonly ushort[] values;

		/// <summary>
		/// 文件记录写入。
		/// </summary>
		/// <param name="fileNumber">文件号。</param>
		/// <param name="startRecord">起始记录号。</param>
		/// <param name="values">要写入的寄存器值，至少 1 个。</param>
		/// <exception cref="ModbusSlaveException">寄存器值为空或不含任何元素。</exception>
		public FileRecordWrite(ushort fileNumber, ushort startRecord, ushort[] values)
		{
			if (values is null)
			{
				throw ModbusSlaveException.IllegalDataValue("文件记录写入的寄存器值不能为空");
			}

			if (values.Length < 1)
			{
				throw ModbusSlaveException.IllegalDataValue("文件记录写入的寄存器数量至少为 1");
			}

			this.FileNumber = fileNumber;
			this.StartRecord = startRecord;
			this.values = values;
		}

		/// <summary>
		/// 获取文件号。
		/// </summary>
		public ushort FileNumber { get; }

		/// <summary>
		/// 获取起始记录号。
		/// </summary>
		public ushort StartRecord { get; }

		/// <summary>
		/// 获取寄存器值视图。
		/// </summary>
		public ReadOnlySpan<ushort> Values => this.values;

		/// <summary>
		/// 获取寄存器数量。
		/// </summary>
		public int Count => this.values.Length;
	}

	/// <summary>
	/// 写文件记录请求与响应的共享编解码辅助实现。
	/// </summary>
	internal static class FileRecordCodec
	{
		/// <summary>
		/// 校验写文件记录的子记录集合，并检查编码后的 PDU 是否超出协议上限。
		/// </summary>
		/// <param name="records">子记录集合。</param>
		/// <exception cref="ModbusSlaveException">集合为空、子记录无值或 PDU 超长。</exception>
		internal static void EnsureWriteRecords(FileRecordWrite[] records)
		{
			if (records is null || records.Length == 0)
			{
				throw ModbusSlaveException.IllegalDataValue("写文件记录至少需要 1 条子记录");
			}

			var byteCount = 0;
			for (var i = 0; i < records.Length; i++)
			{
				if (records[i].Count < 1)
				{
					throw ModbusSlaveException.IllegalDataValue($"写文件记录的第 {i} 条子记录的寄存器数量至少为 1");
				}

				byteCount += 7 + records[i].Count * 2;
			}

			if (2 + byteCount > ModbusConstants.MaxPduSize)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"写文件记录的 PDU 长度为 {2 + byteCount}，超出上限 {ModbusConstants.MaxPduSize}");
			}
		}

		/// <summary>
		/// 计算写文件记录的数据域字节数。
		/// </summary>
		/// <param name="records">子记录集合。</param>
		/// <returns>字节数。</returns>
		internal static int ComputeWriteByteCount(FileRecordWrite[] records)
		{
			var byteCount = 0;
			for (var i = 0; i < records.Length; i++)
			{
				byteCount += 7 + records[i].Count * 2;
			}

			return byteCount;
		}

		/// <summary>
		/// 编码写文件记录 PDU。
		/// </summary>
		/// <param name="functionCode">功能码。</param>
		/// <param name="records">子记录集合。</param>
		/// <param name="destination">目标缓冲区。</param>
		internal static void EncodeWrite(byte functionCode, FileRecordWrite[] records, Span<byte> destination)
		{
			var byteCount = ComputeWriteByteCount(records);
			destination[0] = functionCode;
			destination[1] = (byte)byteCount;

			var position = 2;
			for (var i = 0; i < records.Length; i++)
			{
				var record = records[i];
				destination[position] = ModbusConstants.FileRecordReferenceType;
				BinaryPrimitives.WriteUInt16BigEndian(destination[(position + 1)..], record.FileNumber);
				BinaryPrimitives.WriteUInt16BigEndian(destination[(position + 3)..], record.StartRecord);
				BinaryPrimitives.WriteUInt16BigEndian(destination[(position + 5)..], (ushort)record.Count);

				var values = record.Values;
				for (var j = 0; j < values.Length; j++)
				{
					BinaryPrimitives.WriteUInt16BigEndian(destination[(position + 7 + j * 2)..], values[j]);
				}

				position += 7 + record.Count * 2;
			}
		}

		/// <summary>
		/// 解析写文件记录的数据域。
		/// </summary>
		/// <param name="functionCode">功能码。</param>
		/// <param name="data">数据域（不含功能码）。</param>
		/// <returns>解析出的子记录集合。</returns>
		/// <exception cref="ModbusSlaveException">数据域非法。</exception>
		internal static FileRecordWrite[] DecodeWriteData(byte functionCode, ReadOnlySpan<byte> data)
		{
			if (data.Length < 8)
			{
				throw ModbusSlaveException.TruncatedPdu(functionCode, 8, data.Length);
			}

			var byteCount = data[0];
			if (data.Length < 1 + byteCount)
			{
				throw ModbusSlaveException.TruncatedPdu(functionCode, 1 + byteCount, data.Length);
			}

			if (data.Length != 1 + byteCount)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"功能码 0x{functionCode:X2} 的数据域长度应为 {1 + byteCount}，实际为 {data.Length}");
			}

			var payload = data.Slice(1, byteCount);

			var recordCount = 0;
			var position = 0;
			while (position < byteCount)
			{
				var remaining = byteCount - position;
				if (remaining < 7)
				{
					throw ModbusSlaveException.IllegalDataValue(
						$"功能码 0x{functionCode:X2} 的子记录剩余 {remaining} 字节，不足 7 字节的记录头");
				}

				if (payload[position] != ModbusConstants.FileRecordReferenceType)
				{
					throw ModbusSlaveException.IllegalDataValue(
						$"功能码 0x{functionCode:X2} 的子记录引用类型 0x{payload[position]:X2} 非法，应为 0x{ModbusConstants.FileRecordReferenceType:X2}");
				}

				var itemCount = BinaryPrimitives.ReadUInt16BigEndian(payload[(position + 5)..]);
				if (itemCount < 1)
				{
					throw ModbusSlaveException.IllegalDataValue(
						$"功能码 0x{functionCode:X2} 的子记录寄存器数量 {itemCount} 非法，至少为 1");
				}

				var total = 7 + itemCount * 2;
				if (remaining < total)
				{
					throw ModbusSlaveException.IllegalDataValue(
						$"功能码 0x{functionCode:X2} 的子记录需要 {total} 字节，剩余 {remaining} 字节");
				}

				recordCount++;
				position += total;
			}

			if (position != byteCount)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"功能码 0x{functionCode:X2} 的子记录消耗 {position} 字节，与字节计数 {byteCount} 不符");
			}

			var records = new FileRecordWrite[recordCount];
			position = 0;
			for (var i = 0; i < recordCount; i++)
			{
				var itemCount = (int)BinaryPrimitives.ReadUInt16BigEndian(payload[(position + 5)..]);
				var values = new ushort[itemCount];
				for (var j = 0; j < itemCount; j++)
				{
					values[j] = BinaryPrimitives.ReadUInt16BigEndian(payload[(position + 7 + j * 2)..]);
				}

				records[i] = new FileRecordWrite(
					BinaryPrimitives.ReadUInt16BigEndian(payload[(position + 1)..]),
					BinaryPrimitives.ReadUInt16BigEndian(payload[(position + 3)..]),
					values);

				position += 7 + itemCount * 2;
			}

			return records;
		}
	}

	/// <summary>
	/// 读文件记录请求（FC14），数据域为「字节计数 + 若干文件记录引用」。
	/// </summary>
	public sealed class ReadFileRecordRequest : ModbusRequest
	{
		private readonly FileRecordReference[] records;

		/// <summary>
		/// 读文件记录请求。
		/// </summary>
		/// <param name="records">文件记录引用列表。</param>
		/// <exception cref="ModbusSlaveException">引用列表为空、数量超限或记录长度非法。</exception>
		public ReadFileRecordRequest(ReadOnlySpan<FileRecordReference> records)
		{
			if (records.IsEmpty)
			{
				throw ModbusSlaveException.IllegalDataValue("读文件记录请求至少需要 1 个文件记录引用");
			}

			if (records.Length > ModbusConstants.MaxReadFileRecordSubRequests)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"读文件记录请求的子请求数 {records.Length} 超出上限 {ModbusConstants.MaxReadFileRecordSubRequests}");
			}

			for (var i = 0; i < records.Length; i++)
			{
				if (records[i].RecordLength < 1)
				{
					throw ModbusSlaveException.IllegalDataValue($"读文件记录请求的第 {i} 个引用的记录长度至少为 1");
				}
			}

			this.records = records.ToArray();
		}

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.ReadFileRecord;

		/// <summary>
		/// 获取文件记录引用列表。
		/// </summary>
		public ReadOnlySpan<FileRecordReference> Records => this.records;

		/// <summary>
		/// 获取数据域的字节数。
		/// </summary>
		public int ByteCount => 7 * this.records.Length;

		/// <inheritdoc/>
		public override int Length => 2 + this.ByteCount;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			destination[1] = (byte)this.ByteCount;

			var position = 2;
			for (var i = 0; i < this.records.Length; i++)
			{
				var record = this.records[i];
				destination[position] = ModbusConstants.FileRecordReferenceType;
				BinaryPrimitives.WriteUInt16BigEndian(destination[(position + 1)..], record.FileNumber);
				BinaryPrimitives.WriteUInt16BigEndian(destination[(position + 3)..], record.RecordNumber);
				BinaryPrimitives.WriteUInt16BigEndian(destination[(position + 5)..], record.RecordLength);
				position += 7;
			}
		}

		/// <summary>
		/// 解析请求。
		/// </summary>
		/// <param name="data">数据域（不含功能码）。</param>
		/// <returns>解析出的请求。</returns>
		/// <exception cref="ModbusSlaveException">数据域非法。</exception>
		public static ReadFileRecordRequest Decode(ReadOnlySpan<byte> data)
		{
			if (data.Length < 8)
			{
				throw ModbusSlaveException.TruncatedPdu(ModbusFunctionCode.ReadFileRecord, 8, data.Length);
			}

			var byteCount = data[0];
			if (byteCount < 7 || byteCount % 7 != 0)
			{
				throw ModbusSlaveException.IllegalDataValue($"读文件记录请求的字节计数 {byteCount} 非法，应为 7 的正整数倍");
			}

			var referenceCount = byteCount / 7;
			if (referenceCount > ModbusConstants.MaxReadFileRecordSubRequests)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"读文件记录请求的子请求数 {referenceCount} 超出上限 {ModbusConstants.MaxReadFileRecordSubRequests}");
			}

			if (data.Length < 1 + byteCount)
			{
				throw ModbusSlaveException.TruncatedPdu(ModbusFunctionCode.ReadFileRecord, 1 + byteCount, data.Length);
			}

			if (data.Length != 1 + byteCount)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"读文件记录请求的数据域长度应为 {1 + byteCount}，实际为 {data.Length}");
			}

			var references = new FileRecordReference[referenceCount];
			for (var i = 0; i < referenceCount; i++)
			{
				var offset = 1 + i * 7;
				if (data[offset] != ModbusConstants.FileRecordReferenceType)
				{
					throw ModbusSlaveException.IllegalDataValue(
						$"读文件记录请求的第 {i} 个引用类型 0x{data[offset]:X2} 非法，应为 0x{ModbusConstants.FileRecordReferenceType:X2}");
				}

				references[i] = new FileRecordReference(
					BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 1)..]),
					BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 3)..]),
					BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 5)..]));
			}

			return new ReadFileRecordRequest(references);
		}
	}

	/// <summary>
	/// 读文件记录响应（FC14），数据域为「字节计数 + 若干子响应」。
	/// </summary>
	public sealed class ReadFileRecordResponse : ModbusResponse
	{
		private readonly ushort[][] records;
		private readonly int byteCount;

		/// <summary>
		/// 读文件记录响应。
		/// </summary>
		/// <param name="records">每条子响应包含的寄存器值，子响应不含文件号。</param>
		/// <exception cref="ModbusSlaveException">子响应集合为空或存在空的子响应。</exception>
		public ReadFileRecordResponse(ushort[][] records)
		{
			if (records is null || records.Length == 0)
			{
				throw ModbusSlaveException.IllegalDataValue("读文件记录响应至少需要 1 条子响应");
			}

			var byteCount = 0;
			for (var i = 0; i < records.Length; i++)
			{
				if (records[i] is null || records[i].Length < 1)
				{
					throw ModbusSlaveException.IllegalDataValue($"读文件记录响应的第 {i} 条子响应没有寄存器数据");
				}

				byteCount += 2 + records[i].Length * 2;
			}

			this.records = records;
			this.byteCount = byteCount;
		}

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.ReadFileRecord;

		/// <summary>
		/// 获取各子响应的寄存器数据。
		/// </summary>
		public ushort[][] Records => this.records;

		/// <summary>
		/// 获取数据域的字节数。
		/// </summary>
		public int ByteCount => this.byteCount;

		/// <inheritdoc/>
		public override int Length => 2 + this.byteCount;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			destination[0] = this.FunctionCode;
			destination[1] = (byte)this.byteCount;

			var position = 2;
			for (var i = 0; i < this.records.Length; i++)
			{
				var values = this.records[i];
				destination[position] = (byte)(values.Length * 2 + 1);
				destination[position + 1] = ModbusConstants.FileRecordReferenceType;
				for (var j = 0; j < values.Length; j++)
				{
					BinaryPrimitives.WriteUInt16BigEndian(destination[(position + 2 + j * 2)..], values[j]);
				}

				position += 2 + values.Length * 2;
			}
		}

		/// <summary>
		/// 解析响应。
		/// </summary>
		/// <param name="expectedFunctionCode">期望的响应功能码。</param>
		/// <param name="pdu">响应 PDU（含功能码）。</param>
		/// <returns>解析出的响应。</returns>
		/// <exception cref="ModbusSlaveException">功能码不匹配或数据域非法。</exception>
		public static ReadFileRecordResponse Decode(byte expectedFunctionCode, ReadOnlySpan<byte> pdu)
		{
			if (pdu.IsEmpty)
			{
				throw ModbusSlaveException.IllegalDataValue("读文件记录响应 PDU 为空");
			}

			if (pdu[0] != expectedFunctionCode)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"功能码不匹配：期望 0x{expectedFunctionCode:X2}，实际 0x{pdu[0]:X2}");
			}

			if (pdu.Length < 2)
			{
				throw ModbusSlaveException.TruncatedPdu(expectedFunctionCode, 2, pdu.Length);
			}

			var byteCount = pdu[1];
			if (byteCount < 2)
			{
				throw ModbusSlaveException.IllegalDataValue($"读文件记录响应的字节计数 {byteCount} 至少为 2");
			}

			if (pdu.Length < 2 + byteCount)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"读文件记录响应声明 {byteCount} 字节数据，但实际只有 {pdu.Length - 2} 字节");
			}

			var payload = pdu.Slice(2, byteCount);

			var recordCount = 0;
			var position = 0;
			while (position < byteCount)
			{
				var remaining = byteCount - position;
				if (remaining < 2)
				{
					throw ModbusSlaveException.IllegalDataValue(
						$"读文件记录响应的子响应剩余 {remaining} 字节，不足以构成子响应头");
				}

				var subLength = payload[position];
				if (subLength < 2 || (subLength & 1) == 0)
				{
					throw ModbusSlaveException.IllegalDataValue(
						$"读文件记录响应的子响应长度 {subLength} 非法，应为不小于 2 的奇数");
				}

				var total = 1 + subLength;
				if (remaining < total)
				{
					throw ModbusSlaveException.IllegalDataValue(
						$"读文件记录响应的子响应需要 {total} 字节，剩余 {remaining} 字节");
				}

				if (payload[position + 1] != ModbusConstants.FileRecordReferenceType)
				{
					throw ModbusSlaveException.IllegalDataValue(
						$"读文件记录响应的子响应引用类型 0x{payload[position + 1]:X2} 非法，应为 0x{ModbusConstants.FileRecordReferenceType:X2}");
				}

				if (((subLength - 1) & 1) != 0)
				{
					throw ModbusSlaveException.IllegalDataValue(
						$"读文件记录响应的子响应数据长度 {subLength - 1} 不是偶数");
				}

				recordCount++;
				position += total;
			}

			if (position != byteCount)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"读文件记录响应的子响应消耗 {position} 字节，与字节计数 {byteCount} 不符");
			}

			var records = new ushort[recordCount][];
			position = 0;
			for (var i = 0; i < recordCount; i++)
			{
				var subLength = payload[position];
				var valueCount = (subLength - 1) / 2;
				var values = new ushort[valueCount];
				for (var j = 0; j < valueCount; j++)
				{
					values[j] = BinaryPrimitives.ReadUInt16BigEndian(payload[(position + 2 + j * 2)..]);
				}

				records[i] = values;
				position += 1 + subLength;
			}

			return new ReadFileRecordResponse(records);
		}
	}

	/// <summary>
	/// 写文件记录请求（FC15），数据域为「字节计数 + 若干子记录」。
	/// </summary>
	public sealed class WriteFileRecordRequest : ModbusRequest
	{
		private readonly FileRecordWrite[] records;
		private readonly int byteCount;

		/// <summary>
		/// 写文件记录请求。
		/// </summary>
		/// <param name="records">子记录集合。</param>
		/// <exception cref="ModbusSlaveException">集合为空、子记录无值或 PDU 超长。</exception>
		public WriteFileRecordRequest(FileRecordWrite[] records)
		{
			FileRecordCodec.EnsureWriteRecords(records);
			this.records = records;
			this.byteCount = FileRecordCodec.ComputeWriteByteCount(records);
		}

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.WriteFileRecord;

		/// <summary>
		/// 获取子记录集合。
		/// </summary>
		public FileRecordWrite[] Records => this.records;

		/// <summary>
		/// 获取数据域的字节数。
		/// </summary>
		public int ByteCount => this.byteCount;

		/// <inheritdoc/>
		public override int Length => 2 + this.byteCount;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			FileRecordCodec.EncodeWrite(this.FunctionCode, this.records, destination);
		}

		/// <summary>
		/// 解析请求。
		/// </summary>
		/// <param name="data">数据域（不含功能码）。</param>
		/// <returns>解析出的请求。</returns>
		/// <exception cref="ModbusSlaveException">数据域非法。</exception>
		public static WriteFileRecordRequest Decode(ReadOnlySpan<byte> data)
		{
			return new WriteFileRecordRequest(FileRecordCodec.DecodeWriteData(ModbusFunctionCode.WriteFileRecord, data));
		}
	}

	/// <summary>
	/// 写文件记录响应（FC15），回显请求中的子记录。
	/// </summary>
	public sealed class WriteFileRecordResponse : ModbusResponse
	{
		private readonly FileRecordWrite[] records;
		private readonly int byteCount;

		/// <summary>
		/// 写文件记录响应。
		/// </summary>
		/// <param name="records">回显的子记录集合。</param>
		/// <exception cref="ModbusSlaveException">集合为空、子记录无值或 PDU 超长。</exception>
		public WriteFileRecordResponse(FileRecordWrite[] records)
		{
			FileRecordCodec.EnsureWriteRecords(records);
			this.records = records;
			this.byteCount = FileRecordCodec.ComputeWriteByteCount(records);
		}

		/// <inheritdoc/>
		public override byte FunctionCode => ModbusFunctionCode.WriteFileRecord;

		/// <summary>
		/// 获取回显的子记录集合。
		/// </summary>
		public FileRecordWrite[] Records => this.records;

		/// <summary>
		/// 获取数据域的字节数。
		/// </summary>
		public int ByteCount => this.byteCount;

		/// <inheritdoc/>
		public override int Length => 2 + this.byteCount;

		/// <inheritdoc/>
		public override void Encode(Span<byte> destination)
		{
			FileRecordCodec.EncodeWrite(this.FunctionCode, this.records, destination);
		}

		/// <summary>
		/// 解析响应。
		/// </summary>
		/// <param name="expectedFunctionCode">期望的响应功能码。</param>
		/// <param name="pdu">响应 PDU（含功能码）。</param>
		/// <returns>解析出的响应。</returns>
		/// <exception cref="ModbusSlaveException">功能码不匹配或数据域非法。</exception>
		public static WriteFileRecordResponse Decode(byte expectedFunctionCode, ReadOnlySpan<byte> pdu)
		{
			if (pdu.IsEmpty)
			{
				throw ModbusSlaveException.IllegalDataValue("写文件记录响应 PDU 为空");
			}

			if (pdu[0] != expectedFunctionCode)
			{
				throw ModbusSlaveException.IllegalDataValue(
					$"功能码不匹配：期望 0x{expectedFunctionCode:X2}，实际 0x{pdu[0]:X2}");
			}

			return new WriteFileRecordResponse(WriteFileRecordRequest.Decode(pdu[1..]).Records);
		}
	}
}
