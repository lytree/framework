using System.Buffers;
using Framework.Modbus.Tests.Fixtures;
using Abstractions = Framework.Modbus.Abstractions;

namespace Framework.Modbus.Tests;

/// <summary>
/// 协议契约层（YLFramework.Modbus.Abstractions）测试：主从共用常量、PDU 编解码、MBAP/RTU 帧编解码。
/// </summary>
public sealed class SlaveProtocolContractTests
{
    [Test]
    public async Task Constants_AreSingleSourceForMasterAndSlave()
    {
        // 主站公开常量应转发到契约层，值必须一致（防止两份定义漂移）
        var masterMaxPdu = Framework.Modbus.ModbusConstants.MaxPduSize;
        var masterMaxReadRegisters = Framework.Modbus.ModbusConstants.MaxReadRegisters;
        var masterMaxWriteRegisters = Framework.Modbus.ModbusConstants.MaxWriteRegisters;
        var masterReadInputDiscretes = Framework.Modbus.ModbusFunctionCode.ReadInputDiscretes;
        var masterWriteCoil = Framework.Modbus.ModbusFunctionCode.WriteCoil;
        var masterExceptionMask = Framework.Modbus.ModbusFunctionCode.ExceptionMask;

        var contractReadInputDiscretes = Abstractions.ModbusFunctionCode.ReadDiscreteInputs;
        var contractWriteSingleCoil = Abstractions.ModbusFunctionCode.WriteSingleCoil;
        var contractExceptionFlag = Abstractions.ModbusFunctionCode.ExceptionFlag;

        await Assert.That(masterMaxPdu).IsEqualTo(Abstractions.ModbusConstants.MaxPduSize);
        await Assert.That(masterMaxReadRegisters).IsEqualTo(Abstractions.ModbusConstants.MaxReadRegisters);
        await Assert.That(masterMaxWriteRegisters).IsEqualTo(Abstractions.ModbusConstants.MaxWriteRegisters);
        await Assert.That(contractReadInputDiscretes).IsEqualTo(masterReadInputDiscretes);
        await Assert.That(contractWriteSingleCoil).IsEqualTo(masterWriteCoil);
        await Assert.That(contractExceptionFlag).IsEqualTo(masterExceptionMask);
    }

    [Test]
    public async Task Checksums_SharedWithMasterUtil()
    {
        var data = TestHelpers.Hex("01 03 00 00 00 0A");
        await Assert.That(Abstractions.ModbusCrc16.Compute(data)).IsEqualTo((ushort)Framework.Modbus.Util.ModbusUtil.CalculateCrc16(data));
        await Assert.That(Abstractions.ModbusLrc.Compute(data)).IsEqualTo(Framework.Modbus.Util.ModbusUtil.CalculateLrc(data));
    }

    [Test]
    public async Task Requests_RoundTripThroughPduContract()
    {
        var vectors = new[]
        {
            "01 00 13 00 13",
            "02 00 00 00 08",
            "03 00 00 00 02",
            "04 00 05 00 01",
            "05 00 AC FF 00",
            "06 00 01 03 E8",
            "0F 00 00 00 0A 02 CD 01",
            "10 00 00 00 02 04 00 0A 00 0B",
        };

        foreach (var vector in vectors)
        {
            var pdu = TestHelpers.Hex(vector);
            var request = Abstractions.ModbusRequest.Create(pdu);
            await Assert.That(TestHelpers.Dump(request.ToByteArray())).IsEqualTo(vector);
        }
    }

    [Test]
    public async Task Requests_CarryParsedFields()
    {
        var read = (Abstractions.ReadCoilsRequest)Abstractions.ModbusRequest.Create(TestHelpers.Hex("01 00 13 00 13"));
        await Assert.That(read.Address).IsEqualTo((ushort)0x13);
        await Assert.That(read.Count).IsEqualTo((ushort)0x13);

        var writeCoil = (Abstractions.WriteSingleCoilRequest)Abstractions.ModbusRequest.Create(TestHelpers.Hex("05 00 AC FF 00"));
        await Assert.That(writeCoil.Address).IsEqualTo((ushort)0x00AC);
        await Assert.That(writeCoil.Value).IsTrue();

        var writeRegisters = (Abstractions.WriteMultipleRegistersRequest)Abstractions.ModbusRequest.Create(
            TestHelpers.Hex("10 00 00 00 02 04 00 0A 00 0B"));
        await Assert.That(writeRegisters.Count).IsEqualTo((ushort)2);
        await Assert.That(writeRegisters.Values[0]).IsEqualTo((ushort)10);
        await Assert.That(writeRegisters.Values[1]).IsEqualTo((ushort)11);
    }

