using Framework.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Framework;

public static partial class Helper
{
    /// <summary>
    /// 16位MD5加密
    /// </summary>
    /// <param name="password"></param>
    /// <param name="lowerCase"></param>
    /// <returns></returns>
    public static string MD5Encrypt16(string password, bool lowerCase = false)
    {
        if (password.IsNull())
            return string.Empty;
        return Helper.ToHex(MD5.HashData(Encoding.UTF8.GetBytes(password)), lowerCase);
    }

    /// <summary>
    /// 32位MD5加密
    /// </summary>
    /// <param name="password"></param>
    /// <param name="lowerCase"></param>
    /// <returns></returns>
    public static string MD5Encrypt32(string password = "", bool lowerCase = false)
    {
        if (password.IsNull())
            return string.Empty;
        string pwd = string.Empty;
        byte[] s = MD5.HashData(Encoding.UTF8.GetBytes(password));
        var format = lowerCase ? "x2" : "X2";
        foreach (var item in s)
        {
            pwd = string.Concat(pwd, item.ToString(format));
        }
        return pwd;
    }

    /// <summary>
    /// 64位MD5加密
    /// </summary>
    /// <param name="password"></param>
    /// <returns></returns>
    public static string MD5Encrypt64(string password)
    {
        if (password.IsNull())
            return string.Empty;
        byte[] s = MD5.HashData(Encoding.UTF8.GetBytes(password));
        return ToBase64(s);
    }

    public static string GetHash(Stream stream)
    {
        StringBuilder sb = new();
        using var md5 = MD5.Create();
        byte[] hashBytes = md5.ComputeHash(stream);
        foreach (byte bt in hashBytes)
        {
            sb.Append(bt.ToString("x2"));
        }

        return sb.ToString();
    }
}
