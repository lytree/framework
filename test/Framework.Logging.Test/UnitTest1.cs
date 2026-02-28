using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Framework.Logging.Test;

public class UnitTest1
{

    private readonly string _configPath = "application.json";

    private IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("application.json", optional: false, reloadOnChange: true)
            .Build();
    }

    [Fact]
    public void Test1()
    {
        var config = CreateConfiguration();
        var value = config["Zlogger:MinLogLevel"];
        Assert.NotNull(value);
    }
}
