namespace Brimborium.MessageFlow;

public class MessageFlowLoggingNoop : IMessageFlowLogging, ILogger {
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
        throw new NotImplementedException();
    }

    public bool IsEnabled(LogLevel logLevel) => false;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {    }
    
    public ILogger GetLogger() => this;

    public void LogHandleMessage(NodeIdentifier nameId, FlowMessage message) {    }

    public void LogSendMessage(NodeIdentifier nameId, FlowMessage message) {    }
}