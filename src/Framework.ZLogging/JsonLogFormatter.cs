using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Framework.ZLogging;

/// <summary>
/// 把一条日志渲染为单行 JSON（JSON Lines）。
///
/// 与控制台 / 纯文本链路共享同一份 <see cref="SpectreLogEntry"/> 快照：
/// 消息正文仍由 <see cref="SpectreMessageFormatter"/> 产出，
/// 结构化属性则按「名称 / 值」原样序列化，便于日志系统按字段检索。
/// </summary>
static class JsonLogFormatter
{
    static readonly JsonWriterOptions WriterOptions = new()
    {
        // 日志内容可能含中文，不做 \uXXXX 转义，落盘后可读性更好。
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    static readonly JsonSerializerOptions ValueOptions = new()
    {
        WriteIndented = false,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        MaxDepth = 32
    };

    /// <summary>渲染为不含换行的 JSON 文本；调用方负责补行尾换行。</summary>
    public static string Format(SpectreLogEntry entry, SpectreMessageFormatter formatter)
    {
        var (message, _) = formatter.Format(entry);
        using var buffer = new MemoryStream(512);
        using (var writer = new Utf8JsonWriter(buffer, WriterOptions))
        {
            writer.WriteStartObject();
            // 统一 ISO 8601（带偏移量），跨时区检索时不会歧义。
            writer.WriteString("Timestamp", entry.Timestamp.ToString("O", CultureInfo.InvariantCulture));
            writer.WriteString("LogLevel", entry.LogLevel.ToString());
            writer.WriteString("Category", entry.Category);

            if (entry.EventId.Id != 0 || entry.EventId.Name is not null)
            {
                writer.WriteNumber("EventId", entry.EventId.Id);
                if (entry.EventId.Name is not null)
                    writer.WriteString("EventIdName", entry.EventId.Name);
            }

            if (entry.MessageTemplate is not null)
                writer.WriteString("MessageTemplate", entry.MessageTemplate);
            if (!string.IsNullOrEmpty(message))
                writer.WriteString("Message", message);

            if (entry.Properties.Length > 0)
            {
                writer.WriteStartObject("Properties");
                foreach (var property in entry.Properties)
                {
                    writer.WritePropertyName(property.Key);
                    WriteValue(writer, property.Value);
                }
                writer.WriteEndObject();
            }

            if (!string.IsNullOrEmpty(entry.Scopes))
                writer.WriteString("Scopes", entry.Scopes);

            if (entry.Exception is not null)
            {
                writer.WritePropertyName("Exception");
                WriteException(writer, entry.Exception);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }

    static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        try
        {
            // 按运行时类型序列化；先落到字符串再写入，避免中途失败把 writer 状态写坏。
            writer.WriteRawValue(JsonSerializer.Serialize(value, value.GetType(), ValueOptions));
        }
        catch (Exception)
        {
            // 不可序列化的类型（如带副作用的 getter）降级为 ToString，绝不让日志本身抛异常。
            writer.WriteStringValue(value.ToString());
        }
    }

    static void WriteException(Utf8JsonWriter writer, Exception exception)
    {
        writer.WriteStartObject();
        writer.WriteString("Type", exception.GetType().FullName);
        writer.WriteString("Message", exception.Message);
        if (exception.StackTrace is { } stack)
            writer.WriteString("StackTrace", stack);
        if (exception.InnerException is { } inner)
        {
            writer.WritePropertyName("InnerException");
            WriteException(writer, inner);
        }
        writer.WriteEndObject();
    }
}
