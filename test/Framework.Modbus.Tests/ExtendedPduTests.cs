using Framework.Modbus.Tests.Fixtures;
using Abstractions = Framework.Modbus.Abstractions;

namespace Framework.Modbus.Tests;

/// <summary>
/// 扩展功能码 PDU 编解码测试：FC07 / FC08 / FC0B / FC0C / FC11 / FC14 / FC15 / FC16 / FC17 / FC18。
/// </summary>
public sealed class ExtendedPduTests
{
    /// <summary>断言指定动作抛出异常码为「非法数据值」的从站异常。</summary>
    private static async Task AssertRejectedAsync(Action action)
    {
        var error = TestHelpers.ExpectThrows<Abstractions.ModbusSlaveException>(action);
        await Assert.That(error.ExceptionCode).IsEqualTo(Abstractions.ModbusExceptionCode.IllegalDataValue);
    }

    [Test]
    public async Task Fc07_ReadExceptionStatus_RoundTrips()
    {
        // 请求不携带数据域：PDU 仅含功能码。
        var request = new Abstractions.ReadExceptionStatusRequest();
        var expectedRequestHex = "07";
        await Assert.That(TestHelpers.Dump(request.ToByteArray())).IsEqualTo(expectedRequestHex);
        await Assert.That(request.Length).IsEqualTo(1);

        var decodedRequest = Abstractions.ReadExceptionStatusRequest.Decode(ReadOnlySpan<byte>.Empty);
        await Assert.That(decodedRequest.FunctionCode).IsEqualTo(Abstractions.ModbusFunctionCode.ReadExceptionStatus);

        await AssertRejectedAsync(() => Abstractions.ReadExceptionStatusRequest.Decode(TestHelpers.Hex("00")));

        // 响应数据域为单字节状态。
        var response = new Abstractions.ReadExceptionStatusResponse((byte)0xFF);
        var expectedResponseHex = "07 FF";
        await Assert.That(TestHelpers.Dump(response.ToByteArray())).IsEqualTo(expectedResponseHex);

        var decodedResponse = Abstractions.ReadExceptionStatusResponse.Decode(
            Abstractions.ModbusFunctionCode.ReadExceptionStatus,
            TestHelpers.Hex("07 FF"));
        await Assert.That(decodedResponse.Status).IsEqualTo((byte)0xFF);

        await AssertRejectedAsync(() => Abstractions.ReadExceptionStatusResponse.Decode(
            Abstractions.ModbusFunctionCode.ReadExceptionStatus,
            TestHelpers.Hex("06 FF")));
        await AssertRejectedAsync(() => Abstractions.ReadExceptionStatusResponse.Decode(
            Abstractions.ModbusFunctionCode.ReadExceptionStatus,
            TestHelpers.Hex("07")));
    }

    [Test]
    public async Task Fc08_Diagnostics_RoundTrips()
    {
        // 请求 / 响应布局一致：FC + 子功能码(2) + 数据。
        var request = new Abstractions.DiagnosticsRequest((ushort)0x0000, TestHelpers.Hex("12 34"));
        var expectedRequestHex = "08 00 00 12 34";
        await Assert.That(TestHelpers.Dump(request.ToByteArray())).IsEqualTo(expectedRequestHex);
        await Assert.That(request.SubFunction).IsEqualTo((ushort)0x0000);
        await Assert.That(request.DataWord).IsEqualTo((ushort)0x1234);

        var decodedRequest = Abstractions.DiagnosticsRequest.Decode(TestHelpers.Hex("00 00 12 34"));
        var expectedDataHex = "12 34";
        await Assert.That(decodedRequest.SubFunction).IsEqualTo((ushort)0x0000);
        await Assert.That(TestHelpers.Dump(decodedRequest.Data.ToArray())).IsEqualTo(expectedDataHex);

        await AssertRejectedAsync(() => Abstractions.DiagnosticsRequest.Decode(TestHelpers.Hex("00 00 12")));

        var response = new Abstractions.DiagnosticsResponse((ushort)0x0000, TestHelpers.Hex("12 34"));
        var expectedResponseHex = "08 00 00 12 34";
        await Assert.That(TestHelpers.Dump(response.ToByteArray())).IsEqualTo(expectedResponseHex);

        var decodedResponse = Abstractions.DiagnosticsResponse.Decode(
            Abstractions.ModbusFunctionCode.Diagnostics,
            TestHelpers.Hex("08 00 00 12 34"));
        await Assert.That(decodedResponse.SubFunction).IsEqualTo((ushort)0x0000);
        await Assert.That(decodedResponse.DataWord).IsEqualTo((ushort)0x1234);
        await Assert.That(TestHelpers.Dump(decodedResponse.Data.ToArray())).IsEqualTo(expectedDataHex);

        await AssertRejectedAsync(() => Abstractions.DiagnosticsResponse.Decode(
            Abstractions.ModbusFunctionCode.Diagnostics,
            TestHelpers.Hex("08 00 00 12")));
    }

