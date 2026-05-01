using TUnit.Assertions;
using TUnit.Core;

namespace Framework.Tests;

public interface ITestInterface
{
    string Name { get; set; }
    int Value { get; set; }
}

public partial class HelperInterfaceTests
{
    [Test]
    public async Task GetInterfacePropertyNames_ReturnsPropertyNames()
    {
        var result = Helper.GetInterfacePropertyNames<ITestInterface>();
        await Assert.That(result).Contains("Name");
        await Assert.That(result).Contains("Value");
    }

    [Test]
    public async Task GetInterfacePropertyNames_WithNonInterface_ThrowsArgumentException()
    {
        await Assert.That(async () => await Task.Run(() => Helper.GetInterfacePropertyNames<string>())).Throws<ArgumentException>();
    }
}