using Framework.Modbus.ProcessImage;
using Abstractions = Framework.Modbus.Abstractions;

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
        var references = new Abstractions.FileRecordReference[Records.Count];
        for (int i = 0; i < references.Length; i++)
        {
            var record = Records[i];
            references[i] = new Abstractions.FileRecordReference(
                (ushort)record.FileNumber,
                (ushort)record.RecordNumber,
                (ushort)record.RecordLength);
        }

        using var writer = new ModbusPduWriter();
        PduBridge.Write(writer, new Abstractions.ReadFileRecordRequest(references));
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析请求。</summary>
    public static ReadFileRecordRequest FromPdu(byte[] pdu)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        var decoded = PduBridge.DecodeAs(
            () => Abstractions.ReadFileRecordRequest.Decode(pdu.AsSpan(1)),
            "读文件记录请求");

        var references = decoded.Records;
        var records = new FileRecordReference[references.Length];
        for (int i = 0; i < records.Length; i++)
        {
            var record = references[i];
            records[i] = new FileRecordReference(record.FileNumber, record.RecordNumber, record.RecordLength);
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
        var records = new ushort[FileRecords.Length][];
        for (int i = 0; i < records.Length; i++)
        {
            var fileRecord = FileRecords[i];
            var values = new ushort[fileRecord.Count];
            for (int j = 0; j < values.Length; j++)
            {
                values[j] = (ushort)fileRecord[j].Value;
            }

            records[i] = values;
        }

        using var writer = new ModbusPduWriter();
        PduBridge.Write(writer, new Abstractions.ReadFileRecordResponse(records));
        return writer.ToArray();
    }

    /// <summary>从 PDU 解析响应。</summary>
    /// <remarks>响应中不携带起始记录号，因此解析出的记录号从 0 开始顺序编号。</remarks>
    public static ReadFileRecordResponse FromPdu(byte[] pdu)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        var decoded = PduBridge.DecodeAs(
            () => Abstractions.ReadFileRecordResponse.Decode(ModbusFunctionCode.ReadFileRecord, pdu),
            "读文件记录响应");

        var records = decoded.Records;
        var fileRecords = new FileRecord[records.Length];
        for (int i = 0; i < fileRecords.Length; i++)
        {
            var values = records[i];
            var items = new Record[values.Length];
            for (int j = 0; j < items.Length; j++)
            {
                items[j] = new Record(j, values[j]);
            }

            fileRecords[i] = new FileRecord(i, items);
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
        var records = new Abstractions.FileRecordWrite[fileRecords.Length];
        for (int i = 0; i < records.Length; i++)
        {
            var fileRecord = fileRecords[i];
            var values = new ushort[fileRecord.Count];
            for (int j = 0; j < values.Length; j++)
            {
                values[j] = (ushort)fileRecord[j].Value;
            }

            records[i] = new Abstractions.FileRecordWrite(
                (ushort)fileRecord.FileNumber,
                (ushort)fileRecord.StartReference,
                values);
        }

        var pdu = new Abstractions.WriteFileRecordRequest(records).ToByteArray();
        pdu[0] = functionCode;
        return pdu;
    }

    internal static List<FileRecord> ParseFileRecords(byte[] pdu, string what)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        var decoded = PduBridge.DecodeAs(
            () => Abstractions.WriteFileRecordRequest.Decode(pdu.AsSpan(1)),
            what);

        var fileRecords = new List<FileRecord>(decoded.Records.Length);
        foreach (var record in decoded.Records)
        {
            var values = record.Values;
            var items = new Record[values.Length];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = new Record(record.StartRecord + i, values[i]);
            }

            fileRecords.Add(new FileRecord(record.FileNumber, items));
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
