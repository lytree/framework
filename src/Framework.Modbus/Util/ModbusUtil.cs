using System.Globalization;
using System.Text;

namespace Framework.Modbus.Util;

/// <summary>
/// Modbus 编解码工具：CRC16 / LRC 校验、十六进制转换、寄存器与字节序转换。
/// 对应 jamod 的 <c>net.wimpi.modbus.util.ModbusUtil</c>。
/// </summary>
public static class ModbusUtil
{
    /// <summary>Modbus CRC16 多项式（反转形式）。</summary>
    private const int Crc16Polynomial = 0xA001;

    /// <summary>CRC16 初值。</summary>
    private const int Crc16Preset = 0xFFFF;

    /// <summary>把 16 位整数按大端（高字节在前）转成 2 字节。</summary>
    public static byte[] ToBytes(int value)
    {
        return new[] { (byte)((value >> 8) & 0xFF), (byte)(value & 0xFF) };
    }

    /// <summary>把 32 位整数按大端转成 4 字节。</summary>
    public static byte[] ToBytes(int value, bool asInt32) => asInt32
        ? new[]
        {
            (byte)((value >> 24) & 0xFF),
            (byte)((value >> 16) & 0xFF),
            (byte)((value >> 8) & 0xFF),
            (byte)(value & 0xFF),
        }
        : ToBytes(value);

    /// <summary>把线圈状态转成 FC05 使用的 2 字节：ON = 0xFF00，OFF = 0x0000。</summary>
    public static byte[] CoilToBytes(bool value) => value
        ? new byte[] { 0xFF, 0x00 }
        : new byte[] { 0x00, 0x00 };

    /// <summary>从字节数组的大端 2 字节读取线圈状态。</summary>
    public static bool BytesToCoil(byte[] bytes, int offset)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        return bytes[offset] == 0xFF;
    }

    /// <summary>从字节数组按大端读取 16 位无符号整数。</summary>
    public static int BytesToUInt16(byte[] bytes, int offset)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        return ((bytes[offset] & 0xFF) << 8) | (bytes[offset + 1] & 0xFF);
    }

    /// <summary>从字节数组按大端读取 16 位有符号整数。</summary>
    public static short BytesToInt16(byte[] bytes, int offset) => (short)BytesToUInt16(bytes, offset);

    /// <summary>从字节数组按大端读取 32 位整数。</summary>
    public static int BytesToInt32(byte[] bytes, int offset)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        return ((bytes[offset] & 0xFF) << 24)
            | ((bytes[offset + 1] & 0xFF) << 16)
            | ((bytes[offset + 2] & 0xFF) << 8)
            | (bytes[offset + 3] & 0xFF);
    }

    /// <summary>寄存器数组 → 字节数组（每寄存器 2 字节，大端）。</summary>
    public static byte[] RegistersToBytes(IEnumerable<int> registers)
    {
        ArgumentNullException.ThrowIfNull(registers);
        var list = registers as IList<int> ?? registers.ToList();
        var bytes = new byte[list.Count * 2];
        for (int i = 0; i < list.Count; i++)
        {
            bytes[i * 2] = (byte)((list[i] >> 8) & 0xFF);
            bytes[(i * 2) + 1] = (byte)(list[i] & 0xFF);
        }

        return bytes;
    }

    /// <summary>字节数组 → 寄存器数组（每 2 字节一个大端 16 位值）。</summary>
    public static int[] BytesToRegisters(byte[] bytes, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var registers = new int[count];
        for (int i = 0; i < count; i++)
        {
            registers[i] = BytesToUInt16(bytes, offset + (i * 2));
        }

        return registers;
    }

    /// <summary>计算 Modbus CRC16（初值 0xFFFF，多项式 0xA001）。</summary>
    /// <param name="data">数据缓冲区。</param>
    /// <param name="offset">起始偏移。</param>
    /// <param name="count">参与计算的字节数。</param>
    public static int CalculateCrc16(byte[] data, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (offset < 0 || count < 0 || offset + count > data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "偏移或长度超出缓冲区范围。");
        }

        int crc = Crc16Preset;
        for (int i = offset; i < offset + count; i++)
        {
            crc ^= data[i];
            for (int bit = 0; bit < 8; bit++)
            {
                if ((crc & 0x0001) != 0)
                {
                    crc >>= 1;
                    crc ^= Crc16Polynomial;
                }
                else
                {
                    crc >>= 1;
                }
            }
        }

        return crc & 0xFFFF;
    }

    /// <summary>计算整个缓冲区的 Modbus CRC16。</summary>
    public static int CalculateCrc16(byte[] data) => CalculateCrc16(data, 0, data.Length);

    /// <summary>把 CRC16 追加到缓冲区末尾（低字节在前，符合 Modbus RTU 线序）。</summary>
    public static void AppendCrc16(byte[] frame, int offset, int count)
    {
        int crc = CalculateCrc16(frame, offset, count);
        frame[offset + count] = (byte)(crc & 0xFF);
        frame[offset + count + 1] = (byte)((crc >> 8) & 0xFF);
    }

    /// <summary>校验缓冲区末尾两字节的 CRC16 是否正确。</summary>
    public static bool ValidateCrc16(byte[] frame, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (count < 2)
        {
            return false;
        }

        int expected = CalculateCrc16(frame, offset, count - 2);
        int actual = (frame[offset + count - 2] & 0xFF) | ((frame[offset + count - 1] & 0xFF) << 8);
        return expected == actual;
    }

    /// <summary>计算 Modbus ASCII 的 LRC（纵向冗余校验，二进制补码）。</summary>
    public static byte CalculateLrc(byte[] data, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (offset < 0 || count < 0 || offset + count > data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "偏移或长度超出缓冲区范围。");
        }

        int sum = 0;
        for (int i = offset; i < offset + count; i++)
        {
            sum += data[i];
        }

        return (byte)((-sum) & 0xFF);
    }

    /// <summary>计算整个缓冲区的 LRC。</summary>
    public static byte CalculateLrc(byte[] data) => CalculateLrc(data, 0, data.Length);

    /// <summary>字节数组 → 十六进制字符串。</summary>
    public static string BytesToHex(byte[] data, int offset, int count, bool upperCase = true, string separator = "")
    {
        ArgumentNullException.ThrowIfNull(data);
        var format = upperCase ? "X2" : "x2";
        var builder = new StringBuilder(count * 2);
        for (int i = offset; i < offset + count; i++)
        {
            if (i > offset && separator.Length > 0)
            {
                builder.Append(separator);
            }

            builder.Append(data[i].ToString(format, CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    /// <summary>字节数组 → 十六进制字符串（全量）。</summary>
    public static string BytesToHex(byte[] data, bool upperCase = true, string separator = "") =>
        BytesToHex(data, 0, data.Length, upperCase, separator);

    /// <summary>十六进制字符串 → 字节数组（忽略空白与分隔符）。</summary>
    public static byte[] HexToBytes(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);
        var clean = new StringBuilder(hex.Length);
        foreach (char c in hex)
        {
            if (!char.IsWhiteSpace(c) && c != '-' && c != ':')
            {
                clean.Append(c);
            }
        }

        if (clean.Length % 2 != 0)
        {
            throw new FormatException("十六进制字符串长度必须为偶数。");
        }

        var bytes = new byte[clean.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = byte.Parse(clean.ToString(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return bytes;
    }

    /// <summary>把字节数组格式化为带空格的十六进制字符串，用于日志与调试。</summary>
    public static string Dump(byte[] data) => data is null || data.Length == 0
        ? string.Empty
        : BytesToHex(data, upperCase: true, separator: " ");
}
