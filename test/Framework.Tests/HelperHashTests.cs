using TUnit.Assertions;
using TUnit.Core;

namespace Framework.Tests;

public partial class HelperHashTests
{
    [Test]
    public async Task ComputeSha256Hash_ReturnsHash()
    {
        var result = Helper.ComputeSha256Hash("test");
        await Assert.That(string.IsNullOrEmpty(result)).IsFalse();
    }

    [Test]
    public async Task ComputeSha384Hash_ReturnsHash()
    {
        var result = Helper.ComputeSha384Hash("test");
        await Assert.That(string.IsNullOrEmpty(result)).IsFalse();
    }

    [Test]
    public async Task ComputeSha512Hash_ReturnsHash()
    {
        var result = Helper.ComputeSha512Hash("test");
        await Assert.That(string.IsNullOrEmpty(result)).IsFalse();
    }

    [Test]
    public async Task ComputeSha256Hash_IsConsistent()
    {
        var result1 = Helper.ComputeSha256Hash("test");
        var result2 = Helper.ComputeSha256Hash("test");
        await Assert.That(result1).IsEqualTo(result2);
    }
}