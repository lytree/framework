namespace Framework.Mvvm.Test;

public class LibraryReferenceTests
{
    [Fact]
    public void Framework_Mvvm_Type_Is_Accessible()
    {
        var type = typeof(Framework.Mvvm.Constant.WindowsVariable);

        Assert.NotNull(type);
    }
}
