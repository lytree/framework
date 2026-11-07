using Framework.Modbus.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Middleware.Modbus;
using Abstractions = Framework.Modbus.Abstractions;

namespace Framework.Modbus.Tests;

/// <summary>
/// Middleware Modbus 从站的扩展功能码测试：FC07 / FC08 / FC0B / FC0C / FC11 / FC14 / FC15 / FC16 / FC17 / FC18。
/// 每个功能码覆盖正常路径与至少一条异常路径；宿主使用内存假实现。
/// </summary>
public sealed class SlaveExtendedFunctionCodeTests
{
    private static ModbusSlave CreateSlave(ModbusSlaveHosts hosts, SimpleProcessImage? image = null)
    {
        var options = new ModbusSlaveOptions
        {
            UnitId = 1,
            SuppressBroadcastResponse = false,
        };

        return new ModbusSlave(options, image ?? new SimpleProcessImage(8, 8, 8, 8), NullLogger<ModbusSlave>.Instance, hosts);
    }

    private static ModbusSlaveHosts HostsOf(FakeSlaveHosts fake)
    {
        return new ModbusSlaveHosts(fake, fake, fake, fake, fake, fake);
    }

    private static async Task AssertResponseAsync(ModbusSlave slave, string requestHex, string expectedResponseHex)
    {
        var response = slave.Execute(1, TestHelpers.Hex(requestHex));
        await Assert.That(response).IsNotNull();
        await Assert.That(TestHelpers.Dump(response!.ToByteArray())).IsEqualTo(expectedResponseHex);
    }

    #region FC07 读异常状态

    [Test]
    public async Task Fc07_Slave_ReturnsExceptionStatusFromHost()
    {
        var fake = new FakeSlaveHosts { ExceptionStatusValue = 0x5A };
        var slave = CreateSlave(HostsOf(fake));
        await AssertResponseAsync(slave, "07", "07 5A");
    }

    [Test]
    public async Task Fc07_Slave_WithoutHost_ReturnsIllegalFunction()
    {
        var slave = CreateSlave(ModbusSlaveHosts.Default);
        await AssertResponseAsync(slave, "07", "87 01");
    }

    #endregion

    #region FC08 诊断

    [Test]
    public async Task Fc08_Slave_ReturnQueryDataEchoesRequest()
    {
        var slave = CreateSlave(ModbusSlaveHosts.Default);
        await AssertResponseAsync(slave, "08 00 00 AB CD", "08 00 00 AB CD");
    }

    [Test]
    public async Task Fc08_Slave_ReadsCounterFromHost()
    {
        var fake = new FakeSlaveHosts();
        fake.Counters[Abstractions.ModbusDiagnosticsSubFunction.ReturnBusExceptionErrorCount] = 7;
        var slave = CreateSlave(HostsOf(fake));
        await AssertResponseAsync(slave, "08 00 0D 00 00", "08 00 0D 00 07");
    }

    [Test]
    public async Task Fc08_Slave_UnsupportedSubFunction_ReturnsIllegalFunction()
    {
        var fake = new FakeSlaveHosts();
        var slave = CreateSlave(HostsOf(fake));
        // 0x0004 强制只听模式需要传输层配合，从站按不支持处理
        await AssertResponseAsync(slave, "08 00 04 00 00", "88 01");
    }

    [Test]
    public async Task Fc08_Slave_TruncatedRequest_ReturnsIllegalDataValue()
    {
        var slave = CreateSlave(ModbusSlaveHosts.Default);
        await AssertResponseAsync(slave, "08 00", "88 03");
    }

    #endregion

    #region FC0B 取通信事件计数器

    [Test]
    public async Task Fc0b_Slave_ReturnsCounterFromHost()
    {
        var fake = new FakeSlaveHosts { CommStatus = 0, EventCount = 5 };
        var slave = CreateSlave(HostsOf(fake));
        await AssertResponseAsync(slave, "0B", "0B 00 00 00 05");
    }

    [Test]
    public async Task Fc0b_Slave_WithoutHost_ReturnsIllegalFunction()
    {
        var slave = CreateSlave(ModbusSlaveHosts.Default);
        await AssertResponseAsync(slave, "0B", "8B 01");
    }

    #endregion

    #region FC0C 取通信事件记录

    [Test]
    public async Task Fc0c_Slave_ReturnsEventLogFromHost()
    {
        var fake = new FakeSlaveHosts
        {
            CommStatus = 0,
            EventCount = 2,
            MessageCount = 10,
            Events = new byte[] { 0x20, 0x40 },
        };
        var slave = CreateSlave(HostsOf(fake));
        await AssertResponseAsync(slave, "0C", "0C 08 00 00 00 02 00 0A 20 40");
    }

    [Test]
    public async Task Fc0c_Slave_WithoutHost_ReturnsIllegalFunction()
    {
        var slave = CreateSlave(ModbusSlaveHosts.Default);
        await AssertResponseAsync(slave, "0C", "8C 01");
    }

    #endregion

    #region FC11 报告从站标识

