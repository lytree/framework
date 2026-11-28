using System.Text.Json;
using Framework.ZLogging;
using Framework.ZLogging.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using TUnit.Core;

namespace Framework.ZLogging.Tests.FileIO;

/// <summary>
/// JSON Lines 文件输出：每条日志一行 JSON，字段可被日志系统直接检索。
/// </summary>
[ClassDataSource<TempDirectory>(Shared = SharedType.PerClass)]
public class JsonFileOutputTests(TempDirectory temp)
{
    static Task<string> WriteJsonAsync(TempDirectory temp, Action<ILogger> log,
        Action<SpectreLogOptions>? configure = null)
        => LogTestHelper.CaptureAsync(temp.NewFile(".log"), log, options =>
        {
            options.FileFormat = SpectreLogFileFormat.Json;
            configure?.Invoke(options);
        });

    [Test]
    public async Task EachEntry_ProducesSingleParseableJsonLine()
    {
        var content = await WriteJsonAsync(temp, logger =>
        {
            logger.LogInformation("first {Index}", 1);
            logger.LogWarning("second {Index}", 2);
        });

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        await Assert.That(lines.Length).IsEqualTo(2);

        // 一行必须是一个完整 JSON 对象；缩进或内嵌换行都会破坏 JSON Lines 约定。
        foreach (var line in lines)
        {
            using var document = JsonDocument.Parse(line);
            await Assert.That(document.RootElement.ValueKind).IsEqualTo(JsonValueKind.Object);
        }
    }

    [Test]
    public async Task CoreFields_ArePresent()
    {
        var content = await WriteJsonAsync(temp,
            logger => logger.LogInformation(new EventId(42, "OrderPlaced"), "hello {Name}", "world"),
            options => options.IncludeScopes = false);

        using var document = JsonDocument.Parse(content.Trim());
        var root = document.RootElement;

        await Assert.That(root.GetProperty("LogLevel").GetString()).IsEqualTo("Information");
        await Assert.That(root.GetProperty("Category").GetString()).IsEqualTo("Test");
        await Assert.That(root.GetProperty("Message").GetString()).IsEqualTo("hello world");
        await Assert.That(root.GetProperty("MessageTemplate").GetString()).IsEqualTo("hello {Name}");
        await Assert.That(root.GetProperty("EventId").GetInt32()).IsEqualTo(42);
        await Assert.That(root.GetProperty("EventIdName").GetString()).IsEqualTo("OrderPlaced");
        // 时间戳必须是可被解析的 ISO 8601。
        var timestamp = root.GetProperty("Timestamp").GetString();
        await Assert.That(DateTimeOffset.TryParse(timestamp, out _)).IsTrue();
    }

    [Test]
    public async Task StructuredProperties_AreWrittenByName()
    {
        var content = await WriteJsonAsync(temp,
            logger => logger.LogInformation("user {User} age {Age}", "Alice", 30));

        using var document = JsonDocument.Parse(content.Trim());
        var properties = document.RootElement.GetProperty("Properties");

        await Assert.That(properties.GetProperty("User").GetString()).IsEqualTo("Alice");
        await Assert.That(properties.GetProperty("Age").GetInt32()).IsEqualTo(30);
    }

    [Test]
    public async Task Scopes_AreIncludedWhenEnabled()
    {
        var filePath = temp.NewFile(".log");
        var options = new SpectreLogOptions
        {
            FilePath = filePath,
            FileAppend = false,
            ConsoleOutput = false,
            IncludeScopes = true,
            FileFormat = SpectreLogFileFormat.Json
        };

        // 作用域由 LoggerFactory 注入，因此这条用例走工厂而非直接构造 Provider。
        using (var factory = LoggerFactory.Create(builder => builder.AddSpectre(options)))
        {
            var logger = factory.CreateLogger("Test");
            using (logger.BeginScope("request-1"))
                logger.LogInformation("scoped");
        }

        using var document = JsonDocument.Parse((await File.ReadAllTextAsync(filePath)).Trim());
        await Assert.That(document.RootElement.GetProperty("Scopes").GetString()).Contains("request-1");
    }

    [Test]
    public async Task Exception_IsSerializedAsObject()
    {
        var content = await WriteJsonAsync(temp, logger =>
        {
            try { throw new InvalidOperationException("boom"); }
            catch (Exception exception) { logger.LogError(exception, "failed {Code}", 500); }
        });

        using var document = JsonDocument.Parse(content.Trim());
        var exception = document.RootElement.GetProperty("Exception");

        await Assert.That(exception.GetProperty("Type").GetString()).Contains("InvalidOperationException");
        await Assert.That(exception.GetProperty("Message").GetString()).IsEqualTo("boom");
        await Assert.That(exception.GetProperty("StackTrace").GetString()).IsNotNull();
    }

    [Test]
    public async Task TextFormat_RemainsDefault()
    {
        var filePath = temp.NewFile(".log");
        var content = await LogTestHelper.CaptureAsync(filePath, logger => logger.LogInformation("plain {Name}", "text"));

        // 默认行为不变：落盘必须仍是人类可读的纯文本，而不是 JSON。
        await Assert.That(content).Contains("[INFO]");
        await Assert.That(content).Contains("plain text");
        await Assert.That(content.TrimStart().StartsWith('{')).IsFalse();
    }

    [Test]
    public async Task UnserializableValue_FallsBackWithoutBreakingLine()
    {
        var content = await WriteJsonAsync(temp,
            logger => logger.LogInformation("value {Value}", new ThrowingValue()));

        // 属性序列化失败应降级为 ToString，且整行 JSON 仍然合法。
        using var document = JsonDocument.Parse(content.Trim());
        await Assert.That(document.RootElement.GetProperty("Properties").GetProperty("Value").GetString())
            .IsEqualTo("throwing");
    }

    sealed class ThrowingValue
    {
        public string Boom => throw new InvalidOperationException("getter failed");

        public override string ToString() => "throwing";
    }
}
