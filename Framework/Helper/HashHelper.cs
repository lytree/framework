using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Helper;

public static class HashHelper
{
    private static string ComputeHash(HashAlgorithm hashAlgorithm, string source)
    {
        byte[] array = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(source));
        StringBuilder stringBuilder = new();
        foreach (byte b in array)
        {
            stringBuilder.Append(b.ToString("x2"));
        }
        return stringBuilder.ToString();
    }

    public static string ComputeSha256Hash(string source)
    {
        string text;
        using (SHA256 sha = SHA256.Create())
        {
            text = ComputeHash(sha, source);
        }
        return text;
    }

    public static string ComputeSha384Hash(string source)
    {
        string text;
        using (SHA384 sha = SHA384.Create())
        {
            text = ComputeHash(sha, source);
        }
        return text;
    }

    public static string ComputeSha512Hash(string source)
    {
        string text;
        using (SHA512 sha = SHA512.Create())
        {
            text = ComputeHash(sha, source);
        }
        return text;
    }
}