    [Test]
    public async Task Fc11_Slave_ReturnsSlaveIdFromHost()
    {
        var fake = new FakeSlaveHosts { SlaveId = new byte[] { 0x59, 0x4C, 0xFF } };
        var slave = CreateSlave(HostsOf(fake));
        await AssertResponseAsync(slave, "11", "11 03 59 4C FF");
    }

    [Test]
    public async Task Fc11_Slave_WithoutHost_ReturnsIllegalFunction()
    {
        var slave = CreateSlave(ModbusSlaveHosts.Default);
        await AssertResponseAsync(slave, "11", "91 01");
    }

    #endregion

    #region FC14 读文件记录

    [Test]
    public async Task Fc14_Slave_ReadsFileRecordsFromHost()
    {
        var fake = new FakeSlaveHosts();
        fake.FileCells[(1, 0)] = 0x000A;
        fake.FileCells[(1, 1)] = 0x000B;

        var slave = CreateSlave(HostsOf(fake));
        await AssertResponseAsync(slave, "14 07 06 00 01 00 00 00 02", "14 06 05 06 00 0A 00 0B");
    }

    [Test]
    public async Task Fc14_Slave_UnknownRecord_ReturnsIllegalDataAddress()
    {
        var fake = new FakeSlaveHosts();
        var slave = CreateSlave(HostsOf(fake));
        await AssertResponseAsync(slave, "14 07 06 00 01 00 00 00 02", "94 02");
    }

    [Test]
    public async Task Fc14_Slave_WithoutHost_ReturnsIllegalFunction()
    {
        var slave = CreateSlave(ModbusSlaveHosts.Default);
        await AssertResponseAsync(slave, "14 07 06 00 01 00 00 00 02", "94 01");
    }

    #endregion

    #region FC15 写文件记录

    [Test]
    public async Task Fc15_Slave_WritesFileRecordsAndEchoesRequest()
    {
        var fake = new FakeSlaveHosts();
        var slave = CreateSlave(HostsOf(fake));

        const string request = "15 0B 06 00 01 00 00 00 02 00 0A 00 0B";
        await AssertResponseAsync(slave, request, request);

        await Assert.That(fake.FileCells[(1, 0)]).IsEqualTo((ushort)0x000A);
        await Assert.That(fake.FileCells[(1, 1)]).IsEqualTo((ushort)0x000B);
    }

    [Test]
    public async Task Fc15_Slave_RejectedWrite_ReturnsIllegalDataAddress()
    {
        var fake = new FakeSlaveHosts { RejectFileWrites = true };
        var slave = CreateSlave(HostsOf(fake));
        await AssertResponseAsync(slave, "15 0B 06 00 01 00 00 00 02 00 0A 00 0B", "95 02");
    }

    [Test]
    public async Task Fc15_Slave_WithoutHost_ReturnsIllegalFunction()
    {
        var slave = CreateSlave(ModbusSlaveHosts.Default);
        await AssertResponseAsync(slave, "15 0B 06 00 01 00 00 00 02 00 0A 00 0B", "95 01");
    }

    #endregion

    #region FC16 屏蔽写寄存器

    [Test]
    public async Task Fc16_Slave_MaskWritesHoldingRegister()
    {
        var image = new SimpleProcessImage(8, 8, 8, 8);
        image.WriteHoldingRegister(0, 0x1234);

        var slave = CreateSlave(ModbusSlaveHosts.Default, image);
        // (0x1234 & 0x00F2) | (0x0025 & 0xFF0D) = 0x0035
        await AssertResponseAsync(slave, "16 00 00 00 F2 00 25", "16 00 00 00 F2 00 25");
        await Assert.That(image.ReadHoldingRegister(0)).IsEqualTo((ushort)0x0035);
    }

    [Test]
    public async Task Fc16_Slave_OutOfRangeAddress_ReturnsIllegalDataAddress()
    {
        var slave = CreateSlave(ModbusSlaveHosts.Default, new SimpleProcessImage(8, 8, 8, 8));
        await AssertResponseAsync(slave, "16 00 08 00 F2 00 25", "96 02");
    }

    #endregion

    #region FC17 读写多个寄存器

    [Test]
    public async Task Fc17_Slave_WritesThenReads()
    {
        var image = new SimpleProcessImage(8, 8, 8, 8);
        image.WriteHoldingRegister(0, 0x0001);
        image.WriteHoldingRegister(1, 0x0002);
        image.WriteHoldingRegister(2, 0x0003);
        image.WriteHoldingRegister(3, 0x0004);

        var slave = CreateSlave(ModbusSlaveHosts.Default, image);
        // 读 0..3（4 个），写 2..3 = 0xAAAA、0xBBBB；写先于读
        await AssertResponseAsync(slave, "17 00 00 00 04 00 02 00 02 04 AA AA BB BB", "17 08 00 01 00 02 AA AA BB BB");

        await Assert.That(image.ReadHoldingRegister(2)).IsEqualTo((ushort)0xAAAA);
        await Assert.That(image.ReadHoldingRegister(3)).IsEqualTo((ushort)0xBBBB);
    }

