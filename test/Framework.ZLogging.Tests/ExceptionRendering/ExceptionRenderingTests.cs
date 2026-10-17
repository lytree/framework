using Framework.ZLogging;
using Framework.ZLogging.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using TUnit.Core;

namespace Framework.ZLogging.Tests.ExceptionRendering;

[ClassDataSource<TempDirectory>(Shared = SharedType.PerClass)]
public class ExceptionRenderingTests(TempDirectory temp)
{
    static Task<string> WriteAndRead(TempDirectory temp, Action<ILogger> log)
        => LogTestHelper.CaptureAsync(temp.NewFile(".log"), log);

    [Test]
    public async Task Log_WithException_WritesInnerExceptionChain()
    {
        var content = await WriteAndRead(temp, logger =>
        {
            var inner = new InvalidOperationException("inner");
            var outer = new ApplicationException("outer", inner);
            logger.LogError(outer, "boom");
        });

        await Assert.That(content).Contains("boom");
        await Assert.That(content).Contains("outer");
        await Assert.That(content).Contains("inner");
        await Assert.That(content).Contains("--- End of inner exception stack trace ---");
    }

    [Test]
    public async Task Log_WithException_WritesTypeNameAndStackTrace()
    {
        Exception? captured = null;
        var content = await WriteAndRead(temp, logger =>
        {
            try { throw new InvalidOperationException("thrown here"); }
            catch (Exception ex) { captured = ex; logger.LogError(ex, "msg"); }
        });

        await Assert.That(content).Contains("System.InvalidOperationException");
        await Assert.That(content).Contains("thrown here");
        await Assert.That(captured?.StackTrace).IsNotNull();
        // 堆栈必须完整落盘，否则事后无法定位问题。
        await Assert.That(content).Contains("ExceptionRenderingTests");
    }

    [Test]
    public async Task NoStackTrace_DoesNotThrow()
    {
        var content = await WriteAndRead(temp, logger =>
            logger.LogError(new Exception("no stack"), "msg"));

        await Assert.That(content).Contains("no stack");
    }

    [Test]
    public async Task AggregateException_IsRenderedWithoutLoss()
    {
        var content = await WriteAndRead(temp, logger =>
            logger.LogError(new AggregateException(new Exception("a"), new Exception("b")), "many"));

        await Assert.That(content).Contains("a");
        await Assert.That(content).Contains("b");
    }

    [Test]
    public async Task ExceptionOutput_DoesNotBreakPipeline()
    {
        var filePath = temp.NewFile(".log");
        var errors = new List<Exception>();
        var content = await LogTestHelper.CaptureAsync(filePath, logger =>
        {
            logger.LogError(new InvalidOperationException("x"), "boom");
            logger.LogInformation("after");
        }, options => options.InternalErrorLogger = errors.Add);

        await Assert.That(content).Contains("after");
        await Assert.That(errors).IsEmpty();
    }
}