    [Test]
    public async Task Fc0B_GetCommEventCounter_RoundTrips()
    {
        var request = new Abstractions.GetCommEventCounterRequest();
        var expectedRequestHex = "0B";
        await Assert.That(TestHelpers.Dump(request.ToByteArray())).IsEqualTo(expectedRequestHex);
        await Assert.That(request.Length).IsEqualTo(1);

        var decodedRequest = Abstractions.GetCommEventCounterRequest.Decode(ReadOnlySpan<byte>.Empty);
        await Assert.That(decodedRequest.FunctionCode).IsEqualTo(Abstractions.ModbusFunctionCode.GetCommEventCounter);

        await AssertRejectedAsync(() => Abstractions.GetCommEventCounterRequest.Decode(TestHelpers.Hex("00")));

        // 响应数据域为「状态字(2) + 事件计数(2)」，共 5 字节 PDU。
        var response = new Abstractions.GetCommEventCounterResponse((ushort)0x0000, (ushort)0x0001);
        var expectedResponseHex = "0B 00 00 00 01";
        await Assert.That(TestHelpers.Dump(response.ToByteArray())).IsEqualTo(expectedResponseHex);
        await Assert.That(response.Length).IsEqualTo(5);

        var decodedResponse = Abstractions.GetCommEventCounterResponse.Decode(
            Abstractions.ModbusFunctionCode.GetCommEventCounter,
            TestHelpers.Hex("0B 00 00 00 01"));
        await Assert.That(decodedResponse.Status).IsEqualTo((ushort)0x0000);
        await Assert.That(decodedResponse.EventCount).IsEqualTo((ushort)0x0001);

        await AssertRejectedAsync(() => Abstractions.GetCommEventCounterResponse.Decode(
            Abstractions.ModbusFunctionCode.GetCommEventCounter,
            TestHelpers.Hex("0B 00 00 00")));
    }

    [Test]
    public async Task Fc0C_GetCommEventLog_RoundTrips()
    {
        var request = new Abstractions.GetCommEventLogRequest();
        var expectedRequestHex = "0C";
        await Assert.That(TestHelpers.Dump(request.ToByteArray())).IsEqualTo(expectedRequestHex);
        await Assert.That(request.Length).IsEqualTo(1);

        var decodedRequest = Abstractions.GetCommEventLogRequest.Decode(ReadOnlySpan<byte>.Empty);
        await Assert.That(decodedRequest.FunctionCode).IsEqualTo(Abstractions.ModbusFunctionCode.GetCommEventLog);

        await AssertRejectedAsync(() => Abstractions.GetCommEventLogRequest.Decode(TestHelpers.Hex("00")));

        // 响应：FC + byteCount(1) + status(2) + eventCount(2) + messageCount(2) + 事件字节。
        var response = new Abstractions.GetCommEventLogResponse(
            (ushort)0x0000,
            (ushort)0x0102,
            (ushort)0x0003,
            TestHelpers.Hex("20 00"));
        var expectedResponseHex = "0C 08 00 00 01 02 00 03 20 00";
        await Assert.That(TestHelpers.Dump(response.ToByteArray())).IsEqualTo(expectedResponseHex);
        await Assert.That(response.ByteCount).IsEqualTo(8);
        await Assert.That(response.Length).IsEqualTo(10);

        var decodedResponse = Abstractions.GetCommEventLogResponse.Decode(
            Abstractions.ModbusFunctionCode.GetCommEventLog,
            TestHelpers.Hex("0C 08 00 00 01 02 00 03 20 00"));
        var expectedEventsHex = "20 00";
        await Assert.That(decodedResponse.Status).IsEqualTo((ushort)0x0000);
        await Assert.That(decodedResponse.EventCount).IsEqualTo((ushort)0x0102);
        await Assert.That(decodedResponse.MessageCount).IsEqualTo((ushort)0x0003);
        await Assert.That(TestHelpers.Dump(decodedResponse.Events.ToArray())).IsEqualTo(expectedEventsHex);

        await AssertRejectedAsync(() => Abstractions.GetCommEventLogResponse.Decode(
            Abstractions.ModbusFunctionCode.GetCommEventLog,
            TestHelpers.Hex("0C 05 00 00 01 02 00 03")));
        await AssertRejectedAsync(() => Abstractions.GetCommEventLogResponse.Decode(
            Abstractions.ModbusFunctionCode.GetCommEventLog,
            TestHelpers.Hex("0C 08 00 00 01 02 00 03 20")));
    }

