using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Core;

namespace Unity.WalmartAuthRelay;

public class TestModule
{
    private readonly ILogger<TestModule> _logger;

    public TestModule(ILogger<TestModule> logger)
    {
        _logger = logger;
    }

    [CloudCodeFunction("HelloWorld")]
    public string HelloWorld(string name)
    {
        _logger.LogInformation($"Saying hello to {name}");
        return $"Hello, {name}!";
    }
}
