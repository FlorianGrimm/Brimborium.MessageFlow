namespace Brimborium.MessageFlow;

public class MessageEngine
    : DisposableWithState
    , IMessageEngine
    , IMessageProcessor
    , IMessageConnectionAccessor {

    protected ImmutableArray<IMessageConnection> ListConnection = [];
    protected ImmutableDictionary<NodeIdentifier, ImmutableArray<IMessageIncomingSink>> DictSinkByOutgoingSource = ImmutableDictionary<NodeIdentifier, ImmutableArray<IMessageIncomingSink>>.Empty;
    protected ImmutableDictionary<NodeIdentifier, IMessageProcessor> DictRunningProcessor = ImmutableDictionary<NodeIdentifier, IMessageProcessor>.Empty;
    protected IMessageOutgoingSource _GlobalOutgoingSource;
    protected IMessageIncomingSink _GlobalIncomingSink;
    private readonly NodeIdentifier _NameId;
    protected CancellationTokenSource? _ExecuteCTS;
    protected TaskCompletionSource? _ExecuteTaskCompletionSource;
    protected Task _TaskExecute = Task.CompletedTask;

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

    public void SetMessageFlowEnd(IMessageProcessor owner) {
        lock (this) {
            foreach (var outgoingSource in owner.GetListOutgoingSource()) {
                this.DictSinkByOutgoingSource = this.DictSinkByOutgoingSource.Remove(outgoingSource.NameId);
                if (this.DictSinkByOutgoingSource.Count == 0) { }
            }
            this.DictRunningProcessor = this.DictRunningProcessor.Remove(owner.NameId);
        }
        this.Logger.LogInformation("Owner:{MessageProcessorId}, DictRunningProcessor:#{DictRunningProcessorCount}", owner.NameId, this.DictRunningProcessor.Count);
        if (this.DictRunningProcessor.Count == 0) {
            this._ExecuteCTS?.Cancel();
        }
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken) {
        lock (this) {
            if (this._ExecuteCTS is not null) {
                return;
            }
            if (this._ExecuteTaskCompletionSource is not null) {
                return;
            }
            this._ExecuteCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            this._ExecuteTaskCompletionSource = new TaskCompletionSource();
            this._TaskExecute = this._ExecuteTaskCompletionSource.Task;
        }

        var htMessageProcessor = CollectMessageProcessor();
        var executeToken = this._ExecuteCTS.Token;
        foreach (var messageProcessor in htMessageProcessor) {
            await messageProcessor.StartAsync(executeToken);
            lock (this) {
                this.DictRunningProcessor = this.DictRunningProcessor.Add(messageProcessor.NameId, messageProcessor);
            }
        }
    }

    public async ValueTask ExecuteAsync(CancellationToken cancellationToken) {
        if (this._ExecuteCTS is null) { return; }
        if (this._ExecuteTaskCompletionSource is null) { return; }
        if (this._TaskExecute.IsCompleted) { return; }

        var listMessageProcessor = this.DictRunningProcessor.Values.ToArray();

        foreach (var messageProcessor in listMessageProcessor) {
            try {
                await messageProcessor.ExecuteAsync(cancellationToken);
            } catch (OperationCanceledException) {
            }
        }

        using (var executeCTS = this._ExecuteCTS) {
            try {
                if (executeCTS is not null) {
                    if (!executeCTS.IsCancellationRequested) {
                        await executeCTS.CancelAsync();
                    }
                }
            } catch {
            } finally {
                this._ExecuteCTS = null;
                this._ExecuteTaskCompletionSource = null;
            }
        }
    }

    public Task TaskExecute => this._TaskExecute;

    private HashSet<IMessageProcessor> CollectMessageProcessor() {
        var htMessageProcessor = new System.Collections.Generic.HashSet<IMessageProcessor>();
        foreach (var connection in this.ListConnection) {
            if (connection is IMessageConnectionInternal messageConnectionInternal) {
                messageConnectionInternal.CollectMessageProcessor(htMessageProcessor);
            }
        }
        htMessageProcessor.Remove(this);

        return htMessageProcessor;
    }


    public List<IMessageIncomingSink> GetListIncomingSink() => [this.GlobalIncomingSink];

    public List<IMessageOutgoingSource> GetListOutgoingSource() => [this.GlobalOutgoingSource];

    public MessageGraph ToMessageGraph() {
        var listConnection = this.ListConnection;
        var listMessageProcessor = (this.DictRunningProcessor.Count > 0)
            ? this.DictRunningProcessor.Values.ToImmutableArray()
            : this.CollectMessageProcessor().ToImmutableArray();
        MessageGraph result = new(new(), new());
        foreach (var messageProcessor in listMessageProcessor) {
            result.ListNode.Add(messageProcessor.ToMessageGraphNode());
        }
        foreach (var connection in listConnection) {
            result.ListConnection.Add(connection.ToMessageGraphConnection());
        }
        return result;
    }

    public MessageGraphNode ToMessageGraphNode()
        => new(
            this.NameId, 
            this.GetListOutgoingSource().ToListNodeIdentifier(),
            this.GetListIncomingSink().ToListNodeIdentifier(),
            new());


    private ValueTask GlobalIncomingSinkWriteAsync(RootMessage item, CancellationToken cancellationToken) {
        var handler = this.GlobalIncomingSinkWrite;
        if (handler is not null) { handler(item); }
        return ValueTask.CompletedTask;
    }

    public Action<RootMessage>? GlobalIncomingSinkWrite { get; set; }

}
