namespace Framework.Repository.Test;

public class LibraryReferenceTests
{
    [Fact]
    public void Framework_Repository_Type_Is_Accessible()
    {
        var type = typeof(Framework.Repository.Entities.EntityBase);

        Assert.NotNull(type);
    }
}
