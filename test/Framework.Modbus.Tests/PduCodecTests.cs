using Framework.Modbus.Messages;
using Framework.Modbus.ProcessImage;
using Framework.Modbus.Tests.Fixtures;
using Framework.Modbus.Util;

namespace Framework.Modbus.Tests;

/// <summary>全部功能码的 PDU 编解码测试（对照固定十六进制向量）。</summary>
public sealed class PduCodecTests
{
    // ------------------------------------------------------------------
    // 请求编码
    // ------------------------------------------------------------------

    [Test]
    public async Task ReadBitsRequests_EncodeExpectedPdu()
    {
        await Assert.That(TestHelpers.Dump(new ReadCoilsRequest(0x13, 0x13).ToPdu())).IsEqualTo("01 13 00 13");
        await Assert.That(TestHelpers.Dump(new ReadInputDiscretesRequest(0, 8).ToPdu())).IsEqualTo("02 00 00 00 08");
    }

    [Test]
    public async Task ReadRegisterRequests_EncodeExpectedPdu()
    {
        await Assert.That(TestHelpers.Dump(new ReadMultipleRegistersRequest(0, 2).ToPdu())).IsEqualTo("03 00 00 00 02");
        await Assert.That(TestHelpers.Dump(new ReadInputRegistersRequest(5, 1).ToPdu())).IsEqualTo("04 00 05 00 01");
    }

    [Test]
    public async Task SingleWriteRequests_EncodeExpectedPdu()
    {
        await Assert.That(TestHelpers.Dump(new WriteCoilRequest(0x00AC, true).ToPdu())).IsEqualTo("05 00 AC FF 00");
        await Assert.That(TestHelpers.Dump(new WriteCoilRequest(0x00AC, false).ToPdu())).IsEqualTo("05 00 AC 00 00");
        await Assert.That(TestHelpers.Dump(new WriteRegisterRequest(1, 1000).ToPdu())).IsEqualTo("06 00 01 03 E8");
    }

    [Test]
    public async Task DiagnosticsAndStatusRequests_EncodeExpectedPdu()
    {
        await Assert.That(TestHelpers.Dump(new ReadExceptionStatusRequest().ToPdu())).IsEqualTo("07");
        await Assert.That(TestHelpers.Dump(new DiagnosticsRequest(0x0000, 0x1234).ToPdu())).IsEqualTo("08 00 00 12 34");
        await Assert.That(TestHelpers.Dump(new GetCommEventCounterRequest().ToPdu())).IsEqualTo("0B");
        await Assert.That(TestHelpers.Dump(new GetCommEventLogRequest().ToPdu())).IsEqualTo("0C");
    }

    [Test]
    public async Task MultipleWriteRequests_EncodeExpectedPdu()
    {
        var coils = BitVector.Parse("10000101");
        await Assert.That(TestHelpers.Dump(new WriteMultipleCoilsRequest(0, coils).ToPdu()))
            .IsEqualTo("0F 00 00 00 08 01 85");

        var registers = new[] { new SimpleRegister(0x000A), new SimpleRegister(0x000B) };
        await Assert.That(TestHelpers.Dump(new WriteMultipleRegistersRequest(0, registers).ToPdu()))
            .IsEqualTo("10 00 00 00 02 04 00 0A 00 0B");
    }

    [Test]
    public async Task ReportSlaveIdRequest_EncodesExpectedPdu()
    {
        await Assert.That(TestHelpers.Dump(new ReportSlaveIdRequest().ToPdu())).IsEqualTo("11");
    }

    [Test]
    public async Task FileRecordRequests_EncodeExpectedPdu()
    {
        var read = new ReadFileRecordRequest(new[] { new FileRecordReference(4, 1, 2) });
        await Assert.That(TestHelpers.Dump(read.ToPdu())).IsEqualTo("14 07 06 00 04 00 01 00 02");

        var write = new WriteFileRecordRequest(new[]
        {
            new FileRecord(4, new[] { new Record(1, 0x000A), new Record(2, 0x000B) }),
        });
        await Assert.That(TestHelpers.Dump(write.ToPdu()))
            .IsEqualTo("15 0B 06 00 04 00 01 00 02 00 0A 00 0B");
    }

    [Test]
    public async Task MaskWriteAndReadWriteRequests_EncodeExpectedPdu()
    {
        await Assert.That(TestHelpers.Dump(new MaskWriteRegisterRequest(4, 0x00F2, 0x0025).ToPdu()))
            .IsEqualTo("16 00 04 00 F2 00 25");

        var request = new ReadWriteMultipleRegistersRequest(0, 2, 3, new[] { 1, 2 });
        await Assert.That(TestHelpers.Dump(request.ToPdu()))
            .IsEqualTo("17 00 00 00 02 00 03 00 02 04 00 01 00 02");
    }

