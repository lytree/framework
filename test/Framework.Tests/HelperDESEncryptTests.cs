using TUnit.Assertions;
using TUnit.Core;

namespace Framework.Tests;

public partial class HelperDESEncryptTests
{
    [Test]
    public async Task DESEncrypt_EncryptsString()
    {
        var encrypted = Helper.DESEncrypt("test", "12345678");
        await Assert.That(string.IsNullOrEmpty(encrypted)).IsFalse();
    }

    [Test]
    public async Task DESDecrypt_DecryptsString()
    {
        var encrypted = Helper.DESEncrypt("test", "12345678");
        var decrypted = Helper.DESDecrypt(encrypted, "12345678");
        await Assert.That(decrypted).IsEqualTo("test");
    }

    [Test]
    public async Task DESEncrypt4Hex_ReturnsHexString()
    {
        var encrypted = Helper.DESEncrypt4Hex("test", "12345678");
        await Assert.That(string.IsNullOrEmpty(encrypted)).IsFalse();
    }

    [Test]
    public async Task DESDecrypt4Hex_DecryptsHexString()
    {
        var encrypted = Helper.DESEncrypt4Hex("test", "12345678");
        var decrypted = Helper.DESDecrypt4Hex(encrypted, "12345678");
        await Assert.That(decrypted).IsEqualTo("test");
    }

    [Test]
    public async Task DESDecrypt_WithNull_ReturnsNull()
    {
        var result = Helper.DESDecrypt(null!);
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task DESEncrypt_WithNull_ReturnsEmptyString()
    {
        var result = Helper.DESEncrypt(null!);
        await Assert.That(result).IsEmpty();
    }
}
