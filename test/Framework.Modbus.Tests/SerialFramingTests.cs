using System.Text;
using Framework.Modbus.IO;
using Framework.Modbus.Messages;
using Framework.Modbus.Tests.Fixtures;
using Framework.Modbus.Util;

namespace Framework.Modbus.Tests;

/// <summary>RTU / ASCII 组帧、校验与解析测试（使用内存字节流，无需物理串口）。</summary>
public sealed class SerialFramingTests
{
    [Test]
    public async Task Rtu_WritesFrameWithTrailingCrc()
    {
        var (transport, connection) = CreateRtu("01 03 04 00 0A 00 0B 9B F6");

        transport.Send(new ReadMultipleRegistersRequest(0, 2));

        await Assert.That(connection.WrittenHex).IsEqualTo("01 03 00 00 00 02 C4 0B");
    }

    [Test]
    public async Task Rtu_ParsesRegisterResponse()
    {
        var (transport, _) = CreateRtu("01 03 04 00 0A 00 0B 9B F6");

        var response = (ReadMultipleRegistersResponse)transport.Send(new ReadMultipleRegistersRequest(0, 2));

        await Assert.That(response.Values[0]).IsEqualTo(10);
        await Assert.That(response.Values[1]).IsEqualTo(11);
        await Assert.That(response.UnitId).IsEqualTo(1);
    }

    [Test]
    public async Task Rtu_ParsesExceptionResponse()
    {
        var (transport, _) = CreateRtu("01 83 02 C0 F1");

        var response = transport.Send(new ReadMultipleRegistersRequest(0, 2));

        await Assert.That(response.IsException).IsTrue();
        var exception = (ExceptionResponse)response;
        await Assert.That(exception.OriginalFunctionCode).IsEqualTo(ModbusFunctionCode.ReadMultipleRegisters);
        await Assert.That(exception.ExceptionCode).IsEqualTo(ModbusExceptionCode.IllegalDataAddress);
    }

    [Test]
    public async Task Rtu_ParsesVariableLengthResponse()
    {
        var (transport, _) = CreateRtu("01 11 02 FF 64 FD 27");

        var response = (ReportSlaveIdResponse)transport.Send(new ReportSlaveIdRequest());

        await Assert.That(response.SlaveData.Length).IsEqualTo(2);
        await Assert.That(response.IsRunning).IsTrue();
    }

    [Test]
    public async Task Rtu_ParsesFifoQueueResponse()
    {
        var (transport, _) = CreateRtu("01 18 00 06 00 02 00 0A 00 0B F5 C7");

        var response = (ReadFifoQueueResponse)transport.Send(new ReadFifoQueueRequest(0x1234));

        await Assert.That(response.FifoCount).IsEqualTo(2);
        await Assert.That(response.Values[0]).IsEqualTo(10);
        await Assert.That(response.Values[1]).IsEqualTo(11);
    }

    [Test]
    public async Task Rtu_ParsesCommEventLogResponse()
    {
        var (transport, _) = CreateRtu("01 0C 08 00 00 00 0A 00 05 00 01 EC 27");

        var response = (GetCommEventLogResponse)transport.Send(new GetCommEventLogRequest());

        await Assert.That(response.EventCount).IsEqualTo(10);
        await Assert.That(response.MessageCount).IsEqualTo(5);
        await Assert.That(response.Events.Length).IsEqualTo(2);
    }

    [Test]
    public async Task Rtu_RejectsBadCrc()
    {
        var (transport, _) = CreateRtu("01 03 04 00 0A 00 0B 9B F7");

        TestHelpers.ExpectThrows<ModbusIOException>(() => transport.Send(new ReadMultipleRegistersRequest(0, 2)));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Rtu_RejectsFunctionCodeMismatch()
    {
        // 请求 FC01，但响应是 FC03
        var (transport, _) = CreateRtu("01 03 04 00 0A 00 0B 9B F6");

        var error = TestHelpers.ExpectThrows<ModbusIOException>(() => transport.Send(new ReadCoilsRequest(0, 2)));
        await Assert.That(error.Message).Contains("功能码");
    }

    [Test]
    public async Task Rtu_ReportsTimeoutWhenNoResponse()
    {
        var connection = new FakeConnection();
        var transport = new ModbusRtuTransport(connection, SerialParameters.CreateRtu("FAKE")) { Timeout = 60, Retries = 1 };
        transport.Connect();

        TestHelpers.ExpectThrows<ModbusIOException>(() => transport.Send(new ReadMultipleRegistersRequest(0, 2)));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Ascii_WritesFrameWithLrc()
    {
        var (transport, connection) = CreateAscii(":010304000A000BE3\r\n");

        transport.Send(new ReadMultipleRegistersRequest(0, 2));

        await Assert.That(connection.WrittenAscii).IsEqualTo(":010300000002FA\r\n");
    }

    [Test]
    public async Task Ascii_ParsesRegisterResponse()
    {
        var (transport, _) = CreateAscii(":010304000A000BE3\r\n");

        var response = (ReadMultipleRegistersResponse)transport.Send(new ReadMultipleRegistersRequest(0, 2));

        await Assert.That(response.Values[0]).IsEqualTo(10);
        await Assert.That(response.Values[1]).IsEqualTo(11);
    }

    [Test]
    public async Task Ascii_ParsesCoilResponse()
    {
        var (transport, _) = CreateAscii(":010103CD6B05BE\r\n");

        var response = (ReadCoilsResponse)transport.Send(new ReadCoilsRequest(0x13, 0x13));

        await Assert.That(response.Coils.Size).IsEqualTo(24);
        await Assert.That(response.Coils.GetBit(0)).IsTrue();
    }

    [Test]
    public async Task Ascii_RejectsBadLrc()
    {
        var (transport, _) = CreateAscii(":010304000A000BE4\r\n");

        TestHelpers.ExpectThrows<ModbusIOException>(() => transport.Send(new ReadMultipleRegistersRequest(0, 2)));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Ascii_RejectsNonHexPayload()
    {
        var (transport, _) = CreateAscii(":ZZZZ\r\n");

        TestHelpers.ExpectThrows<ModbusIOException>(() => transport.Send(new ReadMultipleRegistersRequest(0, 2)));
        await Task.CompletedTask;
    }

    private static (ModbusRtuTransport Transport, FakeConnection Connection) CreateRtu(string responseHex)
    {
        var connection = new FakeConnection
        {
            Responder = _ => TestHelpers.Hex(responseHex),
        };

        var transport = new ModbusRtuTransport(connection, SerialParameters.CreateRtu("FAKE"))
        {
            Timeout = 200,
            Retries = 1,
        };
        transport.Connect();
        return (transport, connection);
    }

    private static (ModbusAsciiTransport Transport, FakeConnection Connection) CreateAscii(string responseText)
    {
        var connection = new FakeConnection
        {
            Responder = _ => Encoding.ASCII.GetBytes(responseText),
        };

        var parameters = SerialParameters.CreateAscii("FAKE");
        var transport = new ModbusAsciiTransport(connection, parameters)
        {
            Timeout = 200,
            Retries = 1,
        };
        transport.Connect();
        return (transport, connection);
    }
}