    [Test]
    public async Task ReadFifoQueueRequest_EncodesExpectedPdu()
    {
        await Assert.That(TestHelpers.Dump(new ReadFifoQueueRequest(0x1234).ToPdu())).IsEqualTo("18 12 34");
    }

    // ------------------------------------------------------------------
    // 响应解析
    // ------------------------------------------------------------------

    [Test]
    public async Task ReadBitsResponse_ParsesBits()
    {
        var response = (ReadCoilsResponse)ModbusResponseFactory.FromPdu(TestHelpers.Hex("01 03 CD 6B 05"));
        await Assert.That(response.Coils.Size).IsEqualTo(19);
        await Assert.That(response.Coils.ToString()).IsEqualTo("10110011 11010110 101");
    }

    [Test]
    public async Task ReadRegistersResponses_ParseBigEndianValues()
    {
        var holding = (ReadMultipleRegistersResponse)ModbusResponseFactory.FromPdu(TestHelpers.Hex("03 04 00 0A 00 0B"));
        await Assert.That(holding.Values[0]).IsEqualTo(10);
        await Assert.That(holding.Values[1]).IsEqualTo(11);
        await Assert.That(holding.Registers[0].Value).IsEqualTo(10);

        var input = (ReadInputRegistersResponse)ModbusResponseFactory.FromPdu(TestHelpers.Hex("04 02 12 34"));
        await Assert.That(input.Values[0]).IsEqualTo(0x1234);
    }

    [Test]
    public async Task SingleWriteResponses_EchoRequest()
    {
        var coil = (WriteCoilResponse)ModbusResponseFactory.FromPdu(TestHelpers.Hex("05 00 AC FF 00"));
        await Assert.That(coil.Reference).IsEqualTo(0xAC);
        await Assert.That(coil.CoilState).IsTrue();

        var register = (WriteRegisterResponse)ModbusResponseFactory.FromPdu(TestHelpers.Hex("06 00 01 03 E8"));
        await Assert.That(register.Reference).IsEqualTo(1);
        await Assert.That(register.Value).IsEqualTo(1000);
    }

    [Test]
    public async Task DiagnosticsResponses_ParseFields()
    {
        var status = (ReadExceptionStatusResponse)ModbusResponseFactory.FromPdu(TestHelpers.Hex("07 6D"));
        await Assert.That(status.ExceptionStatus).IsEqualTo(0x6D);

        var diagnostics = (DiagnosticsResponse)ModbusResponseFactory.FromPdu(TestHelpers.Hex("08 00 00 12 34"));
        await Assert.That(diagnostics.SubFunction).IsEqualTo(0);
        await Assert.That(diagnostics.Data).IsEqualTo(0x1234);
    }

    [Test]
    public async Task CommEventResponses_ParseFields()
    {
        var counter = (GetCommEventCounterResponse)ModbusResponseFactory.FromPdu(TestHelpers.Hex("0B FF FF 00 0A"));
        await Assert.That(counter.IsBusy).IsTrue();
        await Assert.That(counter.EventCount).IsEqualTo(10);

        var log = (GetCommEventLogResponse)ModbusResponseFactory.FromPdu(
            TestHelpers.Hex("0C 08 00 00 00 0A 00 05 00 01"));
        await Assert.That(log.MessageCount).IsEqualTo(5);
        await Assert.That(log.Events.Length).IsEqualTo(2);
    }

    [Test]
    public async Task MultipleWriteResponses_ParseEchoedRange()
    {
        var coils = (WriteMultipleCoilsResponse)ModbusResponseFactory.FromPdu(TestHelpers.Hex("0F 00 00 00 08"));
        await Assert.That(coils.BitCount).IsEqualTo(8);

        var registers = (WriteMultipleRegistersResponse)ModbusResponseFactory.FromPdu(TestHelpers.Hex("10 00 00 00 02"));
        await Assert.That(registers.WordCount).IsEqualTo(2);
    }

    [Test]
    public async Task ReportSlaveIdResponse_ExposesSlaveData()
    {
        var response = (ReportSlaveIdResponse)ModbusResponseFactory.FromPdu(TestHelpers.Hex("11 02 FF 64"));
        await Assert.That(response.SlaveData.Length).IsEqualTo(2);
        await Assert.That(response.IsRunning).IsTrue();
    }

    [Test]
    public async Task FileRecordResponse_ParsesRecords()
    {
        var response = (ReadFileRecordResponse)ModbusResponseFactory.FromPdu(TestHelpers.Hex("14 04 03 06 00 0A"));
        await Assert.That(response.FileRecords.Length).IsEqualTo(1);
        await Assert.That(response.FileRecords[0].Count).IsEqualTo(1);
        await Assert.That(response.FileRecords[0][0].Value).IsEqualTo(10);
    }

