using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Framework;

public static partial class Helper
{
    public static string ComputeSha256Hash(string source)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant();

    public static string ComputeSha384Hash(string source)
        => Convert.ToHexString(SHA384.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant();

    public static string ComputeSha512Hash(string source)
        => Convert.ToHexString(SHA512.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant();
}