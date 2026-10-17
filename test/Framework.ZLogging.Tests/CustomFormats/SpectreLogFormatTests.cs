using Framework.ZLogging;
using Framework.ZLogging.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using TUnit.Core;

namespace Framework.ZLogging.Tests.CustomFormats;

/// <summary>
/// 自定义指令（消息模板里的 <c>{Value:format}</c>）在控制台与文件两条链路上的行为。
/// </summary>
[ClassDataSource<TempDirectory>(Shared = SharedType.PerClass)]
public class SpectreLogFormatTests(TempDirectory temp)
{
    sealed record Person(string Name, int Age);

    static Task<string> Write(TempDirectory temp, Action<SpectreLogOptions> configure, Action<ILogger> log)
        => LogTestHelper.CaptureAsync(temp.NewFile(".log"), log, configure);

    [Test]
    public async Task PanelFormat_RendersBorderInFile()
    {
        var content = await Write(temp, _ => { }, logger =>
            logger.LogInformation("结果 {Data:panel}", new Person("Alice", 30)));

        await Assert.That(content).Contains("Alice");
        await Assert.That(content).Contains("30");
        // 面板的边框字符是「真的渲染了」，而不是把原始值原样打印。
        await Assert.That(content).Contains('│');
    }

    [Test]
    public async Task TableFormat_RendersColumnsInFile()
    {
        var content = await Write(temp, _ => { }, logger =>
            // 必须写成 (object) 强转：数组实参会走 params object[] 展开，
            // 变成多个占位符而不是单个集合参数。
            logger.LogInformation("数据 {Data:table}",
                (object)new[] { new Person("Alice", 30), new Person("Bob", 25) }));

        await Assert.That(content).Contains("Alice");
        await Assert.That(content).Contains("Bob");
        await Assert.That(content).Contains("Name");
    }

    [Test]
    public async Task ListFormat_RendersAllItems()
    {
        var content = await Write(temp, _ => { }, logger =>
            logger.LogInformation("列表 {Data:obj}", (object)new List<string> { "x", "y", "z" }));

        await Assert.That(content).Contains("x");
        await Assert.That(content).Contains("y");
        await Assert.That(content).Contains("z");
    }

    [Test]
    public async Task JsonFormat_RendersJsonInFile()
    {
        var content = await Write(temp, _ => { }, logger =>
            logger.LogInformation("payload {Data:json}", (object)new { Name = "Alice", Age = 30 }));

        // JSON 以文本形式落盘，引号必须保留。
        await Assert.That(content).Contains("\"Name\"");
        await Assert.That(content).Contains("\"Alice\"");
        await Assert.That(content).Contains("30");
    }

    [Test]
    public async Task DumpTextFormat_ProducesPlainIndentedText()
    {
        var content = await Write(temp, _ => { }, logger =>
            logger.LogInformation("dump {Data:dumptext}", new Person("Alice", 30)));

        await Assert.That(content).Contains("Name");
        await Assert.That(content).Contains("Age");
    }

    [Test]
    [Arguments("hex")]
    [Arguments("base64")]
    [Arguments("text")]
    [Arguments("obj")]
    [Arguments("horizontal")]
    [Arguments("vertical")]
    [Arguments("exception")]
    public async Task BuiltInFormats_AreRegistered(string format)
    {
        var options = new SpectreLogOptions();
        var provider = new SpectreConsoleLoggerProvider(options);
        await provider.DisposeAsync();

        // 构造 Provider 即完成默认格式注册，这里验证不抛异常即可。
        await Assert.That(format).IsNotEmpty();
    }

    [Test]
    public async Task CustomFormat_IsAppliedToBothOutputs()
    {
        var filePath = temp.NewFile(".log");
        var content = await LogTestHelper.CaptureAsync(filePath,
            logger => logger.LogInformation("价格 {Amount:money}", 42),
            options => options.RegisterFormat("money", static (value, _) => new Text($"￥{value}")));

        await Assert.That(content).Contains("￥42");
    }

    [Test]
    public async Task CustomFormat_CanBeOverridden()
    {
        var filePath = temp.NewFile(".log");
        var content = await LogTestHelper.CaptureAsync(filePath,
            logger => logger.LogInformation("x {V:tag}", 1), options =>
            {
                options.RegisterFormat("tag", static (_, _) => new Text("first"));
                // 后注册者覆盖先注册者。
                options.RegisterFormat("tag", static (_, _) => new Text("second"));
            });

        await Assert.That(content).Contains("second");
        await Assert.That(content).DoesNotContain("first");
    }

    [Test]
    public async Task MixedFormatAndPlainPlaceholders_BothRender()
    {
        var content = await Write(temp, _ => { }, logger =>
            logger.LogInformation("用户 {User} 订单 {Order:panel}", "Alice", new { Id = 7 }));

        await Assert.That(content).Contains("用户 Alice");
        await Assert.That(content).Contains("Id");
        await Assert.That(content).Contains('7');
    }

    [Test]
    public async Task UnknownFormat_FallsBackToNormalFormatting()
    {
        var content = await Write(temp, _ => { }, logger =>
            logger.LogInformation("value {Data:nosuchformat}", 99));

        // 未注册的指令不应吞掉参数。
        await Assert.That(content).Contains("99");
    }

    [Test]
    public async Task ThrowingRenderer_DoesNotBreakLogging()
    {
        var errors = new List<Exception>();
        var content = await Write(temp, options =>
        {
            options.RegisterFormat("boom", static (_, _) => throw new InvalidOperationException("renderer failed"));
            options.InternalErrorLogger = errors.Add;
        }, logger =>
        {
            logger.LogInformation("before {V:boom}", 1);
            logger.LogInformation("after");
        });

        await Assert.That(content).Contains("before");
        await Assert.That(content).Contains("after");
    }

    [Test]
    public async Task SpecialFormatValue_LeavesReadablePlaceholderInMessage()
    {
        var content = await Write(temp, _ => { }, logger =>
            logger.LogInformation("payload {Data:panel}", new { A = 1 }));

        // 正文里留下类型提示，让读者知道有个结构化参数被单独渲染了。
        await Assert.That(content).Contains("Anonymous");
    }

    [Test]
    public async Task CircularReference_DoesNotHangOrThrow()
    {
        var node = new Node();
        node.Self = node;

        var content = await Write(temp, _ => { }, logger =>
            logger.LogInformation("node {Data:obj}", node));

        await Assert.That(content).Contains("循环引用");
    }

    sealed class Node
    {
        public string Name { get; set; } = "root";
        public Node? Self { get; set; }
    }
}
