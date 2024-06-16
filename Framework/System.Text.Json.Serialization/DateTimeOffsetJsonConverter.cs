
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace System.Text.Json.Serialization;

/// <summary>
/// DateTimeOffset 序列化对象
/// </summary>
public class DateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
	private readonly string _format = "yyyy-MM-dd HH:mm:ss";
	/// <summary>
	/// 默认使用 yyyy-MM-dd HH:mm:ss序列化
	/// </summary>
	public DateTimeOffsetJsonConverter()
	{

	}
	/// <summary>
	/// 使用 format 序列化时间
	/// </summary>
	/// <param name="format">格式化参数</param>

	public DateTimeOffsetJsonConverter(string format)
	{
		_format = format;
	}

	public override void Write(Utf8JsonWriter writer, DateTimeOffset date, JsonSerializerOptions options)
	{
		writer.WriteStringValue(date.ToString(_format));
	}

	public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = reader.GetString();

		if (string.IsNullOrEmpty(value))
			return DateTimeOffset.MinValue;

		if (DateTimeOffset.TryParse(value, out DateTimeOffset res))
			return res;

		if (DateTimeOffset.TryParse(value, null, DateTimeStyles.AssumeUniversal, out res))
			return res;

		if (DateTimeOffset.TryParse(value, null, DateTimeStyles.RoundtripKind, out res))
			return res;

		return DateTimeOffset.ParseExact(value, _format, null);
	}
}
