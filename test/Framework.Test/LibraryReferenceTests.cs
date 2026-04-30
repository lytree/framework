namespace Framework.Test;

public class LibraryReferenceTests
{
    [Fact]
    public void Framework_Type_Is_Accessible()
    {
        var ex = new Framework.UnsupportedException();

        Assert.NotNull(ex);
    }
}
