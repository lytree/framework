using TUnit.Assertions;
using TUnit.Core;

namespace Framework.Tests;

public partial class HelperConvertTests
{
    [Test]
    public async Task ToBase32String_EncodesBytes()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        var result = Helper.ToBase32String(bytes);
        await Assert.That(string.IsNullOrEmpty(result)).IsFalse();
    }

    [Test]
    public async Task ToBase32String_WithEmptyBytes_ReturnsEmptyString()
    {
        var result = Helper.ToBase32String(Array.Empty<byte>());
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task FromBase32String_DecodesString()
    {
        var encoded = Helper.ToBase32String(new byte[] { 1, 2, 3, 4, 5 });
        var result = Helper.FromBase32String(encoded);
        await Assert.That(result.Length).IsEqualTo(5);
    }

    [Test]
    public async Task FromBase32String_WithEmptyString_ThrowsArgumentException()
    {
        await Assert.That(async () => await Task.Run(() => Helper.FromBase32String(""))).Throws<ArgumentException>();
    }
}
