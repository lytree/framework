using System.Buffers;
using TUnit.Core;

namespace Framework.Tests.System.IO;

public class PooledMemoryStreamTests
{
    [Test]
    public async Task Constructor_Default_HasZeroLength()
    {
        using var ms = new PooledMemoryStream();

        await Assert.That(ms.Length).IsEqualTo(0);
        await Assert.That(ms.Capacity).IsEqualTo(0);
        await Assert.That(ms.Position).IsEqualTo(0);
    }

    [Test]
    public async Task Constructor_WithBuffer_CopiesContent()
    {
        var buffer = new byte[] { 1, 2, 3, 4, 5 };
        using var ms = new PooledMemoryStream(buffer);

        await Assert.That(ms.Length).IsEqualTo(5);
        await Assert.That(ms.Capacity).IsGreaterThanOrEqualTo(5);
        await Assert.That(ms.ToArray()).IsEquivalentTo(buffer);
    }

    [Test]
    public async Task Write_ThenRead_RoundTrips()
    {
        using var ms = new PooledMemoryStream();
        var payload = new byte[] { 0x10, 0x20, 0x30, 0x40 };

        ms.Write(payload, 0, payload.Length);
        ms.Position = 0;
        var read = new byte[4];
        var readBytes = ms.Read(read, 0, 4);

        await Assert.That(readBytes).IsEqualTo(4);
        await Assert.That(read).IsEquivalentTo(payload);
    }

    [Test]
    public async Task Capacity_GrowsWhenWritingBeyondCapacity()
    {
        using var ms = new PooledMemoryStream();

        // 写入大量数据触发扩容
        var payload = new byte[8192];
        Random.Shared.NextBytes(payload);
        ms.Write(payload, 0, payload.Length);

        await Assert.That(ms.Capacity).IsGreaterThan(0);
        await Assert.That(ms.Length).IsEqualTo(8192);
    }

    [Test]
    public async Task Seek_BeyondEnd_GrowsStream()
    {
        using var ms = new PooledMemoryStream();

        var pos = ms.Seek(100, SeekOrigin.End);

        await Assert.That(pos).IsEqualTo(100);
        await Assert.That(ms.Length).IsGreaterThanOrEqualTo(100);
    }

    [Test]
    public async Task Seek_NegativeOffset_Throws()
    {
        using var ms = new PooledMemoryStream();
        ms.Write(new byte[] { 1, 2, 3 }, 0, 3);

        await Assert.That(() => ms.Seek(-1, SeekOrigin.Begin)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task GetSpan_ExposesWrittenBytes()
    {
        using var ms = new PooledMemoryStream();
        ms.Write(new byte[] { 9, 8, 7, 6 }, 0, 4);

        var span = ms.GetSpan().ToArray();

        await Assert.That(span.Length).IsEqualTo(4);
        await Assert.That(span).IsEquivalentTo(new byte[] { 9, 8, 7, 6 });
    }

    [Test]
    public async Task Dispose_ReleasesBufferToPool()
    {
        // 用专用 ArrayPool，捕获 Rent/Return 调用
        var tracker = new TrackedArrayPool();
        var ms = new PooledMemoryStream(tracker, 64);
        ms.Dispose();

        await Assert.That(tracker.ReturnedCount).IsEqualTo(1);
    }

    [Test]
    public async Task DoubleDispose_DoesNotThrowOrReturnTwice()
    {
        var tracker = new TrackedArrayPool();
        var ms = new PooledMemoryStream(tracker, 64);

        ms.Dispose();
        ms.Dispose();

        await Assert.That(tracker.ReturnedCount).IsEqualTo(1);
    }

    [Test]
    public async Task DisposedStream_OperationsThrow()
    {
        var ms = new PooledMemoryStream();
        ms.Dispose();

        await Assert.That(() => ms.Write(new byte[1], 0, 1)).Throws<ObjectDisposedException>();
        await Assert.That(() => ms.Read(new byte[1], 0, 1)).Throws<ObjectDisposedException>();
        await Assert.That(() => ms.Seek(0, SeekOrigin.Begin)).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task Enumerator_IteratesAllBytes()
    {
        var payload = new byte[] { 1, 2, 3, 4, 5 };
        using var ms = new PooledMemoryStream(payload);

        var collected = new List<byte>();
        foreach (var b in ms)
            collected.Add(b);

        await Assert.That(collected).IsEquivalentTo(payload);
    }

    sealed class TrackedArrayPool : ArrayPool<byte>
    {
        public int ReturnedCount;

        public override byte[] Rent(int minimumLength) => new byte[Math.Max(minimumLength, 16)];

        public override void Return(byte[] array, bool clearArray = false) => Interlocked.Increment(ref ReturnedCount);
    }
}
