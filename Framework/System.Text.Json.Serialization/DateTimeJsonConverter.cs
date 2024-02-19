using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace Framework.System.Text.Json.Serialization;

public class DateTimeJsonConverter : JsonConverter<DateTime>
{
	private readonly string _format = "yyyy-MM-dd HH:mm:ss";

	public DateTimeJsonConverter()
	{
	}
	public DateTimeJsonConverter(string format)
	{
		_format = format;
	}
	public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return DateTime.Parse(s: reader.GetString());
	}

	public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString(_format));
	}
}
