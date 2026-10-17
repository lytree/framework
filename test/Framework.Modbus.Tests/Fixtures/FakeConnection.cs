using System.Text;
using Framework.Modbus.Net;
using Framework.Modbus.Util;

namespace Framework.Modbus.Tests.Fixtures;

/// <summary>
/// 内存字节流连接：把预先灌入的字节当作"从站响应"，并记录主机写入的字节。
/// 用于在没有物理串口的情况下测试 RTU / ASCII 组帧。
/// </summary>
internal sealed class FakeConnection : ITransportConnection
{
    private readonly Queue<byte> _incoming = new();

    /// <summary>主机写入的所有字节。</summary>
    public List<byte> Written { get; } = new();

    /// <summary>
    /// 从站应答回调：收到请求帧后返回响应帧，由 <see cref="Write"/> 自动灌入接收队列。
    /// 这样测试无需关心"先发送还是先喂数据"的顺序。
    /// </summary>
    public Func<byte[], byte[]>? Responder { get; set; }

    /// <summary>最近一次收到的请求帧。</summary>
    public byte[]? LastRequestFrame { get; private set; }

    /// <inheritdoc />
    public bool IsConnected { get; private set; }

    /// <inheritdoc />
    public int ReadTimeout { get; set; } = 200;

    /// <inheritdoc />
    public int WriteTimeout { get; set; } = 200;

    /// <inheritdoc />
    public int Available => _incoming.Count;

    /// <summary>主机写入的字节数组。</summary>
    public byte[] WrittenBytes => Written.ToArray();

    /// <summary>主机写入字节的十六进制表示（空格分隔）。</summary>
    public string WrittenHex => ModbusUtil.BytesToHex(Written.ToArray(), upperCase: true, separator: " ");

    /// <summary>主机写入字节的 ASCII 表示。</summary>
    public string WrittenAscii => Encoding.ASCII.GetString(WrittenBytes);

    /// <summary>灌入二进制响应字节。</summary>
    public void Feed(byte[] data)
    {
        foreach (var b in data)
        {
            _incoming.Enqueue(b);
        }
    }

    /// <summary>灌入十六进制响应字节。</summary>
    public void Feed(string hex) => Feed(ModbusUtil.HexToBytes(hex));

    /// <summary>灌入 ASCII 响应文本（自动附加 CRLF）。</summary>
    public void FeedAscii(string text) => Feed(Encoding.ASCII.GetBytes(text));

    /// <inheritdoc />
    public void Connect() => IsConnected = true;

    /// <inheritdoc />
    public void Close() => IsConnected = false;

    /// <inheritdoc />
    public int Read(byte[] buffer, int offset, int count)
    {
        int read = 0;
        while (read < count && _incoming.Count > 0)
        {
            buffer[offset + read] = _incoming.Dequeue();
            read++;
        }

        if (read == 0)
        {
            // 模拟阻塞式读取，避免上层 ReadFully 空转。
            Thread.Sleep(1);
        }

        return read;
    }

    /// <inheritdoc />
    public void Write(byte[] buffer, int offset, int count)
    {
        var frame = new byte[count];
        Array.Copy(buffer, offset, frame, 0, count);
        LastRequestFrame = frame;

        for (int i = 0; i < count; i++)
        {
            Written.Add(buffer[offset + i]);
        }

        if (Responder is not null)
        {
            Feed(Responder(frame));
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
    }

    /// <inheritdoc />
    public void DiscardBuffers() => _incoming.Clear();

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
