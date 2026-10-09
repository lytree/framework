using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Framework;

/// <summary>
/// 哈希算法相关的辅助方法集，依赖 <see cref="System.Security.Cryptography"/> 中的 SHA-2 系列实现
/// （<see cref="SHA256"/> / <see cref="SHA384"/> / <see cref="SHA512"/>）以及 <see cref="Encoding.UTF8"/> 编码，
/// 提供字符串到哈希摘要字符串的便捷计算能力，结果以小写十六进制形式输出。
/// </summary>
public static partial class Helper
{
    /// <summary>
    /// 计算 <paramref name="source"/> 的 UTF-8 字节流的 SHA-256 摘要，返回小写十六进制字符串（64 个十六进制字符 / 256 位）。
    /// </summary>
    /// <param name="source">待哈希的源字符串。内部按 UTF-8 编码为字节序列后参与计算。</param>
    /// <returns>长度为 64 的小写十六进制字符串形式的 SHA-256 摘要。</returns>
    public static string ComputeSha256Hash(string source)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant();

    /// <summary>
    /// 计算 <paramref name="source"/> 的 UTF-8 字节流的 SHA-384 摘要，返回小写十六进制字符串（96 个十六进制字符 / 384 位）。
    /// 抗碰撞强度高于 SHA-256，适合对安全等级要求更高的摘要场景。
    /// </summary>
    /// <param name="source">待哈希的源字符串。内部按 UTF-8 编码为字节序列后参与计算。</param>
    /// <returns>长度为 96 的小写十六进制字符串形式的 SHA-384 摘要。</returns>
    public static string ComputeSha384Hash(string source)
        => Convert.ToHexString(SHA384.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant();

    /// <summary>
    /// 计算 <paramref name="source"/> 的 UTF-8 字节流的 SHA-512 摘要，返回小写十六进制字符串（128 个十六进制字符 / 512 位）。
    /// 提供 SHA-2 系列中最强的抗碰撞强度与摘要长度。
    /// </summary>
    /// <param name="source">待哈希的源字符串。内部按 UTF-8 编码为字节序列后参与计算。</param>
    /// <returns>长度为 128 的小写十六进制字符串形式的 SHA-512 摘要。</returns>
    public static string ComputeSha512Hash(string source)
        => Convert.ToHexString(SHA512.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant();
}