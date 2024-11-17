using System;
using System.Linq;
using Net.Middleware.Modbus.Data;
using Net.Middleware.Modbus.Message;

namespace Net.Middleware.Modbus.MessageHandlers
{
    public class ReadWriteMultipleRegistersService : ModbusFunctionServiceBase<ReadWriteMultipleRegistersRequest>
    {
        public ReadWriteMultipleRegistersService() 
            : base(ModbusFunctionCodes.ReadWriteMultipleRegisters)
        {
        }

        public override IModbusMessage CreateRequest(byte[] frame)
        {
            return CreateModbusMessage<ReadWriteMultipleRegistersRequest>(frame);
        }

        public override int GetRtuRequestBytesToRead(byte[] frameStart)
        {
            throw new NotSupportedException();
        }

        public override int GetRtuResponseBytesToRead(byte[] frameStart)
        {
            throw new NotSupportedException();
        }

        protected override IModbusMessage Handle(ReadWriteMultipleRegistersRequest request, ISlaveDataStore dataStore)
        {
            ushort[] pointsToWrite = request.WriteRequest.Data
                .ToArray();

            dataStore.HoldingRegisters.WritePoints(request.WriteRequest.StartAddress, pointsToWrite);

            ushort[] readPoints = dataStore.HoldingRegisters.ReadPoints(request.ReadRequest.StartAddress,
                request.ReadRequest.NumberOfPoints);

            RegisterCollection data = new RegisterCollection(readPoints);

            return new ReadHoldingInputRegistersResponse(
                request.FunctionCode,
                request.SlaveAddress,
                data);
        }
    }
}