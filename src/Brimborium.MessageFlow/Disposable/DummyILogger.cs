namespace Brimborium.MessageFlow.Disposable;

public class DummyILogger : ILogger {
    private static DummyILogger? _Instance;
    public static ILogger Instance => _Instance ??= new DummyILogger();

    public bool IsEnabled(LogLevel logLevel) => false;
    
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => default;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}