using Framework.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Framework;

/// <summary>
/// 加密工具类
/// </summary>
public static partial class Helper
{
    private const string Key = "desenc1!";

    /// <summary>
    /// DES+Base64加密
    /// <para>采用ECB、PKCS7</para>
    /// </summary>
    /// <param name="encryptString">加密字符串</param>
    /// <param name="key">秘钥</param>
    /// <returns></returns>
    [Obsolete("DES 密钥长度仅 56 bit，已被 NIST 废止。请改用 AES（Helper.AESEncrypt）或 SM4（Helper.SM4Encrypt）。")]
    public static string DESEncrypt(string encryptString, string key = "")
    {
        return DESEncrypt(encryptString, key, false, true);
    }

    /// <summary>
    /// DES+Base64解密
    /// <para>采用ECB、PKCS7</para>
    /// </summary>
    /// <param name="decryptString">解密字符串</param>
    /// <param name="key">秘钥</param>
    /// <returns></returns>
    [Obsolete("DES 密钥长度仅 56 bit，已被 NIST 废止。请改用 AES（Helper.AESDecrypt）或 SM4（Helper.SM4Decrypt）。")]
    public static string? DESDecrypt(string decryptString, string key = "")
    {
        return DESDecrypt(decryptString, key, false);
    }

    /// <summary>
    /// DES+16进制加密
    /// <para>采用ECB、PKCS7</para>
    /// </summary>
    /// <param name="encryptString">加密字符串</param>
    /// <param name="key">秘钥</param>
    /// <param name="lowerCase">是否小写</param>
    /// <returns></returns>
    [Obsolete("DES 密钥长度仅 56 bit，已被 NIST 废止。请改用 AES 或 SM4。")]
    public static string DESEncrypt4Hex(string encryptString, string key = "", bool lowerCase = false)
    {
        return DESEncrypt(encryptString, key, true, lowerCase);
    }

    /// <summary>
    /// DES+16进制解密
    /// <para>采用ECB、PKCS7</para>
    /// </summary>
    /// <param name="decryptString">解密字符串</param>
    /// <param name="key">秘钥</param>
    /// <returns></returns>
    [Obsolete("DES 密钥长度仅 56 bit，已被 NIST 废止。请改用 AES 或 SM4。")]
    public static string? DESDecrypt4Hex(string decryptString, string key = "")
    {
        return DESDecrypt(decryptString, key, true);
    }

    private static string DESEncrypt(string encryptString, string key, bool hex, bool lowerCase = false)
    {
        if (encryptString.IsNull())
        {
            return string.Empty;
        }
        if (key.IsNull())
        {
            key = Key;
        }
        if (key.Length < 8)
        {
            throw new ArgumentException("秘钥长度为8位", nameof(key));
        }
        var keyBytes = Encoding.UTF8.GetBytes(key[..8]);
        var inputByteArray = Encoding.UTF8.GetBytes(encryptString);

        using var des = DES.Create();
        des.Mode = CipherMode.ECB;
        des.Key = keyBytes;
        des.Padding = PaddingMode.PKCS7;

        using var stream = new MemoryStream();
        using var cStream = new CryptoStream(stream, des.CreateEncryptor(), CryptoStreamMode.Write);
        cStream.Write(inputByteArray, 0, inputByteArray.Length);
        cStream.FlushFinalBlock();

        var bytes = stream.ToArray();
        return hex ? Helper.ToHex(bytes, lowerCase) : Helper.ToBase64(bytes);
    }

    private static string? DESDecrypt(string decryptString, string key, bool hex)
    {
        if (decryptString.IsNull())
        {
            return null;
        }
        if (key.IsNull())
        {
            key = Key;
        }
        if (key.Length < 8)
        {
            throw new ArgumentException("秘钥长度为8位", nameof(key));
        }
        var keyBytes = Encoding.UTF8.GetBytes(key[..8]);
        var inputByteArray = hex ? Helper.HexToBytes(decryptString) : Convert.FromBase64String(decryptString);

        using var des = DES.Create();
        des.Mode = CipherMode.ECB;
        des.Key = keyBytes;
        des.Padding = PaddingMode.PKCS7;

        using var mStream = new MemoryStream();
        using var cStream = new CryptoStream(mStream, des.CreateDecryptor(), CryptoStreamMode.Write);
        cStream.Write(inputByteArray, 0, inputByteArray.Length);
        cStream.FlushFinalBlock();
        return Encoding.UTF8.GetString(mStream.ToArray());
    }
}
