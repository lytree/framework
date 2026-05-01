using TUnit.Assertions;
using TUnit.Core;

namespace Framework.Tests;

public partial class HelperAssemblyTests
{
    [Test]
    public async Task GetAssemblyList_WithValidName_ReturnsAssembly()
    {
        var result = Helper.GetAssemblyList(new[] { "System.Runtime" });
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task GetAssemblyList_WithNull_ReturnsEmptyArray()
    {
        var result = Helper.GetAssemblyList(null!);
        await Assert.That(result.Length).IsEqualTo(0);
    }

    [Test]
    public async Task GetAssemblyList_WithEmptyArray_ReturnsEmptyArray()
    {
        var result = Helper.GetAssemblyList(Array.Empty<string>());
        await Assert.That(result.Length).IsEqualTo(0);
    }
}