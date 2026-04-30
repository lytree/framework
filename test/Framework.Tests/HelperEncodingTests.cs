using TUnit.Assertions;
using TUnit.Core;

namespace Framework.Tests;

public partial class HelperEncodingTests
{
    [Test]
    public async Task ToHex_EncodesBytes()
    {
        var bytes = new byte[] { 10, 20, 30 };
        var result = Helper.ToHex(bytes);
        await Assert.That(string.IsNullOrEmpty(result)).IsFalse();
    }

    [Test]
    public async Task ToHex_WithNull_ReturnsEmptyString()
    {
        var result = Helper.ToHex(null!);
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task ToHex_WithLowerCase_ReturnsLowercase()
    {
        var bytes = new byte[] { 10, 20, 30 };
        var result = Helper.ToHex(bytes, true);
        await Assert.That(result).IsEqualTo(result.ToLower());
    }

    [Test]
    public async Task HexToBytes_DecodesHexString()
    {
        var result = Helper.HexToBytes("0a141e");
        await Assert.That(result).IsEqualTo(new byte[] { 10, 20, 30 });
    }

    [Test]
    public async Task HexToBytes_WithNull_ReturnsEmptyArray()
    {
        var result = Helper.HexToBytes(null!);
        await Assert.That(result.Length).IsEqualTo(0);
    }

    [Test]
    public async Task ToBase64_EncodesBytes()
    {
        var bytes = new byte[] { 10, 20, 30 };
        var result = Helper.ToBase64(bytes);
        await Assert.That(string.IsNullOrEmpty(result)).IsFalse();
    }

    [Test]
    public async Task ToBase64_WithNull_ReturnsEmptyString()
    {
        var result = Helper.ToBase64(null!);
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task StringToUnicode_ConvertsToUnicode()
    {
        var result = Helper.StringToUnicode("Hello");
        await Assert.That(string.IsNullOrEmpty(result)).IsFalse();
    }

    [Test]
    public async Task UnicodeToString_ConvertsFromUnicode()
    {
        var result = Helper.UnicodeToString("Hello");
        await Assert.That(string.IsNullOrEmpty(result)).IsFalse();
    }
}