using System.IO;
using TUnit.Assertions;
using TUnit.Core;

namespace Framework.Tests;

public partial class HelperStringTests
{
    [Test]
    public async Task GenerateRandom_ReturnsStringWithSpecifiedLength()
    {
        var result = Helper.GenerateRandom(16);
        await Assert.That(result.Length).IsEqualTo(16);
    }

    [Test]
    public async Task GenerateRandom_DefaultLengthIs32()
    {
        var result = Helper.GenerateRandom();
        await Assert.That(result.Length).IsEqualTo(32);
    }

    [Test]
    public async Task GenerateRandomNumber_ReturnsNumericString()
    {
        var result = Helper.GenerateRandomNumber();
        await Assert.That(result.Length).IsEqualTo(6);
    }

    [Test]
    public async Task GenerateRandomNumber_WithCustomLength()
    {
        var result = Helper.GenerateRandomNumber(4);
        await Assert.That(result.Length).IsEqualTo(4);
    }

    [Test]
    public async Task Format_WithObject_ReplacesPlaceholders()
    {
        var result = Helper.Format("Hello {Name}!", new { Name = "World" });
        await Assert.That(result).IsEqualTo("Hello World!");
    }

    [Test]
    public async Task Format_WithNullString_ReturnsEmptyString()
    {
        var result = Helper.Format(null!, new { Name = "World" });
        await Assert.That(string.IsNullOrEmpty(result)).IsTrue();
    }
}