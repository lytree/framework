using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace System;


public static partial class Extensions
{
    /// <summary>
    /// 判断字符串是否为Null、空
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static bool IsNull(this string str)
    {
        return string.IsNullOrWhiteSpace(str);
    }

    /// <summary>
    /// 判断字符串是否不为Null、空
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static bool NotNull(this string str)
    {
        return !string.IsNullOrWhiteSpace(str);
    }

    /// <summary>
    /// 与字符串进行比较，忽略大小写
    /// </summary>
    /// <param name="str"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool EqualsIgnoreCase(this string str, string value)
    {
        return str.Equals(value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 首字母转小写
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string FirstCharToLower(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;
        return string.Concat(str.First().ToString().ToLower(), str.AsSpan(1)); ;
    }

    /// <summary>
    /// 首字母转大写
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string FirstCharToUpper(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        return string.Concat(str.First().ToString().ToUpper(), str.AsSpan(1));
    }

    /// <summary>
    /// 转为Base64，UTF-8格式
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ToBase64(this string str)
    {
        return str.ToBase64(Encoding.UTF8);
    }

    /// <summary>
    /// 转为Base64
    /// </summary>
    /// <param name="str"></param>
    /// <param name="encoding">编码</param>
    /// <returns></returns>
    public static string ToBase64(this string str, Encoding encoding)
    {
        if (str.IsNull())
            return string.Empty;

        var bytes = encoding.GetBytes(str);
        return Helper.ToBase64(bytes);
    }
    public static string ToPath(this string s)
    {
        if (s.IsNull())
            return string.Empty;

        return s.Replace(@"\", "/");
    }
    public static string Limit(this string str, int length)
    {
        if (str.Length > length)
        {
            return str[..length];
        }
        return str;
    }

    public static string LimitWithEllipsis(this string str, int length)
    {
        if (str.Length > length)
        {
            return str[..length] + "...";
        }
        return str;
    }
    /// <summary>
    /// 字符串转指定类型数组
    /// </summary>
    /// <param name="str"></param>
    /// <param name="split"></param>
    /// <returns></returns>
    public static T[] SplitToArray<T>(string str, char split)
    {
        if (string.IsNullOrEmpty(str))
            return Array.Empty<T>();
        T[] arr = [.. str.Split(new string[] { split.ToString() }, StringSplitOptions.RemoveEmptyEntries).Cast<T>()];
        return arr;
    }
}