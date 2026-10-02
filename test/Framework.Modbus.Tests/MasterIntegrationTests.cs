using Framework.Modbus.Facade;
using Framework.Modbus.IO;
using Framework.Modbus.Messages;
using Framework.Modbus.Net;
using Framework.Modbus.ProcessImage;
using Framework.Modbus.Tests.Fixtures;
using Framework.Modbus.Util;

namespace Framework.Modbus.Tests;

/// <summary>TCP 主站与本地回环从站的端到端集成测试。</summary>
public sealed class MasterIntegrationTests
{
    [Test]
    public async Task ReadCoils_ReturnsConfiguredStates()
    {
        using var slave = new LoopbackModbusSlave();
        slave.Coils[0] = true;
        slave.Coils[2] = true;
        slave.Coils[7] = true;

        using var master = CreateMaster(slave);
        var coils = master.ReadCoils(0, 8);

        await Assert.That(coils.Size).IsEqualTo(8);
        await Assert.That(coils.ToString()).IsEqualTo("10100001");
    }

    [Test]
    public async Task ReadInputDiscretes_ReturnsConfiguredStates()
    {
        using var slave = new LoopbackModbusSlave();
        slave.Coils[3] = true;

        using var master = CreateMaster(slave);
        var discretes = master.ReadInputDiscretes(0, 8);

        await Assert.That(discretes.GetBit(3)).IsTrue();
        await Assert.That(discretes.GetBit(0)).IsFalse();
    }

    [Test]
    public async Task ReadRegisters_ReturnsConfiguredValues()
    {
        using var slave = new LoopbackModbusSlave();
        slave.HoldingRegisters[0] = 0x1234;
        slave.HoldingRegisters[1] = 0x5678;
        slave.InputRegisters[0] = 0xABCD;

        using var master = CreateMaster(slave);

        var holding = master.ReadMultipleRegisters(0, 2);
        await Assert.That(holding[0].Value).IsEqualTo(0x1234);
        await Assert.That(holding[1].Value).IsEqualTo(0x5678);

        var input = master.ReadInputRegisterValues(0, 1);
        await Assert.That(input[0]).IsEqualTo(0xABCD);
    }

    [Test]
    public async Task WriteRegister_ThenReadBack()
    {
        using var slave = new LoopbackModbusSlave();
        using var master = CreateMaster(slave);

        master.WriteRegister(3, 4242);
        await Assert.That(slave.HoldingRegisters[3]).IsEqualTo(4242);

        var values = master.ReadMultipleRegisterValues(3, 1);
        await Assert.That(values[0]).IsEqualTo(4242);
    }

    [Test]
    public async Task WriteCoil_ThenReadBack()
    {
        using var slave = new LoopbackModbusSlave();
        using var master = CreateMaster(slave);

        master.WriteCoil(5, true);
        await Assert.That(slave.Coils[5]).IsTrue();

        master.WriteCoil(5, false);
        await Assert.That(slave.Coils[5]).IsFalse();
    }

    [Test]
    public async Task WriteMultipleRegisters_ThenReadBack()
    {
        using var slave = new LoopbackModbusSlave();
        using var master = CreateMaster(slave);

        master.WriteMultipleRegisters(10, new[] { 11, 22, 33 });

        await Assert.That(slave.HoldingRegisters[10]).IsEqualTo(11);
        await Assert.That(slave.HoldingRegisters[11]).IsEqualTo(22);
        await Assert.That(slave.HoldingRegisters[12]).IsEqualTo(33);
    }

    [Test]
    public async Task WriteMultipleCoils_ThenReadBack()
    {
        using var slave = new LoopbackModbusSlave();
        using var master = CreateMaster(slave);

        master.WriteMultipleCoils(0, BitVector.Parse("11001010"));

        await Assert.That(slave.Coils[0]).IsTrue();
        await Assert.That(slave.Coils[1]).IsTrue();
        await Assert.That(slave.Coils[2]).IsFalse();
        await Assert.That(slave.Coils[3]).IsFalse();
        await Assert.That(slave.Coils[4]).IsTrue();
    }

    [Test]
    public async Task SlaveException_IsRaisedForExceptionResponse()
    {
        using var slave = new LoopbackModbusSlave { ForceExceptionCode = ModbusExceptionCode.IllegalDataAddress };
        using var master = CreateMaster(slave);

        var error = TestHelpers.ExpectThrows<SlaveException>(() => master.ReadMultipleRegisters(0, 2));

        await Assert.That(error.ExceptionCode).IsEqualTo(ModbusExceptionCode.IllegalDataAddress);
        await Assert.That(error.FunctionCode).IsEqualTo(ModbusFunctionCode.ReadMultipleRegisters);
        await Assert.That(error.Message).Contains("Illegal data address");
    }

