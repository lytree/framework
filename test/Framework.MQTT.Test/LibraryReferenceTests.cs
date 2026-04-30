using System.Reflection;

namespace Framework.MQTT.Test;

public class LibraryReferenceTests
{
    [Fact]
    public void Framework_MQTT_Assembly_Is_Loadable()
    {
        var assembly = Assembly.Load("Framework.MQTT");

        Assert.NotNull(assembly);
    }
}
