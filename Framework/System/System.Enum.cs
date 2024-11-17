using Framework.System;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Framework.System;

public static class EnumExtension
{
	public static string GetDescription(this Enum item)
	{
		string name = item.ToString();
		var desc = item.GetType().GetField(name)?.GetCustomAttribute<DescriptionAttribute>(false);
		return desc?.Description ?? name;
	}

	public static string ToNameWithDescription(this Enum item)
	{
		string name = item.ToString();
		var desc = item.GetType().GetField(name)?.GetCustomAttribute<DescriptionAttribute>(false);
		return $"{name}{(desc == null || desc.Description.IsNull() ? "" : $"({desc?.Description})")}";
	}

	public static long ToInt64(this Enum item)
	{
		return Convert.ToInt64(item);
	}

	public static List<Dictionary<string, object>>? ToList(this Enum value, bool ignoreNull = false)
	{
		var enumType = value.GetType();

		if (!enumType.IsEnum)
			return null;

		return Enum.GetValues(enumType).Cast<Enum>()
			.Where(m => !ignoreNull || !m.ToString().Equals("Null")).Select(x => new Dictionary<string, object>
			{
				["Label"] = x.GetDescription(),
				["Value"] = x
			}).ToList();
	}

	public static List<Dictionary<string, object>> ToList<T>(bool ignoreNull = false)
	{
		var enumType = typeof(T);

		if (!enumType.IsEnum)
			return null;

		return Enum.GetValues(enumType).Cast<Enum>()
			 .Where(m => !ignoreNull || !m.ToString().Equals("Null")).Select(x => new Dictionary<string, object>
			 {
				 ["Label"] = x.GetDescription(),
				 ["Value"] = x
			 }).ToList();
	}
}