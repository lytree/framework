namespace Framework.AspNetCore.Middleware.Test;

public class LibraryReferenceTests
{
    [Fact]
    public void Framework_AspNetCore_Middleware_Type_Is_Accessible()
    {
        var type = typeof(Framework.AspNetCore.Middleware.FlowAnalyze.FlowStatistics);

        Assert.NotNull(type);
    }
}
