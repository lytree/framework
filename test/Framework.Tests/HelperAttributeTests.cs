using TUnit.Assertions;
using TUnit.Core;
using DisplayNameAttr = System.ComponentModel.DisplayNameAttribute;
using DisplayAttr = System.ComponentModel.DataAnnotations.DisplayAttribute;

namespace Framework.Tests;

public class TestAttributeClass
{
    [DisplayNameAttr("Test Property")]
    public string? TestProperty { get; set; }

    [DisplayAttr(Name = "Display Name")]
    public string? DisplayProperty { get; set; }
}

public partial class HelperAttributeTests
{
    [Test]
    public async Task GetAttributeDisplayName_ReturnsDisplayName()
    {
        var result = Helper.GetAttributeDisplayName<TestAttributeClass>("TestProperty");
        await Assert.That(result).IsEqualTo("Test Property");
    }

    [Test]
    public async Task GetAttributeDisplay_ReturnsDisplayAttributeName()
    {
        var result = Helper.GetAttributeDisplay<TestAttributeClass>("DisplayProperty");
        await Assert.That(result).IsEqualTo("Display Name");
    }
}
