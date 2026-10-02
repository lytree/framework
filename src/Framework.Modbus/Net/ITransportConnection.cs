namespace Framework.Modbus.Net;

/// <summary>
/// 字节流连接抽象（TCP / UDP / 串口）。对应 jamod 的 <c>net.wimpi.modbus.net.*Connection</c>。
/// </summary>
/// <remarks>
/// <see cref="Read"/> 采用"截止时间"语义：在超时时间内没有数据可读时返回 0，
/// 而不是抛异常；调用方据此判断超时。
/// </remarks>
public interface ITransportConnection : IDisposable
{
    /// <summary>连接是否已建立。</summary>
    bool IsConnected { get; }

    /// <summary>读超时（毫秒）。</summary>
    int ReadTimeout { get; set; }

    /// <summary>写超时（毫秒）。</summary>
    int WriteTimeout { get; set; }

    /// <summary>当前可读字节数（串口为缓冲区字节数，网络流返回 0）。</summary>
    int Available { get; }

    /// <summary>建立连接（幂等）。</summary>
    void Connect();

    /// <summary>关闭连接（幂等）。</summary>
    void Close();

    /// <summary>读取若干字节，返回实际读取的字节数；超时或对端关闭时返回 0。</summary>
    int Read(byte[] buffer, int offset, int count);

    /// <summary>写入全部字节。</summary>
    void Write(byte[] buffer, int offset, int count);

    /// <summary>刷新发送缓冲区。</summary>
    void Flush();

    /// <summary>清空收发缓冲区。</summary>
    void DiscardBuffers();
}
