
namespace Brimborium.MessageFlow;

public class MessageEngine
    : DisposableWithState
    , IMessageEngine
    , IMessageProcessor
    , IMessageConnectionAccessor {

    protected ImmutableArray<IMessageConnection> ListConnection = [];
    protected ImmutableDictionary<NodeIdentifier, ImmutableArray<IMessageIncomingSink>> DictSinkByOutgoingSource = ImmutableDictionary<NodeIdentifier, ImmutableArray<IMessageIncomingSink>>.Empty;
    protected IMessageOutgoingSource _GlobalOutgoingSource;
    protected IMessageIncomingSink _GlobalIncomingSink;
    private readonly NodeIdentifier _NameId;

    public MessageEngine(
        NodeIdentifier nameId,
        ILogger logger) : base(logger) {
        this._NameId = nameId;
        this._GlobalOutgoingSource = new MessageOutgoingSource(nameId + nameof(this.GlobalOutgoingSource), this);
        this._GlobalIncomingSink = new MessageIncomingSink(nameId + nameof(this.GlobalIncomingSink), this, this.GlobalIncomingSinkWriteAsync);
    }

    public NodeIdentifier NameId => this._NameId;

    public IMessageOutgoingSource GlobalOutgoingSource => this._GlobalOutgoingSource;
    public IMessageIncomingSink GlobalIncomingSink => this._GlobalIncomingSink;

    public void ConnectMessage(IMessageOutgoingSource outgoingSource, IMessageIncomingSink incomingSink) {
        var connection = new MessageConnection(outgoingSource, incomingSink);
        this.ListConnection = this.ListConnection.Add(connection);
        this.ListConnectionPostChange();
        if (outgoingSource is IMessageOutgoingSourceInternal sourceInternal) {
            sourceInternal.Connect(this);
        }
    }

    public void ConnectData<T>(IMessageOutgoingSource<T> outgoingSource, IMessageIncomingSink<T> incomingSink) where T : RootMessage {
        var connection = new MessageConnection<T>(outgoingSource, incomingSink);
        this.ListConnection = this.ListConnection.Add(connection);
        this.ListConnectionPostChange();
        if (outgoingSource is IMessageOutgoingSourceInternal sourceInternal) {
            sourceInternal.Connect(this);
        }
    }

    private void ListConnectionPostChange() {
        this.DictSinkByOutgoingSource = this.ListConnection
            .GroupBy(connection => connection.OutgoingSource.NameId, connection => connection.IncomingSink)
            .ToImmutableDictionary(group => group.Key, group => group.ToImmutableArray());
    }

    public bool TryGetSinks(
        NodeIdentifier sourceId,
        [MaybeNullWhen(false)] out ImmutableArray<IMessageIncomingSink> result) {
        if (this.DictSinkByOutgoingSource.TryGetValue(sourceId, out var found)) {
            result = found;
            return true;
        } else {
            result = default;
            return false;
        }
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken) {
        var htMessageProcessor = new System.Collections.Generic.HashSet<IMessageProcessor>();
        foreach (var connection in this.ListConnection) {
            if (connection is IMessageConnectionInternal messageConnectionInternal) {
                messageConnectionInternal.CollectMessageProcessor(htMessageProcessor);
            }
        }

        foreach (var messageProcessor in htMessageProcessor) {
            await messageProcessor.StartAsync(cancellationToken);
        }
    }

    public async ValueTask ExecuteAsync(CancellationToken cancellationToken) {
        var htMessageProcessor = new System.Collections.Generic.HashSet<IMessageProcessor>();
        foreach (var connection in this.ListConnection) {
            if (connection is IMessageConnectionInternal messageConnectionInternal) {
                messageConnectionInternal.CollectMessageProcessor(htMessageProcessor);
            }
        }

        foreach (var messageProcessor in htMessageProcessor) {
            await messageProcessor.ExecuteAsync(cancellationToken);
        }
    }


    public List<IMessageIncomingSink> GetListIncomingSink() {
        List<IMessageIncomingSink> result = [];
        return result;
    }

    public List<IMessageOutgoingSource> GetListOutgoingSource() {
        List<IMessageOutgoingSource> result = [];
        return result;
    }

    private ValueTask GlobalIncomingSinkWriteAsync(RootMessage item, CancellationToken cancellationToken) {
        if (this.GlobalIncomingSinkWrite is not null) {
            this.GlobalIncomingSinkWrite(item);
        }
        return ValueTask.CompletedTask;
    }


    public Action<RootMessage>? GlobalIncomingSinkWrite { get; set; }

}
