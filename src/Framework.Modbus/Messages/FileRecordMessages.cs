using Framework.Modbus.ProcessImage;
using Framework.Modbus.Util;

namespace Framework.Modbus.Messages;

/// <summary>文件记录子请求：文件号 / 起始记录号 / 记录长度。</summary>
public readonly record struct FileRecordReference(int FileNumber, int RecordNumber, int RecordLength);

/// <summary>FC14 读文件记录请求。对应 jamod 的 <c>ReadFileRecordRequest</c>。</summary>
public class ReadFileRecordRequest : ModbusRequest
{
    /// <summary>文件记录的引用类型，Modbus 规范固定为 0x06。</summary>
    public const byte RecordReferenceType = 0x06;

    /// <summary>以子请求集合构造请求。</summary>
    public ReadFileRecordRequest(IEnumerable<FileRecordReference> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        Records = records.ToArray();
        if (Records.Count == 0)
        {
            throw new ArgumentException("至少需要一个文件记录子请求。", nameof(records));
        }
    }

    /// <summary>子请求集合。</summary>
    public IReadOnlyList<FileRecordReference> Records { get; }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.ReadFileRecord;

    /// <inheritdoc />
    public override int ExpectedResponsePduLength => -1;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteByte(Records.Count * 7);
        foreach (var record in Records)
        {
            writer.WriteByte(RecordReferenceType);
            writer.WriteUInt16(record.FileNumber);
            writer.WriteUInt16(record.RecordNumber);
            writer.WriteUInt16(record.RecordLength);
        }

        return writer.ToArray();
    }

    /// <summary>从 PDU 解析请求。</summary>
    public static ReadFileRecordRequest FromPdu(byte[] pdu)
    {
        if (pdu.Length < 2)
        {
            throw new ModbusIOException("读文件记录请求 PDU 长度不足。");
        }

        int byteCount = pdu[1];
        var reader = new ModbusPduReader(pdu, 2, byteCount);
        var records = new List<FileRecordReference>();
        while (reader.Remaining >= 7)
        {
            reader.Skip(1);
            int fileNumber = reader.ReadUInt16();
            int recordNumber = reader.ReadUInt16();
            int recordLength = reader.ReadUInt16();
            records.Add(new FileRecordReference(fileNumber, recordNumber, recordLength));
        }

        return new ReadFileRecordRequest(records);
    }
}

/// <summary>FC14 读文件记录响应。</summary>
public sealed class ReadFileRecordResponse : ModbusResponse
{
    /// <summary>以文件记录集合构造响应。</summary>
    public ReadFileRecordResponse(IEnumerable<FileRecord> fileRecords)
    {
        ArgumentNullException.ThrowIfNull(fileRecords);
        FileRecords = fileRecords.ToArray();
    }

    /// <summary>读取到的文件记录。</summary>
    public FileRecord[] FileRecords { get; }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.ReadFileRecord;

    /// <inheritdoc />
    public override byte[] ToPdu()
    {
        var writer = new ModbusPduWriter();
        writer.WriteByte(FunctionCode);
        writer.WriteByte(FileRecords.Sum(fr => 2 + (fr.Count * 2)));
        foreach (var fileRecord in FileRecords)
        {
            writer.WriteByte((fileRecord.Count * 2) + 1);
            writer.WriteByte(ReadFileRecordRequest.RecordReferenceType);
            foreach (var record in fileRecord.Records)
            {
                writer.WriteUInt16(record.Value);
            }
        }

        return writer.ToArray();
    }

    /// <summary>从 PDU 解析响应。</summary>
    /// <remarks>响应中不携带起始记录号，因此解析出的记录号从 0 开始顺序编号。</remarks>
    public static ReadFileRecordResponse FromPdu(byte[] pdu)
    {
        if (pdu.Length < 2)
        {
            throw new ModbusIOException("读文件记录响应 PDU 长度不足。");
        }

        int byteCount = pdu[1];
        if (pdu.Length < 2 + byteCount)
        {
            throw new ModbusIOException($"读文件记录响应声明 {byteCount} 字节，但实际只有 {pdu.Length - 2} 字节。");
        }

        var reader = new ModbusPduReader(pdu, 2, byteCount);
        var fileRecords = new List<FileRecord>();
        while (reader.Remaining >= 2)
        {
            int subLength = reader.ReadByte();
            reader.Skip(1);
            int recordCount = (subLength - 1) / 2;
            var records = new Record[recordCount];
            for (int i = 0; i < recordCount; i++)
            {
                records[i] = new Record(i, reader.ReadUInt16());
            }

            fileRecords.Add(new FileRecord(fileRecords.Count, records));
        }

        return new ReadFileRecordResponse(fileRecords);
    }
}

