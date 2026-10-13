using Microsoft.Extensions.Logging;

namespace AsaSavegameToolkit.Tests.Helpers;

public class TestLogger(TestContext testContext) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        testContext.WriteLine($"[{logLevel}] {message}");
    }
}