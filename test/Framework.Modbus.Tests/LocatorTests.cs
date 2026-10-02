using Framework.Modbus.Facade;
using Framework.Modbus.ProcessImage;
using Framework.Modbus.Tests.Fixtures;
using Framework.Modbus.Util;

namespace Framework.Modbus.Tests;

/// <summary>门面定位器（BaseLocator / ReadReference / WriteReference）测试。</summary>
public sealed class LocatorTests
{
    [Test]
    public async Task CoilStatus_ReturnsBitVector()
    {
        using var slave = new LoopbackModbusSlave();
        slave.Coils[0] = true;
        slave.Coils[1] = true;
        using var master = CreateMaster(slave);

        var result = master.Read(BaseLocator.CoilStatus(unitId: 1, reference: 0, count: 4));

        var bits = result as BitVector;
        await Assert.That(bits).IsNotNull();
        await Assert.That(bits!.ToString()).IsEqualTo("11000000");
    }

    [Test]
    public async Task HoldingRegister_ReturnsRegisterArray()
    {
        using var slave = new LoopbackModbusSlave();
        slave.HoldingRegisters[0] = 100;
        slave.HoldingRegisters[1] = 200;
        using var master = CreateMaster(slave);

        var result = master.Read(BaseLocator.HoldingRegister(unitId: 1, reference: 0, count: 2));

        var registers = result as Register[];
        await Assert.That(registers).IsNotNull();
        await Assert.That(registers![0].Value).IsEqualTo(100);
        await Assert.That(registers[1].Value).IsEqualTo(200);
    }

    [Test]
    public async Task InputRegister_ReadsFromInputArea()
    {
        using var slave = new LoopbackModbusSlave();
        slave.InputRegisters[4] = 999;
        using var master = CreateMaster(slave);

        var result = master.Read(BaseLocator.InputRegister(unitId: 1, reference: 4));

        var registers = result as Register[];
        await Assert.That(registers![0].Value).IsEqualTo(999);
    }

    [Test]
    public async Task WriteHoldingRegister_AppliesValue()
    {
        using var slave = new LoopbackModbusSlave();
        using var master = CreateMaster(slave);

        master.Write(BaseLocator.WriteHoldingRegister(unitId: 1, reference: 2), 3000);

        await Assert.That(slave.HoldingRegisters[2]).IsEqualTo(3000);
    }

    [Test]
    public async Task WriteCoil_UsesNonZeroAsOn()
    {
        using var slave = new LoopbackModbusSlave();
        using var master = CreateMaster(slave);

        master.Write(BaseLocator.WriteCoil(unitId: 1, reference: 6), 1);
        await Assert.That(slave.Coils[6]).IsTrue();

        master.Write(BaseLocator.WriteCoil(unitId: 1, reference: 6), 0);
        await Assert.That(slave.Coils[6]).IsFalse();
    }

    [Test]
    public async Task WriteReference_WithoutValue_UsesLastValue()
    {
        using var slave = new LoopbackModbusSlave();
        using var master = CreateMaster(slave);

        var reference = BaseLocator.WriteHoldingRegister(unitId: 1, reference: 1);
        reference.Value = 77;

        master.Write(reference);

        await Assert.That(slave.HoldingRegisters[1]).IsEqualTo(77);
    }

    [Test]
    public async Task ReadRestoresDefaultUnitIdAfterAddressing()
    {
        using var slave = new LoopbackModbusSlave();
        slave.HoldingRegisters[0] = 5;
        using var master = CreateMaster(slave);
        master.SetUnitId(9);

        master.Read(BaseLocator.HoldingRegister(unitId: 1, reference: 0));

        await Assert.That(master.UnitId).IsEqualTo(9);
    }

    [Test]
    public async Task ReadMultipleReferences_ReturnsResultsInOrder()
    {
        using var slave = new LoopbackModbusSlave();
        slave.Coils[0] = true;
        slave.HoldingRegisters[0] = 42;
        using var master = CreateMaster(slave);

        var results = master.Read(
            BaseLocator.CoilStatus(unitId: 1, reference: 0, count: 1),
            BaseLocator.HoldingRegister(unitId: 1, reference: 0, count: 1));

        await Assert.That(results.Length).IsEqualTo(2);
        await Assert.That(results[0] as BitVector).IsNotNull();
        await Assert.That((results[1] as Register[])![0].Value).IsEqualTo(42);
    }

    [Test]
    public async Task WriteMultipleReferences_RejectsLengthMismatch()
    {
        using var slave = new LoopbackModbusSlave();
        using var master = CreateMaster(slave);

        var references = new[] { BaseLocator.WriteCoil(1, 0) };
        var values = new[] { 1, 2 };

        TestHelpers.ExpectThrows<ArgumentException>(() => master.Write(references, values));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Reference_ValidatesRangeAndUnitId()
    {
        TestHelpers.ExpectThrows<ArgumentOutOfRangeException>(() => BaseLocator.CoilStatus(unitId: 256, reference: 0));
        TestHelpers.ExpectThrows<ArgumentOutOfRangeException>(() => BaseLocator.CoilStatus(unitId: 1, reference: 70000));
        TestHelpers.ExpectThrows<ArgumentOutOfRangeException>(() => BaseLocator.CoilStatus(unitId: 1, reference: 0, count: 0));
        await Task.CompletedTask;
    }

    [Test]
    public async Task WriteHoldingRegister_RejectsOutOfRangeValue()
    {
        using var slave = new LoopbackModbusSlave();
        using var master = CreateMaster(slave);

        var reference = BaseLocator.WriteHoldingRegister(1, 0);
        TestHelpers.ExpectThrows<ArgumentOutOfRangeException>(() => reference.Write(master, 70000));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Reference_ToStringDescribesAddressing()
    {
        var reference = BaseLocator.HoldingRegister(unitId: 2, reference: 10, count: 3);
        await Assert.That(reference.ToString()).IsEqualTo("HoldingRegister[2:10] ×3");
    }

    private static ModbusTcpMaster CreateMaster(LoopbackModbusSlave slave)
    {
        var master = ModbusMasterFactory.CreateTcpMaster("127.0.0.1", slave.Port);
        master.SetUnitId(1);
        master.Connect();
        return master;
    }
}