    [Test]
    public async Task WriteFileRecordResponse_ParsesEcho()
    {
        var response = (WriteFileRecordResponse)ModbusResponseFactory.FromPdu(
            TestHelpers.Hex("15 0B 06 00 04 00 01 00 02 00 0A 00 0B"));
        await Assert.That(response.FileRecords[0].FileNumber).IsEqualTo(4);
        await Assert.That(response.FileRecords[0][1].Value).IsEqualTo(11);
    }

    [Test]
    public async Task MaskWriteAndReadWriteResponses_ParseFields()
    {
        var mask = (MaskWriteRegisterResponse)ModbusResponseFactory.FromPdu(TestHelpers.Hex("16 00 04 00 F2 00 25"));
        await Assert.That(mask.AndMask).IsEqualTo(0xF2);
        await Assert.That(mask.OrMask).IsEqualTo(0x25);

        var readWrite = (ReadWriteMultipleRegistersResponse)ModbusResponseFactory.FromPdu(
            TestHelpers.Hex("17 04 00 0A 00 0B"));
        await Assert.That(readWrite.Values[1]).IsEqualTo(11);
    }

    [Test]
    public async Task ReadFifoQueueResponse_ParsesValues()
    {
        var response = (ReadFifoQueueResponse)ModbusResponseFactory.FromPdu(
            TestHelpers.Hex("18 00 06 00 02 00 0A 00 0B"));
        await Assert.That(response.FifoCount).IsEqualTo(2);
        await Assert.That(response.Values[0]).IsEqualTo(10);
        await Assert.That(response.Values[1]).IsEqualTo(11);
    }

    // ------------------------------------------------------------------
    // 往返与错误处理
    // ------------------------------------------------------------------

    [Test]
    public async Task Requests_RoundTripThroughFromPdu()
    {
        var coilsRequest = ReadCoilsRequest.FromPdu(new ReadCoilsRequest(0x13, 0x13).ToPdu());
        await Assert.That(coilsRequest.Reference).IsEqualTo(0x13);
        await Assert.That(coilsRequest.BitCount).IsEqualTo(0x13);

        var registersRequest = ReadMultipleRegistersRequest.FromPdu(new ReadMultipleRegistersRequest(7, 3).ToPdu());
        await Assert.That(registersRequest.Reference).IsEqualTo(7);
        await Assert.That(registersRequest.WordCount).IsEqualTo(3);

        var writeRequest = WriteMultipleRegistersRequest.FromPdu(new WriteMultipleRegistersRequest(0, new[] { 1, 2 }).ToPdu());
        await Assert.That(writeRequest.WordCount).IsEqualTo(2);
        await Assert.That(writeRequest.Registers[1].Value).IsEqualTo(2);
    }

    [Test]
    public async Task ExceptionResponse_IsParsedWithOriginalFunctionCode()
    {
        var response = (ExceptionResponse)ModbusResponseFactory.FromPdu(TestHelpers.Hex("83 02"));
        await Assert.That(response.OriginalFunctionCode).IsEqualTo(ModbusFunctionCode.ReadMultipleRegisters);
        await Assert.That(response.ExceptionCode).IsEqualTo(ModbusExceptionCode.IllegalDataAddress);
        await Assert.That(response.IsException).IsTrue();
        await Assert.That(TestHelpers.Dump(response.ToPdu())).IsEqualTo("83 02");
    }

    [Test]
    public async Task UnknownFunctionCode_ThrowsModbusIOException()
    {
        TestHelpers.ExpectThrows<ModbusIOException>(() => ModbusResponseFactory.FromPdu(TestHelpers.Hex("7F 00")));
        await Task.CompletedTask;
    }

    [Test]
    public async Task TruncatedResponse_ThrowsModbusIOException()
    {
        TestHelpers.ExpectThrows<ModbusIOException>(() => ModbusResponseFactory.FromPdu(TestHelpers.Hex("03 04 00 0A")));
        await Task.CompletedTask;
    }

    [Test]
    public async Task PduWriter_GrowsBeyondInitialBuffer()
    {
        var writer = new ModbusPduWriter();
        var payload = new byte[400];
        Array.Fill(payload, (byte)0x5A);
        writer.WriteByte(ModbusFunctionCode.WriteMultipleRegisters);
        writer.WriteBytes(payload);

        await Assert.That(writer.Length).IsEqualTo(401);
        await Assert.That(writer.ToArray()[400]).IsEqualTo(0x5A);
    }
}
