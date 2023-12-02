namespace Brimborium.MessageFlow;

public class MessageEngine
    : DisposableWithState
    , IMessageEngine
    , IMessageProcessor
    , IMessageConnectionAccessor
    , IMessageFlowLogging {

    protected ImmutableArray<IMessageConnection> ListConnection = [];
    protected ImmutableDictionary<NodeIdentifier, ImmutableArray<IMessageIncomingSink>> DictSinkByOutgoingSource = ImmutableDictionary<NodeIdentifier, ImmutableArray<IMessageIncomingSink>>.Empty;
    protected ImmutableDictionary<NodeIdentifier, IMessageProcessor> DictRunningProcessor = ImmutableDictionary<NodeIdentifier, IMessageProcessor>.Empty;
    protected IMessageOutgoingSource _GlobalOutgoingSource;
    protected IMessageIncomingSink _GlobalIncomingSink;

    protected readonly GlobalOutgoingSourceProcessor _GlobalOutgoingSourceProcessor;
    protected readonly GlobalIncomingSinkProcessor _GlobalIncomingSinkProcessor;
    protected readonly NodeIdentifier _NameId;
    protected readonly MessageFlowLoggingChained _MessageFlowLoggingIncomingSink;
    protected readonly IMessageFlowLogging _MessageFlowLoggingOutgoingSource;

    protected CancellationTokenSource? _ExecuteCTS;
    protected TaskCompletionSource? _ExecuteTaskCompletionSource;
    protected Task _TaskExecute = Task.CompletedTask;

    public MessageEngine(
        NodeIdentifier nameId,
        ILogger logger
        ) : this(nameId, null, logger) {
    }

    public MessageEngine(
        NodeIdentifier nameId,
        IMessageFlowLogging? messageFlowLogging,
        ILogger logger
        ) : base(logger) {
        this._NameId = nameId;
        this._MessageFlowLoggingOutgoingSource = messageFlowLogging ?? new MessageFlowLoggingNoop();
        this._MessageFlowLoggingIncomingSink = new MessageFlowLoggingChained(this._MessageFlowLoggingOutgoingSource, logger);
        this._GlobalOutgoingSourceProcessor = new GlobalOutgoingSourceProcessor(nameId + "GlobalSource", this, this._MessageFlowLoggingOutgoingSource);
        this._GlobalIncomingSinkProcessor = new GlobalIncomingSinkProcessor(nameId + "GlobalSink", this, this._MessageFlowLoggingOutgoingSource);
        this._GlobalOutgoingSource = new MessageOutgoingSource(this._GlobalOutgoingSourceProcessor.NameId + nameof(this.GlobalOutgoingSource), this._GlobalOutgoingSourceProcessor);
        this._GlobalIncomingSink = new MessageIncomingSink(this._GlobalIncomingSinkProcessor.NameId + nameof(this.GlobalIncomingSink), this._GlobalIncomingSinkProcessor, this.GlobalIncomingSinkWriteAsync);
    }

    public NodeIdentifier NameId => this._NameId;

    public IMessageFlowLogging MessageFlowLogging => this._MessageFlowLoggingOutgoingSource;

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

    public void ConnectData<T>(IMessageOutgoingSource<T> outgoingSource, IMessageIncomingSink<T> incomingSink) where T : FlowMessage {
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

    public virtual async ValueTask BootAsync(CancellationToken cancellationToken) {
        var htMessageProcessor = CollectMessageProcessor();
        foreach (var processor in htMessageProcessor) {
            await processor.BootAsync(cancellationToken);
        }
        await ValueTask.CompletedTask;
    }

    public async virtual ValueTask StartAsync(CancellationToken cancellationToken) {
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
        foreach (var processor in htMessageProcessor) {
            this.Logger.LogInformation("Start: {processorId}", processor.NameId);
            await processor.StartAsync(executeToken);
            lock (this) {
                this.DictRunningProcessor = this.DictRunningProcessor.Add(processor.NameId, processor);
            }
        }
    }

    public void Disconnected(IMessageProcessor caller) {
    }

    public virtual async ValueTask ExecuteAsync(CancellationToken cancellationToken) {
        if (this._ExecuteCTS is null) { return; }
        if (this._ExecuteTaskCompletionSource is null) { return; }
        if (this._TaskExecute.IsCompleted) { return; }

        var listMessageProcessor = this.DictRunningProcessor.Values.ToArray();
        var executeToken = this._ExecuteCTS.Token;
        foreach (var processor in listMessageProcessor) {
            try {
                await processor.ExecuteAsync(executeToken);
            } catch (OperationCanceledException) {
                this.Logger.LogInformation("stopped: {processorId}", processor.NameId);
            } catch (Exception error) {
                this.Logger.LogError(error, "failed: {processorId}", processor.NameId);
            }
            lock (this.DictRunningProcessor) {
                this.DictRunningProcessor.Remove(processor.NameId);
            }
            this.Logger.LogInformation("Execute Done: {processorId}", processor.NameId);
        }
        {
            var executeCTS = this._ExecuteCTS;
            try {
                if (executeCTS is not null) {
                    if (!executeCTS.IsCancellationRequested) {
                        await executeCTS.CancelAsync();
                    }
                }
            } catch {
            } finally {
                this._ExecuteCTS = null;
            }
        }
        this._ExecuteTaskCompletionSource.SetResult();
    }

    //public Task TaskExecute => this.GetTaskExecute();

    public async Task GetTaskExecute(CancellationToken cancellationToken) {
        try {
            if (this._ExecuteTaskCompletionSource is not null) {
                await this._ExecuteTaskCompletionSource.Task;
            }
            await this._TaskExecute.WaitAsync(cancellationToken);
        } catch (TaskCanceledException) {
        } catch (OperationCanceledException) {
        }
    }

    public async Task WaitUntilEmptyAsync(CancellationToken cancellationToken) {
        List<IMessageProcessor> listRunningProcessor;
        lock (this) {
            listRunningProcessor = this.DictRunningProcessor.Values.ToList();
        }
        foreach (var processor in listRunningProcessor) {
            try {
                this.Logger.LogInformation("WaitUntilEmptyAsync: {processorId}", processor.NameId);
                await processor.WaitUntilEmptyAsync(cancellationToken);
            } catch (Exception error) {
                // TODO: log
                this.Logger.LogError(error, "Error");
            }
        }
    }

    public virtual async ValueTask ShutdownAsync(CancellationToken cancellationToken) {
        if (this._ExecuteCTS is not null) {
            this.Logger.LogInformation("Shutdown: {engineId}", this.NameId);
            await this._ExecuteCTS.CancelAsync();
        }
        try {
            using (var cts = new CancellationTokenSource()) {
                var task = this.GetTaskExecute(cancellationToken);
                var taskDelay = Task.Delay(TimeSpan.FromSeconds(30), cts.Token);
                var taskResult = await Task.WhenAny(task, taskDelay);
                if (!taskDelay.IsCompleted) {
                    await cts.CancelAsync();
                }
            }
        } catch (TaskCanceledException) {
        } catch (OperationCanceledException) {
        }
        List<IMessageProcessor> listRunningProcessor;
        lock (this) {
            listRunningProcessor = this.DictRunningProcessor.Values.ToList();
        }
        foreach (var processor in listRunningProcessor) {
            try {
                this.Logger.LogInformation("Shutdown: {processorId}", processor.NameId);
                await processor.ShutdownAsync(cancellationToken);
            } catch (Exception error) {
                // TODO: log
                this.Logger.LogError(error, "Error");
            }
        }
    }

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

    public MessageGraphNode ToMessageGraphNode()
        => new(
            this.NameId,
            [],
            [],
            [this._GlobalOutgoingSourceProcessor.NameId, this._GlobalIncomingSinkProcessor.NameId]);

    private ValueTask GlobalIncomingSinkWriteAsync(FlowMessage item, CancellationToken cancellationToken) {
        var handler = this.GlobalIncomingSinkWrite;
        if (handler is not null) { handler(item); }
        return ValueTask.CompletedTask;
    }

    public Action<FlowMessage>? GlobalIncomingSinkWrite { get; set; }

    // IMessageFlowLogging

    public ILogger GetLogger() => this.Logger;

    public void LogHandleMessage(NodeIdentifier nameId, FlowMessage message) {
        this._MessageFlowLoggingOutgoingSource.LogHandleMessage(nameId, message);
    }

    public void LogSendMessage(NodeIdentifier nameId, FlowMessage message) {
        this._MessageFlowLoggingOutgoingSource.LogSendMessage(nameId, message);
    }

    public sealed class GlobalOutgoingSourceProcessor(
        NodeIdentifier nameId,
        MessageEngine messageEngine,
        IMessageFlowLogging messageFlowLogging
    )
    : DisposableWithState(messageFlowLogging.GetLogger())
    , IMessageProcessor
    , IMessageFlowLogging {
        private readonly MessageEngine _MessageEngine = messageEngine;
        private readonly IMessageFlowLogging _MessageFlowLogging = messageFlowLogging;

        public NodeIdentifier NameId => nameId;

        public List<IMessageIncomingSink> GetListIncomingSink() => [];

        public List<IMessageOutgoingSource> GetListOutgoingSource() => this._MessageEngine.GetListOutgoingSource();

        public ValueTask BootAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public ValueTask StartAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public ValueTask ExecuteAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
        
        public Task WaitUntilEmptyAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public ValueTask ShutdownAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public MessageGraphNode ToMessageGraphNode()
            => new(
                nameId,
                this.GetListOutgoingSource().ToListNodeIdentifier(),
                this.GetListIncomingSink().ToListNodeIdentifier(),
                new()
                );

        // IMessageFlowLogging

        public ILogger GetLogger() => this._MessageFlowLogging.GetLogger();

        public void LogSendMessage(NodeIdentifier nameId, FlowMessage message) {
            this._MessageFlowLogging.LogSendMessage(nameId, message);
        }

        public void LogHandleMessage(NodeIdentifier nameId, FlowMessage message) {
            this._MessageFlowLogging.LogHandleMessage(nameId, message);
        }
    }

    public sealed class GlobalIncomingSinkProcessor(
            NodeIdentifier nameId,
            MessageEngine messageEngine,
            IMessageFlowLogging messageFlowLogging
        )
        : DisposableWithState(messageFlowLogging.GetLogger())
        , IMessageProcessor
        , IMessageFlowLogging {
        private readonly MessageEngine _MessageEngine = messageEngine;
        private readonly IMessageFlowLogging _MessageFlowLogging = messageFlowLogging;

        public NodeIdentifier NameId => nameId;

        public List<IMessageIncomingSink> GetListIncomingSink() => this._MessageEngine.GetListIncomingSink();

        public List<IMessageOutgoingSource> GetListOutgoingSource() => [];

        public ValueTask BootAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public ValueTask StartAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public ValueTask ExecuteAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public Task WaitUntilEmptyAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public ValueTask ShutdownAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public MessageGraphNode ToMessageGraphNode()
            => new(
                nameId,
                this.GetListOutgoingSource().ToListNodeIdentifier(),
                this.GetListIncomingSink().ToListNodeIdentifier(),
                new()
                );

        // IMessageFlowLogging

        public ILogger GetLogger() => this._MessageFlowLogging.GetLogger();

        public void LogSendMessage(NodeIdentifier nameId, FlowMessage message) {
            this._MessageFlowLogging.LogSendMessage(nameId, message);
        }

        public void LogHandleMessage(NodeIdentifier nameId, FlowMessage message) {
            this._MessageFlowLogging.LogHandleMessage(nameId, message);
        }
    }
}
