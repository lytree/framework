using System.Linq;
using Net.Middleware.Modbus.Data;
using Net.Middleware.Modbus.Message;

namespace Net.Middleware.Modbus.MessageHandlers
{
    public class WriteMultipleCoilsService : ModbusFunctionServiceBase<WriteMultipleCoilsRequest>
    {
        public WriteMultipleCoilsService() 
            : base(ModbusFunctionCodes.WriteMultipleCoils)
        {
        }

        public override IModbusMessage CreateRequest(byte[] frame)
        {
            return CreateModbusMessage<WriteMultipleCoilsRequest>(frame);
        }

        public override int GetRtuRequestBytesToRead(byte[] frameStart)
        {
            return frameStart[6] + 2;
        }

        public override int GetRtuResponseBytesToRead(byte[] frameStart)
        {
            return 4;
        }

        protected override IModbusMessage Handle(WriteMultipleCoilsRequest request, ISlaveDataStore dataStore)
        {
            bool[] points = request.Data
                .Take(request.NumberOfPoints)
                .ToArray();

            dataStore.CoilDiscretes.WritePoints(request.StartAddress, points);

            return new WriteMultipleCoilsResponse(
               request.SlaveAddress,
               request.StartAddress,
               request.NumberOfPoints);
        }
    }
}