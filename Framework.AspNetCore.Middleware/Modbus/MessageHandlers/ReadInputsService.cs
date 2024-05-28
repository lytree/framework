using Net.Middleware.Modbus.Data;
using Net.Middleware.Modbus.Message;

namespace Net.Middleware.Modbus.MessageHandlers
{
    public class ReadInputsService : ModbusFunctionServiceBase<ReadCoilsInputsRequest>
    {
        public ReadInputsService() 
            : base(ModbusFunctionCodes.ReadInputs)
        {
        }

        public override IModbusMessage CreateRequest(byte[] frame)
        {
            return CreateModbusMessage<ReadCoilsInputsRequest>(frame);
        }

        public override int GetRtuRequestBytesToRead(byte[] frameStart)
        {
            return 1;
        }

        public override int GetRtuResponseBytesToRead(byte[] frameStart)
        {
            return frameStart[2] + 1;
        }

        protected override IModbusMessage Handle(ReadCoilsInputsRequest request, ISlaveDataStore dataStore)
        {
            bool[] discretes = dataStore.CoilInputs.ReadPoints(request.StartAddress, request.NumberOfPoints);

            DiscreteCollection data = new DiscreteCollection(discretes);

            return new ReadCoilsInputsResponse(
                request.FunctionCode,
                request.SlaveAddress,
                data.ByteCount,
                data);
        }
    }
}