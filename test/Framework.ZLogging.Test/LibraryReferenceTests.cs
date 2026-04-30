namespace Framework.ZLogging.Test;

public class LibraryReferenceTests
{
    [Fact]
    public void Framework_ZLogging_Type_Is_Accessible()
    {
        var type = typeof(Framework.ZLogging.ZLoggerSpectreConsoleOptions);

        Assert.NotNull(type);
    }
}
