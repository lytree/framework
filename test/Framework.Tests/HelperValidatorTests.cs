using TUnit.Assertions;
using TUnit.Core;

namespace Framework.Tests;

public partial class HelperValidatorTests
{
    [Test]
    public async Task IsNumber_WithValidNumber_ReturnsTrue()
    {
        var result = "123.45".IsNumber();
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsNumber_WithInvalidNumber_ReturnsFalse()
    {
        var result = "abc".IsNumber();
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsNumberic_WithNegativeNumber_ReturnsTrue()
    {
        var result = "-123".IsNumberic();
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsTel_WithValidTel_ReturnsTrue()
    {
        var result = "010-85849685".IsTel();
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsPhone_WithValidPhone_ReturnsTrue()
    {
        var result = "13812345678".IsPhone();
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsFax_WithValidFax_ReturnsTrue()
    {
        var result = "+86 10-85849685".IsFax();
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsMobile_WithValidMobile_ReturnsTrue()
    {
        var result = "13812345678".IsMobile();
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsEmail_WithValidEmail_ReturnsTrue()
    {
        var result = "test@example.com".IsEmail();
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsOnlyChinese_WithChineseCharacters_ReturnsTrue()
    {
        var result = "你好".IsOnlyChinese();
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsNzx_WithValidString_ReturnsTrue()
    {
        var result = "abc123_".IsNzx();
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsSzzm_WithAlphabeticString_ReturnsTrue()
    {
        var result = "abc123".IsSzzm();
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsPostCode_WithValidPostCode_ReturnsTrue()
    {
        var result = "100000".IsPostCode();
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task CheckLength_WithValidLength_ReturnsTrue()
    {
        var result = "hello".CheckLength(3);
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task CheckLength_WithTooLong_ReturnsFalse()
    {
        var result = "hello".CheckLength(10);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsDateTime_WithValidDateTime_ReturnsTrue()
    {
        var result = "2024-06-15".IsDateTime();
        await Assert.That(result).IsTrue();
    }
}