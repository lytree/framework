using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization;
/// <summary>
/// DateTime 序列化转换器 yyyy-MM-dd HH:mm:ss
/// </summary>
public class DateTimeJsonConverter : JsonConverter<DateTime>
{
	private readonly string _format = "yyyy-MM-dd HH:mm:ss";
	/// <summary>
	/// 默认使用 yyyy-MM-dd HH:mm:ss
	/// </summary>
	public DateTimeJsonConverter()
	{
	}
	/// <summary>
	/// 使用 format 进行格式化
	/// </summary>
	/// <param name="format">格式化参数</param>
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
