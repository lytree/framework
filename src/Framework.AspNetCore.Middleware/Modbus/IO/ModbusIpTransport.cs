using AspNetCore.Middleware.Modbus.Logging;
using System.Net;

namespace AspNetCore.Middleware.Modbus.IO;

/// <summary>
///     Transport for Internet protocols.
///     Refined Abstraction - http://en.wikipedia.org/wiki/Bridge_Pattern
/// </summary>
public class ModbusIpTransport : ModbusTransport
{
	private static readonly object _transactionIdLock = new object();
	private ushort _transactionId;

	public ModbusIpTransport(IStreamResource streamResource, IModbusFactory modbusFactory, ILoggerFactory loggerFactory)
		: base(streamResource, modbusFactory, loggerFactory)
	{
		if (streamResource == null) throw new ArgumentNullException(nameof(streamResource));
	}

	public static byte[] ReadRequestResponse(IStreamResource streamResource, ILogger<IModbusTransport> logger)
	{
		if (streamResource == null) throw new ArgumentNullException(nameof(streamResource));
		if (logger == null) throw new ArgumentNullException(nameof(logger));

		// read header
		var mbapHeader = new byte[6];
		int numBytesRead = 0;

		while (numBytesRead != 6)
		{
			int bRead = streamResource.Read(mbapHeader, numBytesRead, 6 - numBytesRead);

			if (bRead == 0)
			{
				throw new IOException("Read resulted in 0 bytes returned.");
			}

			numBytesRead += bRead;
		}

		logger.LogDebug($"MBAP header: {string.Join(", ", mbapHeader)}");
		var frameLength = (ushort)IPAddress.HostToNetworkOrder(BitConverter.ToInt16(mbapHeader, 4));
		logger.LogDebug($"{frameLength} bytes in PDU.");

		// read message
		var messageFrame = new byte[frameLength];
		numBytesRead = 0;

		while (numBytesRead != frameLength)
		{
			int bRead = streamResource.Read(messageFrame, numBytesRead, frameLength - numBytesRead);

			if (bRead == 0)
			{
				throw new IOException("Read resulted in 0 bytes returned.");
			}

			numBytesRead += bRead;
		}

		logger.LogDebug($"PDU: {frameLength}");
		var frame = mbapHeader.Concat(messageFrame).ToArray();
		logger.LogDebug(frame.ToBase64String());

		return frame;
	}

	public static byte[] GetMbapHeader(IModbusMessage message)
	{
		byte[] transactionId = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)message.TransactionId));
		byte[] length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)(message.ProtocolDataUnit.Length + 1)));

		var stream = new MemoryStream(7);
		stream.Write(transactionId, 0, transactionId.Length);
		stream.WriteByte(0);
		stream.WriteByte(0);
		stream.Write(length, 0, length.Length);
		stream.WriteByte(message.SlaveAddress);

		return stream.ToArray();
	}

	/// <summary>
	///     Create a new transaction ID.
	/// </summary>
	public virtual ushort GetNewTransactionId()
	{
		lock (_transactionIdLock)
		{
			_transactionId = _transactionId == ushort.MaxValue ? (ushort)1 : ++_transactionId;
		}

		return _transactionId;
	}

	public IModbusMessage CreateMessageAndInitializeTransactionId<T>(byte[] fullFrame)
		where T : IModbusMessage, new()
	{
		byte[] mbapHeader = fullFrame[0.. 6];
		byte[] messageFrame = fullFrame[6.. (fullFrame.Length - 6)];

		IModbusMessage response = CreateResponse<T>(messageFrame);
		response.TransactionId = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(mbapHeader, 0));

		return response;
	}

	public override byte[] BuildMessageFrame(IModbusMessage message)
	{
		byte[] header = GetMbapHeader(message);
		byte[] pdu = message.ProtocolDataUnit;
		var messageBody = new MemoryStream(header.Length + pdu.Length);

		messageBody.Write(header, 0, header.Length);
		messageBody.Write(pdu, 0, pdu.Length);

		return messageBody.ToArray();
	}

	public override void Write(IModbusMessage message)
	{
		message.TransactionId = GetNewTransactionId();
		byte[] frame = BuildMessageFrame(message);

		Logger.LogFrameTx(frame);

		StreamResource.Write(frame, 0, frame.Length);
	}

	public override byte[] ReadRequest()
	{
		return ReadRequestResponse(StreamResource, Logger);
	}

	public override IModbusMessage ReadResponse<T>()
	{
		return CreateMessageAndInitializeTransactionId<T>(ReadRequestResponse(StreamResource, Logger));
	}

	internal override void OnValidateResponse(IModbusMessage request, IModbusMessage response)
	{
		if (request.TransactionId != response.TransactionId)
		{
			string msg = $"Response was not of expected transaction ID. Expected {request.TransactionId}, received {response.TransactionId}.";
			throw new IOException(msg);
		}
	}

	public override bool OnShouldRetryResponse(IModbusMessage request, IModbusMessage response)
	{
		if (request.TransactionId > response.TransactionId && request.TransactionId - response.TransactionId < RetryOnOldResponseThreshold)
		{
			// This response was from a previous request
			return true;
		}

		return base.OnShouldRetryResponse(request, response);
	}
}