    [Test]
    public async Task Fc17_Slave_ReadOutOfRange_ReturnsIllegalDataAddress()
    {
        var slave = CreateSlave(ModbusSlaveHosts.Default, new SimpleProcessImage(8, 8, 8, 8));
        // 读地址 9 超出 0..7
        await AssertResponseAsync(slave, "17 00 09 00 01 00 00 00 01 02 00 00", "97 02");
    }

    #endregion

    #region FC18 读 FIFO 队列

    [Test]
    public async Task Fc18_Slave_ReadsFifoFromHost()
    {
        var fake = new FakeSlaveHosts();
        fake.FifoQueues[1] = new ushort[] { 0x000A, 0x000B };

        var slave = CreateSlave(HostsOf(fake));
        await AssertResponseAsync(slave, "18 00 01", "18 00 06 00 02 00 0A 00 0B");
    }

    [Test]
    public async Task Fc18_Slave_UnknownAddress_ReturnsIllegalDataAddress()
    {
        var fake = new FakeSlaveHosts();
        var slave = CreateSlave(HostsOf(fake));
        await AssertResponseAsync(slave, "18 00 01", "98 02");
    }

    [Test]
    public async Task Fc18_Slave_WithoutHost_ReturnsIllegalFunction()
    {
        var slave = CreateSlave(ModbusSlaveHosts.Default);
        await AssertResponseAsync(slave, "18 00 01", "98 01");
    }

    #endregion

    #region 契约层分派

    [Test]
    public async Task ModbusRequest_Create_DispatchesExtendedFunctionCodes()
    {
        var vectors = new[]
        {
            "07",
            "08 00 00 12 34",
            "0B",
            "0C",
            "11",
            "14 07 06 00 01 00 00 00 02",
            "15 0B 06 00 01 00 00 00 02 00 0A 00 0B",
            "16 00 00 00 F2 00 25",
            "17 00 00 00 04 00 02 00 02 04 AA AA BB BB",
            "18 00 01",
        };

        foreach (var vector in vectors)
        {
            var pdu = TestHelpers.Hex(vector);
            var request = Abstractions.ModbusRequest.Create(pdu);
            await Assert.That(TestHelpers.Dump(request.ToByteArray())).IsEqualTo(vector);
        }
    }

    #endregion
}

/// <summary>
/// 内存版的扩展功能码宿主，同时实现六个宿主接口，便于按需注入测试状态。
/// </summary>
internal sealed class FakeSlaveHosts : IModbusSlaveExceptionStatusHost,
    IModbusSlaveDiagnosticsHost,
    IModbusSlaveCommunicationEventHost,
    IModbusSlaveIdentityHost,
    IModbusFileRecordStore,
    IModbusFifoQueueStore
{
    public byte ExceptionStatusValue { get; set; }

    public ushort DiagnosticRegisterValue { get; set; }

    public Dictionary<ushort, ushort> Counters { get; } = new();

    public ushort CommStatus { get; set; }

    public ushort EventCount { get; set; }

    public ushort MessageCount { get; set; }

    public byte[] Events { get; set; } = [];

    public byte[] SlaveId { get; set; } = [0xFF];

    public Dictionary<(ushort File, ushort Record), ushort> FileCells { get; } = new();

    public bool RejectFileWrites { get; set; }

    public Dictionary<ushort, ushort[]> FifoQueues { get; } = new();

    public byte GetExceptionStatus() => this.ExceptionStatusValue;

    public ushort GetDiagnosticRegister() => this.DiagnosticRegisterValue;

    public ushort GetCounter(ushort subFunction) => this.Counters.GetValueOrDefault(subFunction);

    public ushort GetStatus() => this.CommStatus;

    public ushort GetEventCount() => this.EventCount;

    public ushort GetMessageCount() => this.MessageCount;

    public ReadOnlyMemory<byte> GetEvents() => this.Events;

    public ReadOnlyMemory<byte> GetSlaveIdData() => this.SlaveId;

    public void ClearCounters() => this.Counters.Clear();

    public void ClearOverrunCounter()
    {
        this.Counters[Abstractions.ModbusDiagnosticsSubFunction.ReturnBusCharacterOverrunCount] = 0;
    }

    public ushort[]? Read(ushort fileNumber, ushort recordNumber, ushort recordLength)
    {
        var values = new ushort[recordLength];
        for (var i = 0; i < recordLength; i++)
        {
            var key = (fileNumber, (ushort)(recordNumber + i));
            if (this.FileCells.TryGetValue(key, out var value) == false)
            {
                return null;
            }

            values[i] = value;
        }

        return values;
    }

    public bool Write(ushort fileNumber, ushort startRecord, ReadOnlySpan<ushort> values)
    {
        if (this.RejectFileWrites)
        {
            return false;
        }

        for (var i = 0; i < values.Length; i++)
        {
            this.FileCells[(fileNumber, (ushort)(startRecord + i))] = values[i];
        }

        return true;
    }

    public ushort[]? Read(ushort address) => this.FifoQueues.GetValueOrDefault(address);
}
