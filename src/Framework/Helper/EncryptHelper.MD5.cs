using System;
using System.Security.Cryptography;
using System.Text;

namespace Framework;
public static partial class Helper
{
    /// <summary>
    /// 获取字符串的MD5加密字符串。
    /// </summary>
    /// <param name="value">原字符串。</param>
    /// <param name="hex">是否返回16进制字符串。</param>
    /// <param name="lowerCase">是否返回小写字符串。</param>
    /// <returns>MD5加密字符串。</returns>
    public static string Md5Encrypt(string value, bool hex, bool lowerCase = false)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var buffer = Encoding.UTF8.GetBytes(value);
        var bytes = MD5.HashData(buffer);

        return hex ? Helper.ToHex(bytes, lowerCase) : Helper.ToBase64(bytes);
    }
}
