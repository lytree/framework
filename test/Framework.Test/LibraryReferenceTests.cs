using System.Reflection;

namespace Framework.Test;

public class LibraryReferenceTests
{
    [Fact]
    public void Framework_Assembly_Is_Loadable()
    {
        var assembly = Assembly.Load("Framework");

        Assert.NotNull(assembly);
    }
}
