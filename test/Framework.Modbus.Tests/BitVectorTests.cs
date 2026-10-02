using Framework.Modbus.Tests.Fixtures;
using Framework.Modbus.Util;

namespace Framework.Modbus.Tests;

/// <summary>BitVector 的位序、打包与布尔运算测试。</summary>
public sealed class BitVectorTests
{
    [Test]
    public async Task NewVector_HasRequestedSizeAndByteCount()
    {
        var vector = new BitVector(19);
        await Assert.That(vector.Size).IsEqualTo(19);
        await Assert.That(vector.ByteCount).IsEqualTo(3);
        await Assert.That(vector.ToByteArray().Length).IsEqualTo(3);
    }

    [Test]
    public async Task BitOrder_IsLeastSignificantBitFirst()
    {
        var vector = new BitVector(8);
        vector.SetBit(0);
        vector.SetBit(2);

        // 线圈 0 对应首字节的 bit0（LSB），线圈 2 对应 bit2 → 0b0000_0101
        await Assert.That((int)vector.GetBytes()[0]).IsEqualTo(0x05);
    }

    [Test]
    public async Task ParseFromWireBytes_MatchesModbusSpecExample()
    {
        // 规范示例：读线圈 20..38，响应数据段为 CD 6B 05
        var vector = BitVector.FromBytes(TestHelpers.Hex("CD 6B 05"), 19);

        await Assert.That(vector.GetBit(0)).IsTrue();    // 线圈 20
        await Assert.That(vector.GetBit(1)).IsFalse();   // 线圈 21
        await Assert.That(vector.GetBit(2)).IsTrue();    // 线圈 22
        await Assert.That(vector.GetBit(3)).IsTrue();    // 线圈 23
        await Assert.That(vector.GetBit(18)).IsTrue();   // 线圈 38

        await Assert.That(vector.ToString()).IsEqualTo("10110011 11010110 101");
    }

    [Test]
    public async Task SetBitWithValue_WritesBothStates()
    {
        var vector = new BitVector(4);
        vector.SetBit(1, true);
        vector.SetBit(3, true);
        await Assert.That(vector.ToString()).IsEqualTo("0101");

        vector.SetBit(1, false);
        await Assert.That(vector.ToString()).IsEqualTo("0001");
    }

    [Test]
    public async Task ForceSize_PreservesExistingBits()
    {
        var vector = new BitVector(4);
        vector.SetBit(0);
        vector.SetBit(3);

        vector.ForceSize(10);
        await Assert.That(vector.Size).IsEqualTo(10);
        await Assert.That(vector.GetBit(0)).IsTrue();
        await Assert.That(vector.GetBit(3)).IsTrue();
        await Assert.That(vector.GetBit(4)).IsFalse();
    }

    [Test]
    public async Task SetBytes_ReplacesContentAndSize()
    {
        var vector = new BitVector(8);
        vector.SetBytes(TestHelpers.Hex("FF 00 5A"));
        await Assert.That(vector.Size).IsEqualTo(24);
        await Assert.That((int)vector.GetBytes()[2]).IsEqualTo(0x5A);
    }

    [Test]
    public async Task BooleanOperations_Work()
    {
        var a = BitVector.Parse("1100");
        var b = BitVector.Parse("1010");

        a.Xor(b);
        await Assert.That(a.ToString()).IsEqualTo("0110");

        a.Or(BitVector.Parse("1000"));
        await Assert.That(a.ToString()).IsEqualTo("1110");

        a.And(BitVector.Parse("1010"));
        await Assert.That(a.ToString()).IsEqualTo("1010");

        a.Not();
        await Assert.That(a.ToString()).IsEqualTo("0101");
    }

    [Test]
    public async Task BooleanOperations_RejectSizeMismatch()
    {
        var a = new BitVector(4);
        var error = TestHelpers.ExpectThrows<ArgumentException>(() => a.And(new BitVector(8)));
        await Assert.That(error.ParamName).IsEqualTo("other");
    }

    [Test]
    public async Task SetAllAndClearAll_ToggleEveryBit()
    {
        var vector = new BitVector(10);
        vector.SetAll();
        await Assert.That(vector.ToString()).IsEqualTo("11111111 11");

        vector.ClearAll();
        await Assert.That(vector.ToString()).IsEqualTo("00000000 00");
    }

    [Test]
    public async Task Equality_IgnoresTailPaddingBits()
    {
        var fromBits = BitVector.Parse("0101");                  // 位 1、3 → 0b1010
        var fromBytes = new BitVector(new byte[] { 0xFA }, 4);   // 高半字节为尾部填充位，应被屏蔽
        await Assert.That(fromBits.Equals(fromBytes)).IsTrue();
    }

    [Test]
    public async Task GetBit_ThrowsOutOfRange()
    {
        var vector = new BitVector(4);
        TestHelpers.ExpectThrows<ArgumentOutOfRangeException>(() => vector.GetBit(4));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Constructor_RejectsInvalidSize()
    {
        TestHelpers.ExpectThrows<ArgumentOutOfRangeException>(() => new BitVector(0));
        await Task.CompletedTask;
    }

    [Test]
    public async Task ToByteArray_MasksPaddingBits()
    {
        var vector = BitVector.Parse("1");
        await Assert.That(TestHelpers.Dump(vector.ToByteArray())).IsEqualTo("01");
    }
}
