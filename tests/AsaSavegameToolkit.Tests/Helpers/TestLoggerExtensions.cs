using Microsoft.Extensions.Logging;

namespace AsaSavegameToolkit.Tests.Helpers;

public static class TestLoggerExtensions
{
    public static ILogger GetLogger(this TestContext testContext) => new TestLogger(testContext);
}
