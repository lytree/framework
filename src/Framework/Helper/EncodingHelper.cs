using Framework.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Framework.Helper;

public static class EncodingHelper
{
    /// <summary>
    /// 转换为16进制
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="lowerCase">是否小写</param>
    /// <returns></returns>
    public static string ToHex(byte[] bytes, bool lowerCase = true)
    {
        if (bytes == null)
            return string.Empty;

        var result = new StringBuilder();
        var format = lowerCase ? "x2" : "X2";
        for (var i = 0; i < bytes.Length; i++)
        {
            result.Append(bytes[i].ToString(format));
        }

        return result.ToString();
    }

    /// <summary>
    /// 16进制转字节数组
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static byte[] HexToBytes(string s)
    {
        if (s.IsNull())
            return Array.Empty<byte>();
        var bytes = new byte[s.Length / 2];

        for (int x = 0; x < s.Length / 2; x++)
        {
            int i = Convert.ToInt32(s.Substring(x * 2, 2), 16);
            bytes[x] = (byte)i;
        }

        return bytes;
    }

    /// <summary>
    /// 转换为Base64
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static string ToBase64(byte[] bytes)
    {
        if (bytes == null)
            return string.Empty;

        return Convert.ToBase64String(bytes);
    }
    /// <summary>
    /// 字符串转Unicode码
    /// </summary>
    /// <returns>The to unicode.</returns>
    /// <param name="value">Value.</param>
    public static string StringToUnicode(string value)
    {
        byte[] bytes = Encoding.Unicode.GetBytes(value);
        var stringBuilder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i += 2)
        {
            // 取两个字符，每个字符都是右对齐。
            stringBuilder.AppendFormat("u{0}{1}", bytes[i + 1].ToString("x").PadLeft(2, '0'), bytes[i].ToString("x").PadLeft(2, '0'));
        }
        return stringBuilder.ToString();
    }

    /// <summary>
    /// Unicode转字符串
    /// </summary>
    /// <returns>The to string.</returns>
    /// <param name="unicode">Unicode.</param>
    public static string UnicodeToString(string unicode)
    {
        unicode = unicode.Replace("%", "\\");

        return new Regex(@"\\u([0-9A-F]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled).Replace(
             unicode, x => string.Empty + Convert.ToChar(Convert.ToUInt16(x.Result("$1"), 16)));
    }
}