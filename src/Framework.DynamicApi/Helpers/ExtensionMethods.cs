using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Framework.DynamicApi.Helpers;

/// <summary>
/// 动态 API 模块内部使用的字符串与集合扩展方法集合。
/// 主要服务于控制器名/Action 名的拼接、命名约定转换与前后缀剥离等场景。
/// </summary>
internal static class ExtensionMethods
{
	/// <summary>
	/// 判断字符串是否为 <c>null</c> 或空串。
	/// </summary>
	/// <param name="str">待判断的字符串。</param>
	/// <returns>为 <c>null</c> 或 <see cref="string.Empty"/> 时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
	public static bool IsNullOrEmpty(this string str)
	{
		return string.IsNullOrEmpty(str);
	}

	/// <summary>
	/// 判断集合是否为 <c>null</c> 或不包含任何元素。
	/// </summary>
	/// <typeparam name="T">集合元素类型。</typeparam>
	/// <param name="source">待判断的集合实例。</param>
	/// <returns>集合为 <c>null</c> 或元素数为 0 时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
	public static bool IsNullOrEmpty<T>(this ICollection<T> source)
	{
		return source == null || source.Count <= 0;
	}

	/// <summary>
	/// 判断字符串是否与给定候选集合中的任意一项相等。
	/// </summary>
	/// <param name="str">待匹配的字符串。</param>
	/// <param name="data">候选字符串集合。</param>
	/// <returns>存在相等项返回 <c>true</c>，否则返回 <c>false</c>。</returns>
	public static bool IsIn(this string str, params string[] data)
	{
		foreach (var item in data)
		{
			if (str == item)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// 从字符串尾部移除首个匹配的后缀（按参数顺序尝试一次）。
	/// 用于剥离控制器名或 Action 名中如 <c>AppService</c>、<c>Async</c> 等约定后缀。
	/// </summary>
	/// <param name="str">原始字符串。</param>
	/// <param name="postFixes">候选后缀列表，按顺序匹配。</param>
	/// <returns>移除首个匹配后缀的结果；若无后缀匹配、输入为空或候选列表为空则原样返回。</returns>
	public static string RemovePostFix(this string str, params string[] postFixes)
	{
		if (str == null)
		{
			return null;
		}

		if (str == string.Empty)
		{
			return string.Empty;
		}

		if (postFixes.IsNullOrEmpty())
		{
			return str;
		}

		foreach (var postFix in postFixes)
		{
			if (str.EndsWith(postFix))
			{
				return str.Left(str.Length - postFix.Length);
			}
		}

		return str;
	}

	/// <summary>
	/// 从字符串头部移除首个匹配的前缀（按参数顺序尝试一次）。
	/// 用于剥离命名空间前缀或动词前缀等。
	/// </summary>
	/// <param name="str">原始字符串。</param>
	/// <param name="preFixes">候选前缀列表，按顺序匹配。</param>
	/// <returns>移除首个匹配前缀的结果；若无前缀匹配、输入为空或候选列表为空则原样返回。</returns>
	public static string RemovePreFix(this string str, params string[] preFixes)
	{
		if (str == null)
		{
			return null;
		}

		if (str == string.Empty)
		{
			return string.Empty;
		}

		if (preFixes.IsNullOrEmpty())
		{
			return str;
		}

		foreach (var preFix in preFixes)
		{
			if (str.StartsWith(preFix))
			{
				return str.Right(str.Length - preFix.Length);
			}
		}

		return str;
	}


	/// <summary>
	/// 取字符串左侧指定长度的子串。
	/// </summary>
	/// <param name="str">原始字符串，不能为 <c>null</c>。</param>
	/// <param name="len">要截取的字符数。</param>
	/// <returns>从起始位置开始长度为 <paramref name="len"/> 的子串。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="str"/> 为 <c>null</c> 时抛出。</exception>
	/// <exception cref="ArgumentException">当 <paramref name="len"/> 大于 <paramref name="str"/> 的长度时抛出。</exception>
	public static string Left(this string str, int len)
	{
		if (str == null)
		{
			throw new ArgumentNullException("str");
		}

		if (str.Length < len)
		{
			throw new ArgumentException("len argument can not be bigger than given string's length!");
		}

		return str.Substring(0, len);
	}


	/// <summary>
	/// 取字符串右侧指定长度的子串。
	/// </summary>
	/// <param name="str">原始字符串，不能为 <c>null</c>。</param>
	/// <param name="len">要截取的字符数。</param>
	/// <returns>从字符串末尾向前数 <paramref name="len"/> 个字符的子串。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="str"/> 为 <c>null</c> 时抛出。</exception>
	/// <exception cref="ArgumentException">当 <paramref name="len"/> 大于 <paramref name="str"/> 的长度时抛出。</exception>
	public static string Right(this string str, int len)
	{
		if (str == null)
		{
			throw new ArgumentNullException("str");
		}

		if (str.Length < len)
		{
			throw new ArgumentException("len argument can not be bigger than given string's length!");
		}

		return str.Substring(str.Length - len, len);
	}

	/// <summary>
	/// 提取驼峰式（camelCase）命名的首个单词。
	/// 若输入非驼峰命名则按字符大小写边界拆分后取首段。
	/// </summary>
	/// <param name="str">原始字符串。</param>
	/// <returns>拆分后的第一个单词；若长度为 1 则原样返回。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="str"/> 为 <c>null</c> 时抛出。</exception>
	public static string GetCamelCaseFirstWord(this string str)
	{
		if (str == null)
		{
			throw new ArgumentNullException(nameof(str));
		}

		if (str.Length == 1)
		{
			return str;
		}

		var res = Regex.Split(str, @"(?=\p{Lu}\p{Ll})|(?<=\p{Ll})(?=\p{Lu})");

		if (res.Length < 1)
		{
			return str;
		}
		else
		{
			return res[0];
		}
	}

	/// <summary>
	/// 提取帕斯卡式（PascalCase）命名的第二个单词（即去除首个大写字母后的首段）。
	/// </summary>
	/// <param name="str">原始字符串。</param>
	/// <returns>拆分后的第二个单词；若不足两段则返回原字符串。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="str"/> 为 <c>null</c> 时抛出。</exception>
	public static string GetPascalCaseFirstWord(this string str)
	{
		if (str == null)
		{
			throw new ArgumentNullException(nameof(str));
		}

		if (str.Length == 1)
		{
			return str;
		}

		var res = Regex.Split(str, @"(?=\p{Lu}\p{Ll})|(?<=\p{Ll})(?=\p{Lu})");

		if (res.Length < 2)
		{
			return str;
		}
		else
		{
			return res[1];
		}
	}

	/// <summary>
	/// 自适应命名风格提取首个单词：根据首字符大小写自动调用 <see cref="GetPascalCaseFirstWord"/> 或 <see cref="GetCamelCaseFirstWord"/>。
	/// </summary>
	/// <param name="str">原始字符串。</param>
	/// <returns>按命名风格解析后的首个单词。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="str"/> 为 <c>null</c> 时抛出。</exception>
	public static string GetPascalOrCamelCaseFirstWord(this string str)
	{
		if (str == null)
		{
			throw new ArgumentNullException(nameof(str));
		}

		if (str.Length <= 1)
		{
			return str;
		}

		if (str[0] >= 65 && str[0] <= 90)
		{
			return str.GetPascalCaseFirstWord();
		}
		else
		{
			return str.GetCamelCaseFirstWord();
		}
	}

	/// <summary>
	/// 将字符串首字符转换为小写，其余字符保持不变。
	/// </summary>
	/// <param name="s">原始字符串。</param>
	/// <returns>首字符小写化后的字符串；若输入为空或 <c>null</c> 则原样返回。</returns>
	public static string FirstCharToLower(this string s)
	{
		if (string.IsNullOrEmpty(s))
			return s;

		string str = s.First().ToString().ToLower() + s.Substring(1);
		return str;
	}

	/// <summary>
	/// 将字符串首字符转换为大写，其余字符保持不变。
	/// </summary>
	/// <param name="s">原始字符串。</param>
	/// <returns>首字符大写化后的字符串；若输入为空或 <c>null</c> 则原样返回。</returns>
	public static string FirstCharToUpper(this string s)
	{
		if (string.IsNullOrEmpty(s))
			return s;

		string str = s.First().ToString().ToUpper() + s.Substring(1);
		return str;
	}
}