namespace Framework.Test;

public class LibraryReferenceTests
{
    [Fact]
    public void Framework_Type_Is_Accessible()
    {
        var type = typeof(Framework.UnsupportedException);

        Assert.NotNull(type);
    }
}
