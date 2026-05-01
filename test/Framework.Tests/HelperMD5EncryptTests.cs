using TUnit.Assertions;
using TUnit.Core;

namespace Framework.Tests;

public partial class HelperMD5EncryptTests
{
    [Test]
    public async Task MD5Encrypt16_Returns16CharacterString()
    {
        var result = Helper.MD5Encrypt16("test");
        await Assert.That(result.Length).IsEqualTo(16);
    }

    [Test]
    public async Task MD5Encrypt16_WithLowerCase_ReturnsLowercase()
    {
        var result = Helper.MD5Encrypt16("test", true);
        await Assert.That(result).IsEqualTo(result.ToLower());
    }

    [Test]
    public async Task MD5Encrypt32_Returns32CharacterString()
    {
        var result = Helper.MD5Encrypt32("test");
        await Assert.That(result.Length).IsEqualTo(32);
    }

    [Test]
    public async Task MD5Encrypt64_ReturnsBase64String()
    {
        var result = Helper.MD5Encrypt64("test");
        await Assert.That(string.IsNullOrEmpty(result)).IsFalse();
    }

    [Test]
    public async Task MD5Encrypt16_WithNull_ReturnsEmptyString()
    {
        var result = Helper.MD5Encrypt16(null!);
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task MD5Encrypt32_WithNull_ReturnsEmptyString()
    {
        var result = Helper.MD5Encrypt32(null!);
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task MD5Encrypt64_WithNull_ReturnsEmptyString()
    {
        var result = Helper.MD5Encrypt64(null!);
        await Assert.That(result).IsEmpty();
    }
}