    [Test]
    public async Task Fc11_ReportSlaveId_RoundTrips()
    {
        var request = new Abstractions.ReportSlaveIdRequest();
        var expectedRequestHex = "11";
        await Assert.That(TestHelpers.Dump(request.ToByteArray())).IsEqualTo(expectedRequestHex);
        await Assert.That(request.Length).IsEqualTo(1);

        var decodedRequest = Abstractions.ReportSlaveIdRequest.Decode(ReadOnlySpan<byte>.Empty);
        await Assert.That(decodedRequest.FunctionCode).IsEqualTo(Abstractions.ModbusFunctionCode.ReportSlaveId);

        await AssertRejectedAsync(() => Abstractions.ReportSlaveIdRequest.Decode(TestHelpers.Hex("00")));

        var response = new Abstractions.ReportSlaveIdResponse(TestHelpers.Hex("01 02 03"));
        var expectedResponseHex = "11 03 01 02 03";
        await Assert.That(TestHelpers.Dump(response.ToByteArray())).IsEqualTo(expectedResponseHex);
        await Assert.That(response.ByteCount).IsEqualTo(3);

        var decodedResponse = Abstractions.ReportSlaveIdResponse.Decode(
            Abstractions.ModbusFunctionCode.ReportSlaveId,
            TestHelpers.Hex("11 03 01 02 03"));
        var expectedDataHex = "01 02 03";
        await Assert.That(TestHelpers.Dump(decodedResponse.Data.ToArray())).IsEqualTo(expectedDataHex);

        // 声明的字节计数大于实际可用数据。
        await AssertRejectedAsync(() => Abstractions.ReportSlaveIdResponse.Decode(
            Abstractions.ModbusFunctionCode.ReportSlaveId,
            TestHelpers.Hex("11 05 01 02")));
        await AssertRejectedAsync(() => Abstractions.ReportSlaveIdResponse.Decode(
            Abstractions.ModbusFunctionCode.ReportSlaveId,
            TestHelpers.Hex("11")));
    }

