using System.Reflection;

namespace Framework.Logging.Test;

public class LibraryReferenceTests
{
    [Fact]
    public void Framework_Logging_Assembly_Is_Loadable()
    {
        var assembly = Assembly.Load("Framework.Logging");

        Assert.NotNull(assembly);
    }
}
