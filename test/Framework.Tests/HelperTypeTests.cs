using TUnit.Assertions;
using TUnit.Core;

namespace Framework.Tests;

public partial class HelperTypeTests
{
    [Test]
    public async Task IsAnonymousType_WithAnonymousObject_ReturnsTrue()
    {
        var anonymous = new { Name = "test", Value = 123 };
        var result = Helper.IsAnonymousType(anonymous);
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAnonymousType_WithRegularObject_ReturnsFalse()
    {
        var result = Helper.IsAnonymousType("test");
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAnonymousType_WithNull_ThrowsArgumentNullException()
    {
        await Assert.That(async () => await Task.Run(() => Helper.IsAnonymousType(null!))).Throws<ArgumentNullException>();
    }
}