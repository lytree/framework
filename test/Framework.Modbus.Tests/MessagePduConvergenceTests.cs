using Framework.Modbus.Messages;
using Framework.Modbus.Tests.Fixtures;
using Framework.Modbus.Util;
using Abstractions = Framework.Modbus.Abstractions;

namespace Framework.Modbus.Tests;

/// <summary>
/// 主站报文与协议契约层 PDU 的收敛测试：核心功能码（FC01/02/03/04/05/06/15/16）的编码结果、
/// 往返解析与异常类型必须与 <c>Framework.Modbus.Abstractions</c> 保持一致。
/// </summary>
public sealed class MessagePduConvergenceTests
{
    [Test]
    public async Task ReadRequests_EncodeAsContract()
    {
        var coils = new ReadCoilsRequest(0x0013, 0x0013);
        await Assert.That(TestHelpers.Dump(coils.ToPdu()))
            .IsEqualTo(TestHelpers.Dump(new Abstractions.ReadCoilsRequest(0x0013, 0x0013).ToByteArray()));

        var discretes = new ReadInputDiscretesRequest(0, 8);
        await Assert.That(TestHelpers.Dump(discretes.ToPdu()))
            .IsEqualTo(TestHelpers.Dump(new Abstractions.ReadDiscreteInputsRequest(0, 8).ToByteArray()));

        var holding = new ReadMultipleRegistersRequest(0, 2);
        await Assert.That(TestHelpers.Dump(holding.ToPdu()))
            .IsEqualTo(TestHelpers.Dump(new Abstractions.ReadHoldingRegistersRequest(0, 2).ToByteArray()));

        var input = new ReadInputRegistersRequest(5, 1);
        await Assert.That(TestHelpers.Dump(input.ToPdu()))
            .IsEqualTo(TestHelpers.Dump(new Abstractions.ReadInputRegistersRequest(5, 1).ToByteArray()));

        var coilsRoundTrip = ReadCoilsRequest.FromPdu(coils.ToPdu());
        await Assert.That(coilsRoundTrip.Reference).IsEqualTo(0x13);
        await Assert.That(coilsRoundTrip.BitCount).IsEqualTo(0x13);

        var discretesRoundTrip = ReadInputDiscretesRequest.FromPdu(discretes.ToPdu());
        await Assert.That(discretesRoundTrip.Reference).IsEqualTo(0);
        await Assert.That(discretesRoundTrip.BitCount).IsEqualTo(8);

        var holdingRoundTrip = ReadMultipleRegistersRequest.FromPdu(holding.ToPdu());
        await Assert.That(holdingRoundTrip.Reference).IsEqualTo(0);
        await Assert.That(holdingRoundTrip.WordCount).IsEqualTo(2);

        var inputRoundTrip = ReadInputRegistersRequest.FromPdu(input.ToPdu());
        await Assert.That(inputRoundTrip.Reference).IsEqualTo(5);
        await Assert.That(inputRoundTrip.WordCount).IsEqualTo(1);
    }

    [Test]
    public async Task ReadResponses_EncodeAndRoundTripAsContract()
    {
        var bits = BitVector.Parse("10000101");
        var coils = new ReadCoilsResponse(bits);
        await Assert.That(TestHelpers.Dump(coils.ToPdu()))
            .IsEqualTo(TestHelpers.Dump(
                new Abstractions.ReadBitsResponse(ModbusFunctionCode.ReadCoils, bits.ToByteArray()).ToByteArray()));

        var coilsRoundTrip = ReadCoilsResponse.FromPdu(coils.ToPdu());
        await Assert.That(coilsRoundTrip.Coils).IsEqualTo(bits);

        var registers = new ReadMultipleRegistersResponse(new[] { 0x000A, 0x000B });
        await Assert.That(TestHelpers.Dump(registers.ToPdu()))
            .IsEqualTo(TestHelpers.Dump(
                new Abstractions.ReadRegistersResponse(ModbusFunctionCode.ReadMultipleRegisters, new ushort[] { 10, 11 })
                    .ToByteArray()));

        var registersRoundTrip = ReadMultipleRegistersResponse.FromPdu(registers.ToPdu());
        await Assert.That(registersRoundTrip.Values).IsEquivalentTo(new[] { 0x000A, 0x000B });
    }