    [Test]
    public async Task SlaveException_IsRaisedForUnsupportedFunctionCode()
    {
        using var slave = new LoopbackModbusSlave();
        using var master = CreateMaster(slave);

        var error = TestHelpers.ExpectThrows<SlaveException>(() => master.ReadFifoQueue(0));

        await Assert.That(error.ExceptionCode).IsEqualTo(ModbusExceptionCode.IllegalFunction);
    }

    [Test]
    public async Task Transport_RetriesAfterDroppedResponse()
    {
        using var slave = new LoopbackModbusSlave { DropFirstRequests = 1 };
        slave.HoldingRegisters[0] = 7;

        using var master = CreateMaster(slave, timeout: 250);
        master.Retries = 3;

        var values = master.ReadMultipleRegisterValues(0, 1);

        await Assert.That(values[0]).IsEqualTo(7);
        await Assert.That(slave.WaitForRequests(2)).IsTrue();
        await Assert.That(slave.RequestCount).IsEqualTo(2);
    }

    [Test]
    public async Task Transport_SurfacesTimeoutWhenAllRetriesFail()
    {
        using var slave = new LoopbackModbusSlave { DropFirstRequests = 5 };

        using var master = CreateMaster(slave, timeout: 150);
        master.Retries = 2;

        TestHelpers.ExpectThrows<ModbusIOException>(() => master.ReadMultipleRegisters(0, 1));

        await Assert.That(slave.WaitForRequests(2)).IsTrue();
        await Assert.That(slave.RequestCount).IsEqualTo(2);
    }

    [Test]
    public async Task Transport_RaisesSendAndReceiveEvents()
    {
        using var slave = new LoopbackModbusSlave();
        slave.HoldingRegisters[0] = 1;

        using var master = CreateMaster(slave);
        var sent = new List<string>();
        var received = new List<string>();
        master.Transport.MessageSent += (_, e) => sent.Add(e.Hex);
        master.Transport.MessageReceived += (_, e) => received.Add(e.Hex);

        master.ReadMultipleRegisters(0, 1);

        await Assert.That(sent.Count).IsEqualTo(1);
        await Assert.That(received.Count).IsEqualTo(1);
        await Assert.That(sent[0]).StartsWith("00 01 00 00 00 06 01 03");
        await Assert.That(received[0]).IsEqualTo("00 01 00 00 00 05 01 03 02 00 01");
    }

    [Test]
    public async Task Transport_AssignsIncrementingTransactionIds()
    {
        using var slave = new LoopbackModbusSlave();
        using var master = CreateMaster(slave);

        int first = master.TransactionId;
        master.ReadMultipleRegisters(0, 1);
        int second = master.TransactionId;
        master.ReadMultipleRegisters(0, 1);
        int third = master.TransactionId;

        await Assert.That(second).IsEqualTo(first + 1);
        await Assert.That(third).IsEqualTo(second + 1);
    }

    [Test]
    public async Task TcpConnection_ThrowsForUnreachableEndpoint()
    {
        var connection = new TcpConnection("127.0.0.1", 1) { ReadTimeout = 200 };

        TestHelpers.ExpectThrows<ConnectionException>(() => connection.Connect());
        await Task.CompletedTask;
    }

    [Test]
    public async Task UdpTransport_ExposesUdpConnection()
    {
        using var transport = ModbusUdpTransport.Create("127.0.0.1", 1502);
        await Assert.That(transport.UdpConnection.Port).IsEqualTo(1502);
        await Assert.That(transport.IsConnected).IsFalse();
    }

    [Test]
    public async Task SerialMaster_SelectsTransportByEncoding()
    {
        using var asciiMaster = ModbusMasterFactory.CreateAsciiMaster(SerialParameters.CreateAscii("FAKE"));
        await Assert.That(asciiMaster.Transport).IsTypeOf<Framework.Modbus.IO.ModbusAsciiTransport>();

        using var rtuMaster = ModbusMasterFactory.CreateRtuMaster(SerialParameters.CreateRtu("FAKE"));
        await Assert.That(rtuMaster.Transport).IsTypeOf<Framework.Modbus.IO.ModbusRtuTransport>();
    }

    private static ModbusTcpMaster CreateMaster(LoopbackModbusSlave slave, int timeout = ModbusConstants.DefaultTimeout)
    {
        var master = ModbusMasterFactory.CreateTcpMaster("127.0.0.1", slave.Port, reconnect: true, timeout: timeout);
        master.SetUnitId(1);
        master.Connect();
        return master;
    }
}
