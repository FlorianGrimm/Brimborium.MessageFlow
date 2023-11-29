namespace Brimborium.MessageFlow;

public class MessageFlowLoggingChained(
    IMessageFlowLogging? next, 
    ILogger logger
    ) : IMessageFlowLogging {
    private readonly ILogger _Logger = logger;
    private IMessageFlowLogging? _Next= next;

    public IMessageFlowLogging? Next { get => this._Next; set => this._Next = value; }

    public ILogger GetLogger() => this._Logger;

    public void LogHandleMessage(NodeIdentifier nameId, FlowMessage message) {
        this._Next?.LogHandleMessage(nameId, message);
    }

    public void LogSendMessage(NodeIdentifier nameId, FlowMessage message) {
        this._Next?.LogSendMessage(nameId, message);
    }
}