    [Test]
    public async Task Fc14_ReadFileRecord_RoundTrips()
    {
        // 请求：FC + byteCount(1) + 每项 [0x06, file(2), record(2), length(2)]。
        var request = new Abstractions.ReadFileRecordRequest(new[]
        {
            new Abstractions.FileRecordReference((ushort)0x0001, (ushort)0x0002, (ushort)0x0003),
        });
        var expectedRequestHex = "14 07 06 00 01 00 02 00 03";
        await Assert.That(TestHelpers.Dump(request.ToByteArray())).IsEqualTo(expectedRequestHex);
        await Assert.That(request.ByteCount).IsEqualTo(7);
        await Assert.That(request.Length).IsEqualTo(9);

        var decodedRequest = Abstractions.ReadFileRecordRequest.Decode(TestHelpers.Hex("07 06 00 01 00 02 00 03"));
        await Assert.That(decodedRequest.Records.Length).IsEqualTo(1);
        await Assert.That(decodedRequest.Records[0].FileNumber).IsEqualTo((ushort)0x0001);
        await Assert.That(decodedRequest.Records[0].RecordNumber).IsEqualTo((ushort)0x0002);
        await Assert.That(decodedRequest.Records[0].RecordLength).IsEqualTo((ushort)0x0003);

        // 引用类型必须为 0x06。
        await AssertRejectedAsync(() => Abstractions.ReadFileRecordRequest.Decode(TestHelpers.Hex("07 05 00 01 00 02 00 03")));
        // 字节计数必须是 7 的整数倍。
        await AssertRejectedAsync(() => Abstractions.ReadFileRecordRequest.Decode(TestHelpers.Hex("08 06 00 01 00 02 00 03 00")));

        // 响应：FC + byteCount(1) + 每段 [subLen(1)=1+2N, 0x06, 值(2N)]。
        var response = new Abstractions.ReadFileRecordResponse(new[] { new ushort[] { 0x0A0B, 0x0C0D } });
        var expectedResponseHex = "14 06 05 06 0A 0B 0C 0D";
        await Assert.That(TestHelpers.Dump(response.ToByteArray())).IsEqualTo(expectedResponseHex);
        await Assert.That(response.ByteCount).IsEqualTo(6);

        var decodedResponse = Abstractions.ReadFileRecordResponse.Decode(
            Abstractions.ModbusFunctionCode.ReadFileRecord,
            TestHelpers.Hex("14 06 05 06 0A 0B 0C 0D"));
        await Assert.That(decodedResponse.Records.Length).IsEqualTo(1);
        await Assert.That(decodedResponse.Records[0].Length).IsEqualTo(2);
        await Assert.That(decodedResponse.Records[0][0]).IsEqualTo((ushort)0x0A0B);
        await Assert.That(decodedResponse.Records[0][1]).IsEqualTo((ushort)0x0C0D);

        await AssertRejectedAsync(() => Abstractions.ReadFileRecordResponse.Decode(
            Abstractions.ModbusFunctionCode.ReadFileRecord,
            TestHelpers.Hex("14 06 05 07 0A 0B 0C 0D")));
    }

    [Test]
    public async Task Fc15_WriteFileRecord_RoundTrips()
    {
        // 请求 / 响应同为回显布局：FC + byteCount(1) + 每项 [0x06, file(2), start(2), count(2), 值(2count)]。
        var records = new[]
        {
            new Abstractions.FileRecordWrite((ushort)0x0001, (ushort)0x0002, new ushort[] { 0x000A }),
        };
        var request = new Abstractions.WriteFileRecordRequest(records);
        var expectedHex = "15 09 06 00 01 00 02 00 01 00 0A";
        await Assert.That(TestHelpers.Dump(request.ToByteArray())).IsEqualTo(expectedHex);
        await Assert.That(request.ByteCount).IsEqualTo(9);
        await Assert.That(request.Length).IsEqualTo(11);

        var decodedRequest = Abstractions.WriteFileRecordRequest.Decode(TestHelpers.Hex("09 06 00 01 00 02 00 01 00 0A"));
        await Assert.That(decodedRequest.Records.Length).IsEqualTo(1);
        await Assert.That(decodedRequest.Records[0].FileNumber).IsEqualTo((ushort)0x0001);
        await Assert.That(decodedRequest.Records[0].StartRecord).IsEqualTo((ushort)0x0002);
        await Assert.That(decodedRequest.Records[0].Count).IsEqualTo(1);
        await Assert.That(decodedRequest.Records[0].Values[0]).IsEqualTo((ushort)0x000A);

        await AssertRejectedAsync(() => Abstractions.WriteFileRecordRequest.Decode(TestHelpers.Hex("09 05 00 01 00 02 00 01 00 0A")));

        var response = new Abstractions.WriteFileRecordResponse(records);
        await Assert.That(TestHelpers.Dump(response.ToByteArray())).IsEqualTo(expectedHex);

        var decodedResponse = Abstractions.WriteFileRecordResponse.Decode(
            Abstractions.ModbusFunctionCode.WriteFileRecord,
            TestHelpers.Hex("15 09 06 00 01 00 02 00 01 00 0A"));
        await Assert.That(decodedResponse.Records.Length).IsEqualTo(1);
        await Assert.That(decodedResponse.Records[0].FileNumber).IsEqualTo((ushort)0x0001);
        await Assert.That(decodedResponse.Records[0].Values[0]).IsEqualTo((ushort)0x000A);

        await AssertRejectedAsync(() => Abstractions.WriteFileRecordResponse.Decode(
            Abstractions.ModbusFunctionCode.WriteFileRecord,
            TestHelpers.Hex("14 09 06 00 01 00 02 00 01 00 0A")));
    }

