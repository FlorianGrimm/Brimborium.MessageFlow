namespace Brimborium.MessageFlow;

public abstract class MessageProcessor(
    NodeIdentifier nameId,
    ILogger logger)
    : DisposableWithState(logger)
    , IMessageProcessor {
    private readonly NodeIdentifier _NameId = nameId;

    public NodeIdentifier NameId => this._NameId;

    public virtual List<IMessageIncomingSink> GetListIncomingSink() => [];

    public virtual List<IMessageOutgoingSource> GetListOutgoingSource() => [];

    public abstract ValueTask StartAsync(CancellationToken cancellationToken);

    public abstract ValueTask ExecuteAsync(CancellationToken cancellationToken);
}
