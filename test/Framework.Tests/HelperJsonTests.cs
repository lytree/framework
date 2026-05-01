using TUnit.Assertions;
using TUnit.Core;

namespace Framework.Tests;

public partial class HelperJsonTests
{
    [Test]
    public async Task JsonSerialize_SerializesObject()
    {
        var obj = new { Name = "test", Value = 123 };
        var result = Helper.JsonSerialize(obj);
        await Assert.That(string.IsNullOrEmpty(result)).IsFalse();
    }

    [Test]
    public async Task JsonDeserialize_DeserializesJson()
    {
        var json = "{\"name\":\"test\",\"value\":123}";
        var result = Helper.JsonDeserialize<TestClass>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("test");
    }

    [Test]
    public async Task JsonDeserialize_WithType_DeserializesJson()
    {
        var json = "{\"name\":\"test\",\"value\":123}";
        var result = Helper.JsonDeserialize(json, typeof(TestClass));
        await Assert.That(result).IsNotNull();
    }

    private class TestClass
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }
}