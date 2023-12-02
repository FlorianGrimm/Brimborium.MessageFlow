
namespace Brimborium.MessageFlow;

public abstract class MessageProcessor(
    NodeIdentifier nameId,
    IMessageFlowLogging messageFlowLogging)
    : DisposableWithState(messageFlowLogging.GetLogger())
    , IMessageProcessor
    , IMessageFlowLogging {
    protected readonly NodeIdentifier _NameId = nameId;
    protected readonly IMessageFlowLogging _MessageFlowLogging = messageFlowLogging;

    public NodeIdentifier NameId => this._NameId;

    public virtual List<IMessageIncomingSink> GetListIncomingSink() => [];

    public virtual List<IMessageOutgoingSource> GetListOutgoingSource() => [];

    public virtual ValueTask BootAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
    
    public virtual ValueTask StartAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public virtual ValueTask ExecuteAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public virtual Task WaitUntilEmptyAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public virtual ValueTask ShutdownAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public virtual MessageGraphNode ToMessageGraphNode()
        => new MessageGraphNode(
            this.NameId,
            this.GetListOutgoingSource().ToListNodeIdentifier(),
            this.GetListIncomingSink().ToListNodeIdentifier(),
            new());

    public void LogSendMessage(NodeIdentifier nameId, FlowMessage message)
        => this._MessageFlowLogging.LogSendMessage(nameId, message);

    public ILogger GetLogger()
        => this._MessageFlowLogging.GetLogger();

    public void LogHandleMessage(NodeIdentifier nameId, FlowMessage message) {
        this._MessageFlowLogging.LogHandleMessage(nameId, message);
    }
}
