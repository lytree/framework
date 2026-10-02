using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Framework.Modbus.Messages;
using Framework.Modbus.Util;

namespace Framework.Modbus.Tests.Fixtures;

/// <summary>
/// 极简的 Modbus TCP 从站，仅用于集成测试：
/// 支持 FC01 / FC02 / FC03 / FC04 / FC05 / FC06 / FC0F / FC10，其余功能码返回"非法功能"。
/// </summary>
internal sealed class LoopbackModbusSlave : IDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _acceptLoop;
    private int _requestCount;

    public LoopbackModbusSlave()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _acceptLoop = Task.Run(AcceptLoopAsync);
    }

    /// <summary>监听端口。</summary>
    public int Port { get; }

    /// <summary>已收到的请求数量。</summary>
    public int RequestCount => Volatile.Read(ref _requestCount);

    /// <summary>保持寄存器数据区。</summary>
    public ConcurrentDictionary<int, int> HoldingRegisters { get; } = new();

    /// <summary>输入寄存器数据区。</summary>
    public ConcurrentDictionary<int, int> InputRegisters { get; } = new();

    /// <summary>线圈数据区。</summary>
    public ConcurrentDictionary<int, bool> Coils { get; } = new();

    /// <summary>非空时对所有请求返回该异常码。</summary>
    public byte? ForceExceptionCode { get; set; }

    /// <summary>前 N 个请求只读取、不响应（用于测试重试）。</summary>
    public int DropFirstRequests { get; set; }

    /// <inheritdoc />
    public void Dispose()
    {
        _cts.Cancel();

        try
        {
            _listener.Stop();
        }
        catch (SocketException)
        {
            // 已停止。
        }

        try
        {
            _acceptLoop.Wait(TimeSpan.FromSeconds(3));
        }
        catch (AggregateException)
        {
            // 取消导致的异常可以忽略。
        }

        _cts.Dispose();
    }

    private async Task AcceptLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (SocketException)
            {
                return;
            }

            _ = Task.Run(() => HandleClientAsync(client));
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        {
            var stream = client.GetStream();
            var header = new byte[ModbusConstants.MbapHeaderSize];

            while (!_cts.IsCancellationRequested)
            {
                if (!await ReadExactAsync(stream, header, header.Length))
                {
                    return;
                }

                int length = (header[4] << 8) | header[5];
                int unitId = header[6];
                if (length < 2)
                {
                    return;
                }

                var pdu = new byte[length - 1];
                if (!await ReadExactAsync(stream, pdu, pdu.Length))
                {
                    return;
                }

                int count = Interlocked.Increment(ref _requestCount);
                if (count <= DropFirstRequests)
                {
                    continue;
                }

                var responsePdu = BuildResponse(pdu);
                var frame = new byte[ModbusConstants.MbapHeaderSize + responsePdu.Length];
                frame[0] = header[0];
                frame[1] = header[1];
                frame[2] = 0;
                frame[3] = 0;
                int responseLength = responsePdu.Length + 1;
                frame[4] = (byte)((responseLength >> 8) & 0xFF);
                frame[5] = (byte)(responseLength & 0xFF);
                frame[6] = (byte)unitId;
                Array.Copy(responsePdu, 0, frame, ModbusConstants.MbapHeaderSize, responsePdu.Length);

                await stream.WriteAsync(frame, _cts.Token);
                await stream.FlushAsync(_cts.Token);
            }
        }
    }

    private byte[] BuildResponse(byte[] pdu)
    {
        byte functionCode = pdu[0];

        if (ForceExceptionCode is byte exceptionCode)
        {
            return new[] { (byte)(functionCode | ModbusFunctionCode.ExceptionMask), exceptionCode };
        }

        switch (functionCode)
        {
            case ModbusFunctionCode.ReadCoils:
            {
                var (reference, count) = ReadReferenceAndCount(pdu);
                var bits = new BitVector(count);
                for (int i = 0; i < count; i++)
                {
                    bits.SetBit(i, Coils.GetValueOrDefault(reference + i));
                }

                return new ReadCoilsResponse(bits).ToPdu();
            }

            case ModbusFunctionCode.ReadInputDiscretes:
            {
                var (reference, count) = ReadReferenceAndCount(pdu);
                var bits = new BitVector(count);
                for (int i = 0; i < count; i++)
                {
                    bits.SetBit(i, Coils.GetValueOrDefault(reference + i));
                }

                return new ReadInputDiscretesResponse(bits).ToPdu();
            }

            case ModbusFunctionCode.ReadMultipleRegisters:
            {
                var (reference, count) = ReadReferenceAndCount(pdu);
                var values = new int[count];
                for (int i = 0; i < count; i++)
                {
                    values[i] = HoldingRegisters.GetValueOrDefault(reference + i);
                }

                return new ReadMultipleRegistersResponse(values).ToPdu();
            }

            case ModbusFunctionCode.ReadInputRegisters:
            {
                var (reference, count) = ReadReferenceAndCount(pdu);
                var values = new int[count];
                for (int i = 0; i < count; i++)
                {
                    values[i] = InputRegisters.GetValueOrDefault(reference + i);
                }

                return new ReadInputRegistersResponse(values).ToPdu();
            }

            case ModbusFunctionCode.WriteCoil:
            {
                var reader = new ModbusPduReader(pdu, 1);
                int reference = reader.ReadUInt16();
                bool state = reader.ReadUInt16() == 0xFF00;
                Coils[reference] = state;
                return new WriteCoilResponse(reference, state).ToPdu();
            }

            case ModbusFunctionCode.WriteSingleRegister:
            {
                var reader = new ModbusPduReader(pdu, 1);
                int reference = reader.ReadUInt16();
                int value = reader.ReadUInt16();
                HoldingRegisters[reference] = value;
                return new WriteRegisterResponse(reference, value).ToPdu();
            }

            case ModbusFunctionCode.WriteMultipleCoils:
            {
                var reader = new ModbusPduReader(pdu, 1);
                int reference = reader.ReadUInt16();
                int count = reader.ReadUInt16();
                int byteCount = reader.ReadByte();
                var bits = new BitVector(reader.ReadBytes(byteCount), count);
                for (int i = 0; i < count; i++)
                {
                    Coils[reference + i] = bits.GetBit(i);
                }

                return new WriteMultipleCoilsResponse(reference, count).ToPdu();
            }

            case ModbusFunctionCode.WriteMultipleRegisters:
            {
                var reader = new ModbusPduReader(pdu, 1);
                int reference = reader.ReadUInt16();
                int count = reader.ReadUInt16();
                int byteCount = reader.ReadByte();
                var data = reader.ReadBytes(byteCount);
                for (int i = 0; i < count; i++)
                {
                    HoldingRegisters[reference + i] = ((data[i * 2] & 0xFF) << 8) | (data[(i * 2) + 1] & 0xFF);
                }

                return new WriteMultipleRegistersResponse(reference, count).ToPdu();
            }

            default:
                return new[]
                {
                    (byte)(functionCode | ModbusFunctionCode.ExceptionMask),
                    ModbusExceptionCode.IllegalFunction,
                };
        }
    }

    private static (int Reference, int Count) ReadReferenceAndCount(byte[] pdu)
    {
        var reader = new ModbusPduReader(pdu, 1);
        return (reader.ReadUInt16(), reader.ReadUInt16());
    }

    private static async Task<bool> ReadExactAsync(NetworkStream stream, byte[] buffer, int count)
    {
        int total = 0;
        while (total < count)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(total, count - total));
            if (read <= 0)
            {
                return false;
            }

            total += read;
        }

        return true;
    }
}