    [Test]
    public async Task Fc16_MaskWriteRegister_RoundTrips()
    {
        // 请求 / 响应布局一致：FC + address(2) + andMask(2) + orMask(2)。
        var request = new Abstractions.MaskWriteRegisterRequest((ushort)0x0004, (ushort)0x00F2, (ushort)0x0025);
        var expectedHex = "16 00 04 00 F2 00 25";
        await Assert.That(TestHelpers.Dump(request.ToByteArray())).IsEqualTo(expectedHex);
        await Assert.That(request.Length).IsEqualTo(7);

        var decodedRequest = Abstractions.MaskWriteRegisterRequest.Decode(TestHelpers.Hex("00 04 00 F2 00 25"));
        await Assert.That(decodedRequest.Address).IsEqualTo((ushort)0x0004);
        await Assert.That(decodedRequest.AndMask).IsEqualTo((ushort)0x00F2);
        await Assert.That(decodedRequest.OrMask).IsEqualTo((ushort)0x0025);

        await AssertRejectedAsync(() => Abstractions.MaskWriteRegisterRequest.Decode(TestHelpers.Hex("00 04 00 F2 00")));
        await AssertRejectedAsync(() => Abstractions.MaskWriteRegisterRequest.Decode(TestHelpers.Hex("00 04 00 F2 00 25 00")));

        var response = new Abstractions.MaskWriteRegisterResponse((ushort)0x0004, (ushort)0x00F2, (ushort)0x0025);
        await Assert.That(TestHelpers.Dump(response.ToByteArray())).IsEqualTo(expectedHex);

        var decodedResponse = Abstractions.MaskWriteRegisterResponse.Decode(
            Abstractions.ModbusFunctionCode.MaskWriteRegister,
            TestHelpers.Hex("16 00 04 00 F2 00 25"));
        await Assert.That(decodedResponse.Address).IsEqualTo((ushort)0x0004);
        await Assert.That(decodedResponse.AndMask).IsEqualTo((ushort)0x00F2);
        await Assert.That(decodedResponse.OrMask).IsEqualTo((ushort)0x0025);

        await AssertRejectedAsync(() => Abstractions.MaskWriteRegisterResponse.Decode(
            Abstractions.ModbusFunctionCode.MaskWriteRegister,
            TestHelpers.Hex("16 00 04 00 F2")));
    }

