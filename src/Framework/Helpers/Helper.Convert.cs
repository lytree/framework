using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework;

/// <summary>
/// 编码与进制转换相关的辅助方法集，依赖 <see cref="Encoding"/> 等标准库设施，
/// 当前实现专注于 RFC 4648 Base32 字符串与字节数组的双向转换，可在不需要 URL 安全 / 文件名安全编码的场景中替代 Base64 使用。
/// </summary>
public static partial class Helper
{
	// Base32 字符集，共 32 个字符

	private static readonly char[] BASE32_CHARS =
		"ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

	// Base32 填充字符
	private const char BASE32_PADDING_CHAR = '=';

	// Base62 字符集，共 62 个字符

	private static readonly char[] BASE62_CHARS =
		"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();

	/// <summary>
	/// 将字节数组按 RFC 4648 Base32 编码为字符串。
	/// 输出使用大写字母与数字 <c>2-7</c>，长度非 5 的整数倍时按规范使用 <c>'='</c> 填充。
	/// </summary>
	/// <param name="bytes">待编码的字节数组。为 <c>null</c> 时抛出异常；为空数组时返回空字符串。</param>
	/// <returns>Base32 编码字符串。空输入返回 <see cref="string.Empty"/>。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="bytes"/> 为 <c>null</c>。</exception>
	public static string ToBase32String(byte[] bytes)
	{
        ArgumentNullException.ThrowIfNull(bytes);

        int length = bytes.Length;
		if (length == 0)
		{
			return string.Empty;
		}

		// 每 5 字节编码为 8 个 Base32 字符：5 * 8 = 40 位 = 8 * 5 位
		char[] chars = new char[(length + 4) / 5 * 8];
		int index = 0;
		for (int i = 0; i < length; i += 5)
		{
			// 把 5 字节加载到 int 高位，低位不足处用 0 补齐
			int val = (bytes[i] << 24) + ((i + 1 < length ? bytes[i + 1] : 0) << 16) +
					  ((i + 2 < length ? bytes[i + 2] : 0) << 8) + ((i + 3 < length ? bytes[i + 3] : 0) << 0);
			// 每次取 5 位，映射到 Base32 字符表
			chars[index++] = BASE32_CHARS[(val >> 35) & 0x1F];
			chars[index++] = BASE32_CHARS[(val >> 30) & 0x1F];
			chars[index++] = BASE32_CHARS[(val >> 25) & 0x1F];
			chars[index++] = BASE32_CHARS[(val >> 20) & 0x1F];
			chars[index++] = BASE32_CHARS[(val >> 15) & 0x1F];
			chars[index++] = BASE32_CHARS[(val >> 10) & 0x1F];
			chars[index++] = BASE32_CHARS[(val >> 5) & 0x1F];
			chars[index++] = BASE32_CHARS[val & 0x1F];
		}

		// 添加填充字符：仅最后一段不足 5 字节时需要补 '='
		int paddingCount = length % 5;
		if (paddingCount > 0)
		{
			chars[^1] = BASE32_PADDING_CHAR;
			if (paddingCount == 1)
			{
				chars[^2] = BASE32_PADDING_CHAR;
			}
			if (paddingCount <= 2)
			{
				chars[^3] = BASE32_PADDING_CHAR;
			}
			if (paddingCount <= 3)
			{
				chars[^4] = BASE32_PADDING_CHAR;
			}
			if (paddingCount <= 4)
			{
				chars[^5] = BASE32_PADDING_CHAR;
			}
		}

		return new string(chars);
	}

	/// <summary>
	/// 解码符合 RFC 4648 Base32 规范的字符串为字节数组。
	/// 接受大写字母 <c>A-Z</c> 与数字 <c>2-7</c>，并允许末尾使用 <c>'='</c> 作为填充。
	/// </summary>
	/// <param name="str">Base32 编码字符串。长度必须为 8 的整数倍（含填充），否则抛出异常。</param>
	/// <returns>解码后的字节数组。</returns>
	/// <exception cref="ArgumentException">
	/// 当 <paramref name="str"/> 为 <c>null</c>、空串、长度不是 8 的整数倍，或包含 <c>A-Z / 2-7</c> 之外字符时抛出。
	/// </exception>
	public static byte[] FromBase32String(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			throw new ArgumentException("String is null or empty.", nameof(str));
		}

		int length = str.Length;
		if (length % 8 != 0)
		{
			throw new ArgumentException("Invalid length of input string: " + length, nameof(str));
		}

		// 统计末尾的填充字符数量，注意第 5 个位置不会是 '='，所以从第 4 个回退
		int paddingCount = 0;
		if (length > 0 && str[length - 1] == BASE32_PADDING_CHAR)
		{
			paddingCount++;
		}
		if (length > 1 && str[length - 2] == BASE32_PADDING_CHAR)
		{
			paddingCount++;
		}
		if (length > 3 && str[length - 3] == BASE32_PADDING_CHAR)
		{
			paddingCount++;
		}
		if (length > 4 && str[length - 4] == BASE32_PADDING_CHAR)
		{
			paddingCount++;
		}
		if (length > 6 && str[length - 6] == BASE32_PADDING_CHAR)
		{
			paddingCount++;
		}

		// 输出字节数 = 解码区段 * 5 - 填充数
		byte[] bytes = new byte[length / 8 * 5 - paddingCount];
		int index = 0;
		for (int i = 0; i < length; i += 8)
		{
			// 把 8 个 5 位字符拼成 40 位整数
			int val = (DecodeBase32Char(str[i]) << 35) +
					  (DecodeBase32Char(str[i + 1]) << 30) +
					  (DecodeBase32Char(str[i + 2]) << 25) +
					  (DecodeBase32Char(str[i + 3]) << 20) +
					  (DecodeBase32Char(str[i + 4]) << 15) +
					  (DecodeBase32Char(str[i + 5]) << 10) +
					  (DecodeBase32Char(str[i + 6]) << 5) +
					  DecodeBase32Char(str[i + 7]);
			// 按高 8 位顺序输出，避免越界处写入
			bytes[index++] = (byte)(val >> 24);
			if (index < bytes.Length)
			{
				bytes[index++] = (byte)(val >> 16);
			}
			if (index < bytes.Length)
			{
				bytes[index++] = (byte)(val >> 8);
			}
			if (index < bytes.Length)
			{
				bytes[index++] = (byte)val;
			}
		}

		return bytes;
	}


	// 解码 Base32 字符
	private static int DecodeBase32Char(char c)
	{
		if (c >= 'A' && c <= 'Z')
		{
			return c - 'A';
		}
		if (c >= '2' && c <= '7')
		{
			return c - '2' + 26;
		}
		throw new ArgumentException("Invalid character in input string: " + c, nameof(c));
	}
}