    [Test]
    public async Task Requests_RejectIllegalFunctionAndCount()
    {
        var unknown = TestHelpers.ExpectThrows<Abstractions.ModbusSlaveException>(
            () => Abstractions.ModbusRequest.Create(TestHelpers.Hex("7F 00")));
        await Assert.That(unknown.ExceptionCode).IsEqualTo(Abstractions.ModbusExceptionCode.IllegalFunction);

        var tooMany = TestHelpers.ExpectThrows<Abstractions.ModbusSlaveException>(
            () => Abstractions.ModbusRequest.Create(TestHelpers.Hex("03 00 00 00 FF")));
        await Assert.That(tooMany.ExceptionCode).IsEqualTo(Abstractions.ModbusExceptionCode.IllegalDataValue);

        var badCoil = TestHelpers.ExpectThrows<Abstractions.ModbusSlaveException>(
            () => Abstractions.ModbusRequest.Create(TestHelpers.Hex("05 00 01 12 34")));
        await Assert.That(badCoil.ExceptionCode).IsEqualTo(Abstractions.ModbusExceptionCode.IllegalDataValue);
    }

    [Test]
    public async Task Responses_EncodeExpectedPdu()
    {
        await Assert.That(TestHelpers.Dump(new Abstractions.ReadBitsResponse(0x01, TestHelpers.Hex("CD 6B 05")).ToByteArray()))
            .IsEqualTo("01 03 CD 6B 05");
        await Assert.That(TestHelpers.Dump(new Abstractions.ReadRegistersResponse(0x03, new ushort[] { 0xAE41, 0xBD84 }).ToByteArray()))
            .IsEqualTo("03 04 AE 41 BD 84");
        await Assert.That(TestHelpers.Dump(new Abstractions.WriteSingleResponse(0x05, 0x00AC, 0xFF00).ToByteArray()))
            .IsEqualTo("05 00 AC FF 00");
        await Assert.That(TestHelpers.Dump(new Abstractions.WriteMultipleResponse(0x10, 0, 2).ToByteArray()))
            .IsEqualTo("10 00 00 00 02");
        await Assert.That(TestHelpers.Dump(new Abstractions.ModbusExceptionResponse(0x03, 0x02).ToByteArray()))
            .IsEqualTo("83 02");
    }

    [Test]
    public async Task MbapFrame_RoundTrips()
    {
        var codec = new Abstractions.MbapFrameCodec();
        var buffer = new ArrayBufferWriter<byte>();
        codec.WriteFrame(buffer, 0x11, 0x1234, new Abstractions.ReadRegistersResponse(0x03, new ushort[] { 0x000A }));

        await Assert.That(TestHelpers.Dump(buffer.WrittenSpan.ToArray()))
            .IsEqualTo("12 34 00 00 00 05 11 03 02 00 0A");

        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(buffer.WrittenSpan.ToArray()));
        await Assert.That(codec.TryReadFrame(ref reader, out var unitId, out var transactionId, out var pdu)).IsTrue();
        await Assert.That(unitId).IsEqualTo((byte)0x11);
        await Assert.That(transactionId).IsEqualTo((ushort)0x1234);
        await Assert.That(TestHelpers.Dump(pdu.ToArray())).IsEqualTo("03 02 00 0A");
    }

    [Test]
    public async Task RtuFrame_RoundTripsAndRejectsBadCrc()
    {
        var codec = new Abstractions.RtuFrameCodec();
        var buffer = new ArrayBufferWriter<byte>();
        codec.WriteFrame(buffer, 0x01, 0, new Abstractions.WriteSingleResponse(0x06, 0, 0x000A));

        var frame = buffer.WrittenSpan.ToArray();
        await Assert.That(frame.Length).IsEqualTo(8);

        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(frame));
        await Assert.That(codec.TryReadFrame(ref reader, out var unitId, out _, out var pdu)).IsTrue();
        await Assert.That(unitId).IsEqualTo((byte)0x01);
        await Assert.That(TestHelpers.Dump(pdu.ToArray())).IsEqualTo("06 00 00 00 0A");

        var corrupted = frame.ToArray();
        corrupted[^1] ^= 0xFF;
        var badReader = new SequenceReader<byte>(new ReadOnlySequence<byte>(corrupted));
        var error = new Abstractions.ModbusFrameException("占位");
        try
        {
            codec.TryReadFrame(ref badReader, out _, out _, out _);
        }
        catch (Abstractions.ModbusFrameException ex)
        {
            error = ex;
        }

        await Assert.That(error.Message).Contains("CRC");
    }

    [Test]
    public async Task RtuFrame_InfersLengthFromByteCountField()
    {
        var codec = new Abstractions.RtuFrameCodec();
        var pdu = new Abstractions.WriteMultipleRegistersRequest(0, 2, new ushort[] { 0x000A, 0x000B }).ToByteArray();
        var frame = new byte[1 + pdu.Length + Abstractions.ModbusCrc16.Length];
        frame[0] = 0x01;
        pdu.CopyTo(frame, 1);
        var crc = Abstractions.ModbusCrc16.Compute(frame.AsSpan(0, 1 + pdu.Length));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(1 + pdu.Length), crc);

        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(frame));
        await Assert.That(codec.TryReadFrame(ref reader, out _, out _, out var decoded)).IsTrue();
        await Assert.That(TestHelpers.Dump(decoded.ToArray())).IsEqualTo(TestHelpers.Dump(pdu));
    }
}
