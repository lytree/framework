using Framework.Modbus.Tests.Fixtures;
using Framework.Modbus.Util;

namespace Framework.Modbus.Tests;

/// <summary>CRC16 / LRC / 十六进制转换的校验向量测试。</summary>
public sealed class ChecksumTests
{
    [Test]
    public async Task Crc16_MatchesKnownModbusVector()
    {
        // Modbus 规范中的经典示例：读从站 1 的 10 个保持寄存器
        // CRC 值 0xCDC5，线序为低字节在前 → C5 CD
        var data = TestHelpers.Hex("01 03 00 00 00 0A");
        await Assert.That(ModbusUtil.CalculateCrc16(data)).IsEqualTo(0xCDC5);
    }

    [Test]
    public async Task Crc16_IsAppendedLowByteFirst()
    {
        var frame = new byte[8];
        TestHelpers.Hex("01 03 00 00 00 0A").CopyTo(frame, 0);
        ModbusUtil.AppendCrc16(frame, 0, 6);
        await Assert.That(TestHelpers.Dump(frame)).IsEqualTo("01 03 00 00 00 0A C5 CD");
    }

    [Test]
    public async Task Crc16_ValidatesCorrectFrame()
    {
        var frame = TestHelpers.Hex("01 03 00 00 00 0A C5 CD");
        await Assert.That(ModbusUtil.ValidateCrc16(frame, 0, frame.Length)).IsTrue();
    }

    [Test]
    public async Task Crc16_RejectsCorruptedFrame()
    {
        var frame = TestHelpers.Hex("01 03 00 00 00 0A C5 CE");
        await Assert.That(ModbusUtil.ValidateCrc16(frame, 0, frame.Length)).IsFalse();
    }

    [Test]
    public async Task Crc16_MatchesWriteMultipleRegistersVector()
    {
        var data = TestHelpers.Hex("01 10 00 00 00 02 04 00 0A 00 0B");
        await Assert.That(ModbusUtil.CalculateCrc16(data)).IsEqualTo(0x6A92);
    }

    [Test]
    public async Task Crc16_ThrowsWhenRangeExceedsBuffer()
    {
        var data = TestHelpers.Hex("01 03");
        var error = TestHelpers.ExpectThrows<ArgumentOutOfRangeException>(
            () => ModbusUtil.CalculateCrc16(data, 0, 10));
        await Assert.That(error.ParamName).IsEqualTo("count");
    }

    [Test]
    public async Task Lrc_MatchesKnownAsciiVector()
    {
        var data = TestHelpers.Hex("01 03 00 00 00 0A");
        await Assert.That((int)ModbusUtil.CalculateLrc(data)).IsEqualTo(0xF2);
    }

    [Test]
    public async Task Lrc_OfPduWithChecksumIsZero()
    {
        var payload = TestHelpers.Hex("01 03 00 00 00 0A");
        byte lrc = ModbusUtil.CalculateLrc(payload);
        var withChecksum = new byte[payload.Length + 1];
        payload.CopyTo(withChecksum, 0);
        withChecksum[^1] = lrc;

        await Assert.That((int)ModbusUtil.CalculateLrc(withChecksum)).IsEqualTo(0);
    }

    [Test]
    public async Task Hex_RoundTrips()
    {
        var bytes = TestHelpers.Hex("DE AD BE EF");
        await Assert.That(ModbusUtil.BytesToHex(bytes)).IsEqualTo("DEADBEEF");
        await Assert.That(TestHelpers.Dump(ModbusUtil.HexToBytes("DeadBeef"))).IsEqualTo("DE AD BE EF");
    }

    [Test]
    public async Task Hex_RejectsOddLength()
    {
        TestHelpers.ExpectThrows<FormatException>(() => ModbusUtil.HexToBytes("ABC"));
        await Task.CompletedTask;
    }

    [Test]
    public async Task CoilBytes_UseModbusOnOffEncoding()
    {
        await Assert.That(TestHelpers.Dump(ModbusUtil.CoilToBytes(true))).IsEqualTo("FF 00");
        await Assert.That(TestHelpers.Dump(ModbusUtil.CoilToBytes(false))).IsEqualTo("00 00");
    }

    [Test]
    public async Task RegistersAndBytes_RoundTripBigEndian()
    {
        var registers = new[] { 0x0001, 0xABCD, 0xFFFF };
        var bytes = ModbusUtil.RegistersToBytes(registers);
        await Assert.That(TestHelpers.Dump(bytes)).IsEqualTo("00 01 AB CD FF FF");

        var back = ModbusUtil.BytesToRegisters(bytes, 0, 3);
        await Assert.That(back[0]).IsEqualTo(0x0001);
        await Assert.That(back[1]).IsEqualTo(0xABCD);
        await Assert.That(back[2]).IsEqualTo(0xFFFF);
    }

    [Test]
    public async Task UInt16_And_Int32_ConvertBigEndian()
    {
        var bytes = TestHelpers.Hex("12 34 56 78");
        await Assert.That(ModbusUtil.BytesToUInt16(bytes, 0)).IsEqualTo(0x1234);
        await Assert.That(ModbusUtil.BytesToInt32(bytes, 0)).IsEqualTo(0x12345678);
    }
}
