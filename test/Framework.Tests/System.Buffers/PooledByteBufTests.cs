using System.Buffers;
using TUnit.Core;

namespace Framework.Tests.Shared.Buffers;

/// <summary>
/// <see cref="PooledByteBuf"/> 的 IBufferWriter 契约、池化归属与释放安全测试。
/// </summary>
public class PooledByteBufTests
{
    /// <summary>记录租借与归还次数的内存池，用于验证"从哪个池借、还回哪个池"。</summary>
    private sealed class TrackingPool : ArrayPool<byte>
    {
        public int RentCount { get; private set; }

        public int ReturnCount { get; private set; }

        public override byte[] Rent(int minimumLength)
        {
            RentCount++;
            return new byte[Math.Max(minimumLength, 1)];
        }

        public override void Return(byte[] array, bool clearArray = false)
        {
            ReturnCount++;
        }
    }

    [Test]
    public async Task GetSpan_ThenAdvance_ExposesWrittenBytes()
    {
        using var buf = new PooledByteBuf(8);

        var span = buf.GetSpan(3);
        span[0] = 0xAA;
        span[1] = 0xBB;
        span[2] = 0xCC;
        buf.Advance(3);

        await Assert.That(buf.WriterIndex).IsEqualTo(3);
        await Assert.That(buf.WrittenSpan().ToArray()).IsEquivalentTo(new byte[] { 0xAA, 0xBB, 0xCC });
        await Assert.That(buf.ReadableBytes).IsEqualTo(3);
    }

    [Test]
    public async Task GetSpan_SizeHintZero_AlwaysReturnsAtLeastOneByte()
    {
        using var buf = new PooledByteBuf(4);

        // 写满整块容量，此时剩余可写空间为 0
        var full = buf.GetSpan(buf.WritableBytes);
        full.Fill(0x01);
        buf.Advance(buf.WritableBytes);

        // IBufferWriter 约定：sizeHint 为 0 时仍必须能写，否则调用方会陷入死循环
        var span = buf.GetSpan();

        await Assert.That(span.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task GetSpan_NegativeSizeHint_Throws()
    {
        using var buf = new PooledByteBuf(8);

        await Assert.That(() => { _ = buf.GetSpan(-1); }).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Advance_BeyondWritableSpace_Throws()
    {
        using var buf = new PooledByteBuf(8);
        var free = buf.WritableBytes;

        await Assert.That(() => buf.Advance(free + 1)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => buf.Advance(-1)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task WriteBeyondCapacity_GrowsAndKeepsContent()
    {
        using var buf = new PooledByteBuf(4);
        var payload = new byte[64];
        Random.Shared.NextBytes(payload);

        buf.WriteBytes(payload);

        await Assert.That(buf.ReadableBytes).IsEqualTo(64);
        await Assert.That(buf.ToArray()).IsEquivalentTo(payload);
        await Assert.That(buf.Capacity).IsGreaterThanOrEqualTo(64);
    }

    [Test]
    public async Task Dispose_ThenAccess_ThrowsObjectDisposed()
    {
        var buf = new PooledByteBuf(16);
        buf.WriteByte(1);
        buf.Dispose();

        await Assert.That(() => { _ = buf.ReadSpan(); }).Throws<ObjectDisposedException>();
        await Assert.That(() => { _ = buf.WrittenSpan(); }).Throws<ObjectDisposedException>();
        await Assert.That(() => { _ = buf.ToArray(); }).Throws<ObjectDisposedException>();
        await Assert.That(() => { _ = buf.ToFullArray(); }).Throws<ObjectDisposedException>();
        await Assert.That(() => { _ = buf.Clear(); }).Throws<ObjectDisposedException>();
        await Assert.That(() => { _ = buf.MarkReaderIndex(); }).Throws<ObjectDisposedException>();
        await Assert.That(() => { _ = buf.WriteByte(1); }).Throws<ObjectDisposedException>();
        await Assert.That(() => { _ = buf.GetSpan(); }).Throws<ObjectDisposedException>();
        await Assert.That(() => buf.Advance(0)).Throws<ObjectDisposedException>();    }

    [Test]
    public async Task Dispose_ReturnsArrayToInjectedPool()
    {
        var pool = new TrackingPool();
        var buf = new PooledByteBuf(pool, 16);
        buf.WriteBytes(new byte[32]);

        var rentAfterGrowth = pool.RentCount;
        buf.Dispose();

        await Assert.That(rentAfterGrowth).IsGreaterThanOrEqualTo(2);
        // 扩容时归还过一次旧数组，Dispose 再归还当前数组
        await Assert.That(pool.ReturnCount).IsEqualTo(rentAfterGrowth);
    }

    [Test]
    public async Task DoubleDispose_ReturnsOnce()
    {
        var pool = new TrackingPool();
        var buf = new PooledByteBuf(pool, 16);

        buf.Dispose();
        buf.Dispose();
        buf.Dispose();

        await Assert.That(pool.ReturnCount).IsEqualTo(1);
    }

    [Test]
    public async Task DiscardReadBytes_AfterGrowth_KeepsUnreadContent()
    {
        using var buf = new PooledByteBuf(8);
        buf.WriteBytes(new byte[] { 1, 2, 3, 4, 5, 6 });
        buf.SkipBytes(4);

        buf.DiscardReadBytes();

        await Assert.That(buf.ReaderIndex).IsEqualTo(0);
        await Assert.That(buf.ReadableBytes).IsEqualTo(2);
        await Assert.That(buf.ToArray()).IsEquivalentTo(new byte[] { 5, 6 });
    }

    [Test]
    public async Task MarkAndResetReaderIndex_AllowsPeekThenParse()
    {
        using var buf = new PooledByteBuf(8);
        buf.WriteBytes(new byte[] { 0x01, 0x02, 0x03 });

        buf.MarkReaderIndex();
        var peeked = buf.ReadByte();
        buf.ResetReaderIndex();

        await Assert.That(peeked).IsEqualTo((byte)0x01);
        await Assert.That(buf.ReaderIndex).IsEqualTo(0);
        await Assert.That(buf.ReadableBytes).IsEqualTo(3);
    }
}