    [Test]
    public async Task Fc17_ReadWriteMultipleRegisters_RoundTrips()
    {
        // 请求：FC + readAddr(2) + readCount(2) + writeAddr(2) + writeCount(2) + byteCount(1) + 写数据。
        var request = new Abstractions.ReadWriteMultipleRegistersRequest(
            (ushort)0x0003,
            (ushort)0x0006,
            (ushort)0x000E,
            new ushort[] { 0x00FF, 0x00FF, 0x00FF });
        var expectedRequestHex = "17 00 03 00 06 00 0E 00 03 06 00 FF 00 FF 00 FF";
        await Assert.That(TestHelpers.Dump(request.ToByteArray())).IsEqualTo(expectedRequestHex);
        await Assert.That(request.Length).IsEqualTo(16);
        await Assert.That(request.WriteCount).IsEqualTo((ushort)0x0003);

        var decodedRequest = Abstractions.ReadWriteMultipleRegistersRequest.Decode(
            TestHelpers.Hex("00 03 00 06 00 0E 00 03 06 00 FF 00 FF 00 FF"));
        await Assert.That(decodedRequest.ReadAddress).IsEqualTo((ushort)0x0003);
        await Assert.That(decodedRequest.ReadCount).IsEqualTo((ushort)0x0006);
        await Assert.That(decodedRequest.WriteAddress).IsEqualTo((ushort)0x000E);
        await Assert.That(decodedRequest.WriteCount).IsEqualTo((ushort)0x0003);
        await Assert.That(decodedRequest.WriteValues[0]).IsEqualTo((ushort)0x00FF);
        await Assert.That(decodedRequest.WriteValues[2]).IsEqualTo((ushort)0x00FF);

        // 读数量必须落在 1~125。
        await AssertRejectedAsync(() => Abstractions.ReadWriteMultipleRegistersRequest.Decode(
            TestHelpers.Hex("00 03 00 00 00 0E 00 01 02 00 01")));
        await AssertRejectedAsync(() => Abstractions.ReadWriteMultipleRegistersRequest.Decode(
            TestHelpers.Hex("00 03 00 7E 00 0E 00 01 02 00 01")));
        // 字节计数必须等于写数量 * 2。
        await AssertRejectedAsync(() => Abstractions.ReadWriteMultipleRegistersRequest.Decode(
            TestHelpers.Hex("00 03 00 06 00 0E 00 01 04 00 01")));

        // 响应：FC + byteCount(1) + 寄存器数据。
        var response = new Abstractions.ReadWriteMultipleRegistersResponse(new ushort[] { 0x00FF, 0x00FF });
        var expectedResponseHex = "17 04 00 FF 00 FF";
        await Assert.That(TestHelpers.Dump(response.ToByteArray())).IsEqualTo(expectedResponseHex);

        var decodedResponse = Abstractions.ReadWriteMultipleRegistersResponse.Decode(
            TestHelpers.Hex("17 04 00 FF 00 FF"));
        await Assert.That(decodedResponse.Registers.Length).IsEqualTo(2);
        await Assert.That(decodedResponse.Registers[0]).IsEqualTo((ushort)0x00FF);
        await Assert.That(decodedResponse.Registers[1]).IsEqualTo((ushort)0x00FF);

        await AssertRejectedAsync(() => Abstractions.ReadWriteMultipleRegistersResponse.Decode(
            TestHelpers.Hex("03 04 00 FF 00 FF")));
    }

    [Test]
    public async Task Fc18_ReadFifoQueue_RoundTrips()
    {
        // 请求：FC + FIFO 指针地址(2)。
        var request = new Abstractions.ReadFifoQueueRequest((ushort)0x04DE);
        var expectedRequestHex = "18 04 DE";
        await Assert.That(TestHelpers.Dump(request.ToByteArray())).IsEqualTo(expectedRequestHex);
        await Assert.That(request.Length).IsEqualTo(3);

        var decodedRequest = Abstractions.ReadFifoQueueRequest.Decode(TestHelpers.Hex("04 DE"));
        await Assert.That(decodedRequest.Address).IsEqualTo((ushort)0x04DE);

        await AssertRejectedAsync(() => Abstractions.ReadFifoQueueRequest.Decode(TestHelpers.Hex("04 DE 00")));
        await AssertRejectedAsync(() => Abstractions.ReadFifoQueueRequest.Decode(TestHelpers.Hex("04")));

        // 响应：FC + byteCount(2) + fifoCount(2) + 值(2N)，byteCount = 2 + 2N。
        var response = new Abstractions.ReadFifoQueueResponse(new ushort[] { 0x01B8, 0x1284 });
        var expectedResponseHex = "18 00 06 00 02 01 B8 12 84";
        await Assert.That(TestHelpers.Dump(response.ToByteArray())).IsEqualTo(expectedResponseHex);
        await Assert.That(response.ByteCount).IsEqualTo(6);
        await Assert.That(response.FifoCount).IsEqualTo(2);

        var decodedResponse = Abstractions.ReadFifoQueueResponse.Decode(
            Abstractions.ModbusFunctionCode.ReadFifoQueue,
            TestHelpers.Hex("18 00 06 00 02 01 B8 12 84"));
        await Assert.That(decodedResponse.FifoCount).IsEqualTo(2);
        await Assert.That(decodedResponse.Values[0]).IsEqualTo((ushort)0x01B8);
        await Assert.That(decodedResponse.Values[1]).IsEqualTo((ushort)0x1284);

        // byteCount 与 fifoCount 不一致。
        await AssertRejectedAsync(() => Abstractions.ReadFifoQueueResponse.Decode(
            Abstractions.ModbusFunctionCode.ReadFifoQueue,
            TestHelpers.Hex("18 00 06 00 01 01 B8 12 84")));
        // FIFO 数量上限为 31。
        await AssertRejectedAsync(() => { _ = new Abstractions.ReadFifoQueueResponse(new ushort[32]); });
    }
}