    [Test]
    public async Task WriteRequests_EncodeAndRoundTripAsContract()
    {
        var coil = new WriteCoilRequest(0x00AC, true);
        await Assert.That(TestHelpers.Dump(coil.ToPdu()))
            .IsEqualTo(TestHelpers.Dump(new Abstractions.WriteSingleCoilRequest(0x00AC, true).ToByteArray()));

        var register = new WriteRegisterRequest(1, 1000);
        await Assert.That(TestHelpers.Dump(register.ToPdu()))
            .IsEqualTo(TestHelpers.Dump(new Abstractions.WriteSingleRegisterRequest(1, 1000).ToByteArray()));

        var coils = BitVector.Parse("10000101");
        var multipleCoils = new WriteMultipleCoilsRequest(0, coils);
        await Assert.That(TestHelpers.Dump(multipleCoils.ToPdu()))
            .IsEqualTo(TestHelpers.Dump(
                new Abstractions.WriteMultipleCoilsRequest(0, 8, new[] { true, false, false, false, false, true, false, true })
                    .ToByteArray()));

        var multipleRegisters = new WriteMultipleRegistersRequest(0, new[] { 10, 11 });
        await Assert.That(TestHelpers.Dump(multipleRegisters.ToPdu()))
            .IsEqualTo(TestHelpers.Dump(
                new Abstractions.WriteMultipleRegistersRequest(0, 2, new ushort[] { 10, 11 }).ToByteArray()));

        var coilRoundTrip = WriteCoilRequest.FromPdu(coil.ToPdu());
        await Assert.That(coilRoundTrip.Reference).IsEqualTo(0x00AC);
        await Assert.That(coilRoundTrip.CoilState).IsTrue();

        var registerRoundTrip = WriteRegisterRequest.FromPdu(register.ToPdu());
        await Assert.That(registerRoundTrip.Reference).IsEqualTo(1);
        await Assert.That(registerRoundTrip.Value).IsEqualTo(1000);

        var multipleCoilsRoundTrip = WriteMultipleCoilsRequest.FromPdu(multipleCoils.ToPdu());
        await Assert.That(multipleCoilsRoundTrip.Reference).IsEqualTo(0);
        await Assert.That(multipleCoilsRoundTrip.BitCount).IsEqualTo(8);
        await Assert.That(multipleCoilsRoundTrip.Coils).IsEqualTo(coils);

        var multipleRegistersRoundTrip = WriteMultipleRegistersRequest.FromPdu(multipleRegisters.ToPdu());
        await Assert.That(multipleRegistersRoundTrip.Reference).IsEqualTo(0);
        await Assert.That(multipleRegistersRoundTrip.WordCount).IsEqualTo(2);
        await Assert.That(multipleRegistersRoundTrip.Registers[1].Value).IsEqualTo(11);
    }

    [Test]
    public async Task WriteResponses_EncodeAndRoundTripAsContract()
    {
        var coil = new WriteCoilResponse(0x00AC, true);
        await Assert.That(TestHelpers.Dump(coil.ToPdu()))
            .IsEqualTo(TestHelpers.Dump(
                new Abstractions.WriteSingleResponse(ModbusFunctionCode.WriteCoil, 0x00AC, 0xFF00).ToByteArray()));

        var coilRoundTrip = WriteCoilResponse.FromPdu(coil.ToPdu());
        await Assert.That(coilRoundTrip.Reference).IsEqualTo(0x00AC);
        await Assert.That(coilRoundTrip.CoilState).IsTrue();

        var register = new WriteRegisterResponse(1, 1000);
        await Assert.That(TestHelpers.Dump(register.ToPdu()))
            .IsEqualTo(TestHelpers.Dump(
                new Abstractions.WriteSingleResponse(ModbusFunctionCode.WriteSingleRegister, 1, 1000).ToByteArray()));

        var registerRoundTrip = WriteRegisterResponse.FromPdu(register.ToPdu());
        await Assert.That(registerRoundTrip.Reference).IsEqualTo(1);
        await Assert.That(registerRoundTrip.Value).IsEqualTo(1000);

        var coils = new WriteMultipleCoilsResponse(0, 8);
        await Assert.That(TestHelpers.Dump(coils.ToPdu()))
            .IsEqualTo(TestHelpers.Dump(
                new Abstractions.WriteMultipleResponse(ModbusFunctionCode.WriteMultipleCoils, 0, 8).ToByteArray()));

        var coilsRoundTrip = WriteMultipleCoilsResponse.FromPdu(coils.ToPdu());
        await Assert.That(coilsRoundTrip.Reference).IsEqualTo(0);
        await Assert.That(coilsRoundTrip.BitCount).IsEqualTo(8);

        var registers = new WriteMultipleRegistersResponse(0, 2);
        await Assert.That(TestHelpers.Dump(registers.ToPdu()))
            .IsEqualTo(TestHelpers.Dump(
                new Abstractions.WriteMultipleResponse(ModbusFunctionCode.WriteMultipleRegisters, 0, 2).ToByteArray()));

        var registersRoundTrip = WriteMultipleRegistersResponse.FromPdu(registers.ToPdu());
        await Assert.That(registersRoundTrip.Reference).IsEqualTo(0);
        await Assert.That(registersRoundTrip.WordCount).IsEqualTo(2);
    }

