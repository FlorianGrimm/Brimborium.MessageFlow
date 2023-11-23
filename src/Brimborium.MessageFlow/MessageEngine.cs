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
    private readonly GlobalOutgoingSourceProcessor _GlobalOutgoingSourceProcessor;
    private readonly GlobalIncomingSinkProcessor _GlobalIncomingSinkProcessor;
    private readonly NodeIdentifier _NameId;
    protected CancellationTokenSource? _ExecuteCTS;
    protected TaskCompletionSource? _ExecuteTaskCompletionSource;
    protected Task _TaskExecute = Task.CompletedTask;
    protected MessageFlowStart? _MessageFlowStart;

    public MessageEngine(
        NodeIdentifier nameId,
        ILogger logger) : base(logger) {
        this._NameId = nameId;
        this._GlobalOutgoingSourceProcessor = new GlobalOutgoingSourceProcessor(nameId + "GlobalSource", this, logger);
        this._GlobalIncomingSinkProcessor = new GlobalIncomingSinkProcessor(nameId + "GlobalSink", this, logger);
        this._GlobalOutgoingSource = new MessageOutgoingSource(this._GlobalOutgoingSourceProcessor.NameId + nameof(this.GlobalOutgoingSource), this._GlobalOutgoingSourceProcessor);
        this._GlobalIncomingSink = new MessageIncomingSink(this._GlobalIncomingSinkProcessor.NameId + nameof(this.GlobalIncomingSink), this._GlobalIncomingSinkProcessor, this.GlobalIncomingSinkWriteAsync);
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
        {
            MessageFlowStart messageFlowStart = MessageFlowStart.CreateStart(this.NameId);
            await this.GlobalOutgoingSource.SendMessageAsync(messageFlowStart, executeToken);
            this._MessageFlowStart = messageFlowStart;
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

    public async ValueTask<bool> SendFlowEnd(Exception? error = default, CancellationToken cancellationToken = default) {
        MessageFlowStart? messageFlowStart;
        lock (this) {
            messageFlowStart = this._MessageFlowStart;
            this._MessageFlowStart = null;
        }
        if (messageFlowStart is null) {
            return false;
        } else {
            var messageFlowEnd = messageFlowStart.CreateFlowEnd(error);
            await this.GlobalOutgoingSource.SendMessageAsync(messageFlowEnd, CancellationToken.None);
            return true;
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

    public MessageFlowGraph ToMessageFlowGraph() {
        var listConnection = this.ListConnection;
        var listMessageProcessor = (this.DictRunningProcessor.Count > 0)
            ? this.DictRunningProcessor.Values.ToImmutableArray()
            : this.CollectMessageProcessor().ToImmutableArray();
        List<MessageGraphNode> resultListNode = new();
        List<MessageGraphConnection> resultListConnection = new();
        foreach (var messageProcessor in listMessageProcessor) {
            resultListNode.Add(messageProcessor.ToMessageGraphNode());
        }
        HashSet<IMessageProcessor> htMessageProcessorSource = new();
        HashSet<IMessageProcessor> htMessageProcessorSink = new();
        Dictionary<NodeIdentifier, HashSet<NodeIdentifier>> dictLinkedNodes = new();
        foreach (var connection in listConnection) {
            resultListConnection.Add(connection.ToMessageGraphConnection());
            if (connection.OutgoingSource is IMessageOutgoingSourceInternal outgoingSourceInternal
                && connection.IncomingSink is IMessageIncomingSinkInternal incomingSinkInternal) {
                outgoingSourceInternal.CollectMessageProcessor(htMessageProcessorSource);
                incomingSinkInternal.CollectMessageProcessor(htMessageProcessorSink);
                foreach (var processorSource in htMessageProcessorSource) {
                    var nodeId = processorSource.NameId;
                    if (!dictLinkedNodes.TryGetValue(nodeId, out var hsLinkedNodes)) {
                        hsLinkedNodes = new();
                        dictLinkedNodes.Add(nodeId, hsLinkedNodes);
                    }
                    foreach (var processorSink in htMessageProcessorSink) {
                        hsLinkedNodes.Add(processorSink.NameId);
                    }
                }
                htMessageProcessorSource.Clear();
                htMessageProcessorSink.Clear();
            }
        }
        var dictNodeByNameId = resultListNode.ToDictionary(node => node.NameId);

        int order = 1;
        var hsToDoNodeId = dictNodeByNameId.Keys.ToHashSet();
        var hsNextNodeId = new HashSet<NodeIdentifier>();
        hsNextNodeId.Add(this._GlobalOutgoingSourceProcessor.NameId);
        while (hsNextNodeId.Count > 0) {
            var listCurrentNodes = hsNextNodeId.ToArray();
            hsNextNodeId.Clear();
            foreach (var nameId in listCurrentNodes) {
                if (hsToDoNodeId.Remove(nameId)) {
                    if (dictNodeByNameId.TryGetValue(nameId, out var messageGraphNode)) {
                        if (messageGraphNode.Order == 0) {
                            messageGraphNode.Order = order;
                            if (dictLinkedNodes.TryGetValue(nameId, out var hsLinkedNodeId)) {
                                foreach (var sinkId in hsLinkedNodeId) {
                                    hsNextNodeId.Add(sinkId);
                                }
                            }
                        } else {
                            messageGraphNode.Order = order;
                        }
                    }
                }
            }
            hsNextNodeId.IntersectWith(hsToDoNodeId);
            order++;
        }

        MessageFlowGraph result = new(
            resultListNode.OrderBy(n => n.Order).ToList(),
            resultListConnection);

        return result;
    }

    /*
    public MessageGraphNode ToMessageGraphNode()
        => new(
            this.NameId,
            this.GetListOutgoingSource().ToListNodeIdentifier(),
            this.GetListIncomingSink().ToListNodeIdentifier(),
            new());
    */
    public MessageGraphNode ToMessageGraphNode()
        => new(
            this.NameId,
            [],
            [],
            [this._GlobalOutgoingSourceProcessor.NameId, this._GlobalIncomingSinkProcessor.NameId]);

    private ValueTask GlobalIncomingSinkWriteAsync(RootMessage item, CancellationToken cancellationToken) {
        var handler = this.GlobalIncomingSinkWrite;
        if (handler is not null) { handler(item); }
        return ValueTask.CompletedTask;
    }


    public Action<RootMessage>? GlobalIncomingSinkWrite { get; set; }

    public void HandleApplicationStopping() {
        this.SetMessageFlowEnd(this);
    }

    public class GlobalOutgoingSourceProcessor(
        NodeIdentifier nameId,
        MessageEngine messageEngine,
        ILogger logger
    )
    : DisposableWithState(logger)
    , IMessageProcessor {
        private readonly MessageEngine _MessageEngine = messageEngine;

        public NodeIdentifier NameId => nameId;

        public List<IMessageIncomingSink> GetListIncomingSink() => [];

        public List<IMessageOutgoingSource> GetListOutgoingSource() => this._MessageEngine.GetListOutgoingSource();

        public ValueTask StartAsync(CancellationToken cancellationToken) {
            return ValueTask.CompletedTask;
        }

        public ValueTask ExecuteAsync(CancellationToken cancellationToken) {
            return ValueTask.CompletedTask;
        }

        public MessageGraphNode ToMessageGraphNode()
            => new(
                nameId,
                this.GetListOutgoingSource().ToListNodeIdentifier(),
                this.GetListIncomingSink().ToListNodeIdentifier(),
                new()
                );
    }


    public class GlobalIncomingSinkProcessor(
            NodeIdentifier nameId,
            MessageEngine messageEngine,
            ILogger logger
        )
        : DisposableWithState(logger)
        , IMessageProcessor {
        private readonly MessageEngine _MessageEngine = messageEngine;

        public NodeIdentifier NameId => nameId;

        public List<IMessageIncomingSink> GetListIncomingSink() => this._MessageEngine.GetListIncomingSink();

        public List<IMessageOutgoingSource> GetListOutgoingSource() => [];

        public ValueTask StartAsync(CancellationToken cancellationToken) {
            return ValueTask.CompletedTask;
        }

        public ValueTask ExecuteAsync(CancellationToken cancellationToken) {
            return ValueTask.CompletedTask;
        }

        public MessageGraphNode ToMessageGraphNode()
            => new(
                nameId,
                this.GetListOutgoingSource().ToListNodeIdentifier(),
                this.GetListIncomingSink().ToListNodeIdentifier(),
                new()
                );
    }
}
