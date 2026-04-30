namespace Framework.AspNetCore.Test;

public class LibraryReferenceTests
{
    [Fact]
    public void Framework_AspNetCore_Type_Is_Accessible()
    {
        var type = typeof(Framework.AspNetCore.Mvc.ApiResult);

        Assert.NotNull(type);
    }
}
