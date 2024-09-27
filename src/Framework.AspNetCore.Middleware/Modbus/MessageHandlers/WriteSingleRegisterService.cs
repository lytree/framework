using System.Linq;
using Net.Middleware.Modbus.Data;
using Net.Middleware.Modbus.Message;

namespace Net.Middleware.Modbus.MessageHandlers
{
    public class WriteSingleRegisterService : ModbusFunctionServiceBase<WriteSingleRegisterRequestResponse>
    {
        public WriteSingleRegisterService() 
            : base(ModbusFunctionCodes.WriteSingleRegister)
        {
        }

        public override IModbusMessage CreateRequest(byte[] frame)
        {
            return CreateModbusMessage<WriteSingleRegisterRequestResponse>(frame);
        }

        public override int GetRtuRequestBytesToRead(byte[] frameStart)
        {
            return 1;
        }

        public override int GetRtuResponseBytesToRead(byte[] frameStart)
        {
            return 4;
        }

        protected override IModbusMessage Handle(WriteSingleRegisterRequestResponse request, ISlaveDataStore dataStore)
        {
            ushort[] points = request.Data
                .ToArray();

            dataStore.HoldingRegisters.WritePoints(request.StartAddress, points);

            return request;
        }
    }
}