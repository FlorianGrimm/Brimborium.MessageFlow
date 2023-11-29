namespace Brimborium.MessageFlow;

// TODO: idea Monitoring Service for WS Signal-R 
public class MessageFlowLoggingMonitoring(
    ILogger logger
    ) : IMessageFlowLogging {
    private readonly ILogger _Logger = logger;

    public ILogger GetLogger() => this._Logger;

    // TODO: we need the flow here
    public void LogHandleMessage(NodeIdentifier nameId, FlowMessage message) {
        // TODO: NYI
    }

    public void LogSendMessage(NodeIdentifier nameId, FlowMessage message) {
        // TODO: NYI
    }
}