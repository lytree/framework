namespace Framework.Charts.Test;

public class LibraryReferenceTests
{
    [Fact]
    public void Framework_Charts_Type_Is_Accessible()
    {
        var type = typeof(Framework.Charts.Plots);

        Assert.NotNull(type);
    }
}