    [Test]
    public async Task ExceptionResponse_UsesContractEncoding()
    {
        var response = new ExceptionResponse(
            ModbusFunctionCode.ReadMultipleRegisters,
            ModbusExceptionCode.IllegalDataAddress);

        await Assert.That(TestHelpers.Dump(response.ToPdu())).IsEqualTo("83 02");
        await Assert.That((int)response.FunctionCode).IsEqualTo(0x83);

        var parsed = (ExceptionResponse)ModbusResponseFactory.FromPdu(response.ToPdu());
        await Assert.That(parsed.OriginalFunctionCode).IsEqualTo(ModbusFunctionCode.ReadMultipleRegisters);
        await Assert.That(parsed.ExceptionCode).IsEqualTo(ModbusExceptionCode.IllegalDataAddress);
    }

    [Test]
    public async Task IllegalResponsePdu_ThrowsModbusIOExceptionWrappingSlaveException()
    {
        var registers = TestHelpers.ExpectThrows<ModbusIOException>(
            () => ReadMultipleRegistersResponse.FromPdu(TestHelpers.Hex("03 04 00 0A")));
        await Assert.That(registers.InnerException is Abstractions.ModbusSlaveException).IsTrue();
        await Assert.That(registers.InnerException).IsNotNull();

        var bits = TestHelpers.ExpectThrows<ModbusIOException>(
            () => ReadCoilsResponse.FromPdu(TestHelpers.Hex("01 03 CD 6B")));
        await Assert.That(bits.InnerException is Abstractions.ModbusSlaveException).IsTrue();
        await Assert.That(bits.InnerException).IsNotNull();

        var single = TestHelpers.ExpectThrows<ModbusIOException>(
            () => WriteCoilResponse.FromPdu(TestHelpers.Hex("05 00 AC")));
        await Assert.That(single.InnerException is Abstractions.ModbusSlaveException).IsTrue();
        await Assert.That(single.InnerException).IsNotNull();

        var slave = (Abstractions.ModbusSlaveException)registers.InnerException!;
        await Assert.That(slave.ExceptionCode).IsEqualTo(Abstractions.ModbusExceptionCode.IllegalDataValue);
    }

    [Test]
    public async Task ContractDecode_RejectsFunctionCodeMismatch()
    {
        var bits = TestHelpers.ExpectThrows<Abstractions.ModbusSlaveException>(
            () => Abstractions.ReadBitsResponse.Decode(
                ModbusFunctionCode.ReadInputDiscretes,
                TestHelpers.Hex("01 01 FF")));
        await Assert.That(bits.ExceptionCode).IsEqualTo(Abstractions.ModbusExceptionCode.IllegalDataValue);

        var registers = TestHelpers.ExpectThrows<Abstractions.ModbusSlaveException>(
            () => Abstractions.ReadRegistersResponse.Decode(
                ModbusFunctionCode.ReadInputRegisters,
                TestHelpers.Hex("03 02 00 0A")));
        await Assert.That(registers.ExceptionCode).IsEqualTo(Abstractions.ModbusExceptionCode.IllegalDataValue);

        var single = TestHelpers.ExpectThrows<Abstractions.ModbusSlaveException>(
            () => Abstractions.WriteSingleResponse.Decode(
                ModbusFunctionCode.WriteSingleRegister,
                TestHelpers.Hex("05 00 AC FF 00")));
        await Assert.That(single.ExceptionCode).IsEqualTo(Abstractions.ModbusExceptionCode.IllegalDataValue);
    }
}