/// <summary>FC15 写文件记录请求。对应 jamod 的 <c>WriteFileRecordRequest</c>。</summary>
public class WriteFileRecordRequest : ModbusRequest
{
    /// <summary>以文件记录集合构造请求。</summary>
    public WriteFileRecordRequest(IEnumerable<FileRecord> fileRecords)
    {
        ArgumentNullException.ThrowIfNull(fileRecords);
        FileRecords = fileRecords.ToArray();
        if (FileRecords.Length == 0)
        {
            throw new ArgumentException("至少需要一个文件记录。", nameof(fileRecords));
        }
    }

    /// <summary>待写入的文件记录。</summary>
    public FileRecord[] FileRecords { get; }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.WriteFileRecord;

    /// <inheritdoc />
    public override int ExpectedResponsePduLength => ToPdu().Length;

    /// <inheritdoc />
    public override byte[] ToPdu() => BuildPdu(FunctionCode, FileRecords);

    /// <summary>从 PDU 解析请求。</summary>
    public static WriteFileRecordRequest FromPdu(byte[] pdu) => new(ParseFileRecords(pdu, "写文件记录请求"));

    internal static byte[] BuildPdu(byte functionCode, FileRecord[] fileRecords)
    {
        var writer = new ModbusPduWriter();
        writer.WriteByte(functionCode);
        int byteCount = fileRecords.Sum(fr => 7 + (fr.Count * 2));
        writer.WriteByte(byteCount);
        foreach (var fileRecord in fileRecords)
        {
            writer.WriteByte(ReadFileRecordRequest.RecordReferenceType);
            writer.WriteUInt16(fileRecord.FileNumber);
            writer.WriteUInt16(fileRecord.StartReference);
            writer.WriteUInt16(fileRecord.Count);
            foreach (var record in fileRecord.Records)
            {
                writer.WriteUInt16(record.Value);
            }
        }

        return writer.ToArray();
    }

    internal static List<FileRecord> ParseFileRecords(byte[] pdu, string what)
    {
        if (pdu.Length < 2)
        {
            throw new ModbusIOException($"{what}的 PDU 长度不足。");
        }

        int byteCount = pdu[1];
        if (pdu.Length < 2 + byteCount)
        {
            throw new ModbusIOException($"{what}声明 {byteCount} 字节，但实际只有 {pdu.Length - 2} 字节。");
        }

        var reader = new ModbusPduReader(pdu, 2, byteCount);
        var fileRecords = new List<FileRecord>();
        while (reader.Remaining >= 7)
        {
            reader.Skip(1);
            int fileNumber = reader.ReadUInt16();
            int recordNumber = reader.ReadUInt16();
            int recordLength = reader.ReadUInt16();
            var records = new Record[recordLength];
            for (int i = 0; i < recordLength; i++)
            {
                records[i] = new Record(recordNumber + i, reader.ReadUInt16());
            }

            fileRecords.Add(new FileRecord(fileNumber, records));
        }

        return fileRecords;
    }
}

/// <summary>FC15 写文件记录响应（回显请求）。</summary>
public sealed class WriteFileRecordResponse : ModbusResponse
{
    /// <summary>以文件记录集合构造响应。</summary>
    public WriteFileRecordResponse(IEnumerable<FileRecord> fileRecords)
    {
        ArgumentNullException.ThrowIfNull(fileRecords);
        FileRecords = fileRecords.ToArray();
    }

    /// <summary>回显的文件记录。</summary>
    public FileRecord[] FileRecords { get; }

    /// <inheritdoc />
    public override byte FunctionCode => ModbusFunctionCode.WriteFileRecord;

    /// <inheritdoc />
    public override byte[] ToPdu() => WriteFileRecordRequest.BuildPdu(FunctionCode, FileRecords);

    /// <summary>从 PDU 解析响应。</summary>
    public static WriteFileRecordResponse FromPdu(byte[] pdu) =>
        new(WriteFileRecordRequest.ParseFileRecords(pdu, "写文件记录响应"));
}
