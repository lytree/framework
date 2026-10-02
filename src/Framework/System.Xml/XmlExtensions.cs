using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace System.Xml;

/// <summary>
/// XML 操作扩展类
/// </summary>
public static partial class Extensions
{
	public static T GetAs<T>(this XAttribute attr, T defaultValue = default(T))
	{
		T ret = defaultValue;

		if (typeof(string) == typeof(T))
		{
			return (T)(object)attr?.Value;
		}

		if (!string.IsNullOrEmpty(attr?.Value))
		{
			// Cast to Return Data Type 
			// ChangeType does NOT work with Nullable Types
			ret = (T)Convert.ChangeType(attr.Value, typeof(T));
		}

		return ret;
	}

	public static T GetAs<T>(this XElement elem, T defaultValue = default(T))
	{
		T ret = defaultValue;

		if (typeof(string) == typeof(T))
		{
			return (T)(object)elem?.Value;
		}


		if (!string.IsNullOrEmpty(elem?.Value))
		{
			// Cast to Return Data Type 
			// ChangeType does NOT work with Nullable Types
			ret = (T)Convert.ChangeType(elem.Value, typeof(T));
		}

		return ret;
	}

	public static string SerializeToXml<T>(this T source)
	{
		if (source == null) return string.Empty;

		using var stringWriter = new StringWriter();
		using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true }))
		{
			XmlSerializerCache<T>.Instance.Serialize(xmlWriter, source);
			return stringWriter.ToString();
		}
	}

	public static T DeserializeTo<T>(this string xmlSource)
	{
		T deserialized = default(T);

		using (StringReader reader = new StringReader(xmlSource))
		{
			deserialized = (T)XmlSerializerCache<T>.Instance.Deserialize(reader);
		}

		return deserialized;
	}

	private static class XmlSerializerCache<T>
	{
		public static readonly XmlSerializer Instance = new(typeof(T));
	}
}