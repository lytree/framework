using Framework.Modbus.Util;

namespace Framework.Modbus.Tests.Fixtures;

/// <summary>测试辅助方法。</summary>
internal static class TestHelpers
{
    /// <summary>把十六进制字符串解析为字节数组（允许空格分隔）。</summary>
    public static byte[] Hex(string hex) => ModbusUtil.HexToBytes(hex);

    /// <summary>把字节数组格式化为带空格的十六进制字符串。</summary>
    public static string Dump(byte[] data) => ModbusUtil.BytesToHex(data, upperCase: true, separator: " ");

    /// <summary>断言执行指定动作会抛出指定异常。</summary>
    public static TException ExpectThrows<TException>(Action action) where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            action();
        }
        catch (TException expected)
        {
            return expected;
        }
        catch (Exception other)
        {
            throw new InvalidOperationException(
                $"期望抛出 {typeof(TException).Name}，实际抛出 {other.GetType().Name}：{other.Message}", other);
        }

        throw new InvalidOperationException($"期望抛出 {typeof(TException).Name}，但没有抛出任何异常。");
    }
}
