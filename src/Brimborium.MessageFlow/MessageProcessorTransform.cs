namespace Brimborium.MessageFlow;

public abstract class MessageProcessorTransform<TIncomingSink, TOutgoingSource>
    : MessageProcessor
    where TIncomingSink : FlowMessage
    where TOutgoingSource : FlowMessage {

    protected Channel<FlowMessage>? _Channel;
    protected IMessageIncomingSink<TIncomingSink>? _IncomingSink;
    protected IMessageOutgoingSource<TOutgoingSource>? _OutgoingSource;
    protected CancellationTokenSource? _ExecuteCTS;
    protected Task _TaskExecuteLoop = Task.CompletedTask;
    protected ChannelReader<FlowMessage>? _ChannelReader;
    protected TaskCompletionSource? _IsEmptyTCS;

    public MessageProcessorTransform(
        NodeIdentifier nameId,
        IMessageFlowLogging messageFlowLogging
    ) : base(nameId, messageFlowLogging) {
        this._Channel = Channel.CreateUnbounded<FlowMessage>();
        this._IncomingSink = new MessageIncomingSink<TIncomingSink>(nameof(this.IncomingSink), this, this._Channel.Writer.WriteAsync);
        this._OutgoingSource = new MessageOutgoingSource<TOutgoingSource>(nameof(this.OutgoingSource), this);
        this._ExecuteCTS = default;
    }

    public IMessageIncomingSink<TIncomingSink> IncomingSink => this._IncomingSink ?? throw new ObjectDisposedException(TypeNameHelper.GetTypeDisplayName(this));

    public IMessageOutgoingSource<TOutgoingSource> OutgoingSource => this._OutgoingSource ?? throw new ObjectDisposedException(TypeNameHelper.GetTypeDisplayName(this));

    public override List<IMessageIncomingSink> GetListIncomingSink() => new List<IMessageIncomingSink>().AddValueIfNotNull(this._IncomingSink);

    public override List<IMessageOutgoingSource> GetListOutgoingSource() => new List<IMessageOutgoingSource>().AddValueIfNotNull(this._OutgoingSource);

    public override ValueTask StartAsync(CancellationToken cancellationToken) {
        if (this._TaskExecuteLoop.IsCompleted) {
            if (this._ExecuteCTS is null) {
                this._ExecuteCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            }
            this._TaskExecuteLoop = this.ExecuteLoopAsync(this._ExecuteCTS.Token);
        }
        return ValueTask.CompletedTask;
    }

    public override async ValueTask ExecuteAsync(CancellationToken cancellationToken) {
        await this._TaskExecuteLoop.WaitAsync(this._ExecuteCTS?.Token ?? CancellationToken.None).ConfigureAwait(false);
    }

    protected virtual async Task ExecuteLoopAsync(CancellationToken cancellationToken) {
        this._ChannelReader = this._Channel?.Reader;
        while (!cancellationToken.IsCancellationRequested) {
            var channelReader = this._ChannelReader;
            if (channelReader is null) {
                break;
            } else {
                if (!channelReader.TryRead(out var message)) {
                    if (this._IsEmptyTCS is not null) {
                        lock (this) {
                            var isEmptyTCS = this._IsEmptyTCS;
                            if (isEmptyTCS is not null) {
                                isEmptyTCS.SetResult();
                                this._IsEmptyTCS = null;
                            }
                        }
                    }
                    try {
                        if (await channelReader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                            // loop
                        } else {
                            // fini
                            break;
                        }
                    } catch (OperationCanceledException) {
                        this._ChannelReader = null;
                    }
                } else {
                    this._MessageFlowLogging.LogHandleMessage(this._NameId, message);
                    await this.HandleMessageAsync(message, cancellationToken);
                }
            }
        }
    }

    public override Task WaitUntilEmptyAsync(CancellationToken cancellationToken) {
        if (this._Channel is not null) {
            if (this._Channel.Reader.Count == 0) {
                return Task.CompletedTask;
            } else {
                lock (this) {
                    var isEmptyTCS = this._IsEmptyTCS ??= new TaskCompletionSource();
                    return isEmptyTCS.Task;
                }
            }
        } else {
            return Task.CompletedTask;
        }
    }

    public override async ValueTask ShutdownAsync(CancellationToken cancellationToken) {
        this._Channel = default;
        var executeCTS = this._ExecuteCTS;
        if (executeCTS is not null) {
            await executeCTS.CancelAsync();
        }
    }

    protected virtual async ValueTask<bool> HandleMessageAsync(FlowMessage message, CancellationToken cancellationToken) {
        ObjectDisposedException.ThrowIf(this.GetIsDisposed() || this._OutgoingSource is null, this);

        if (message is TIncomingSink incomingSink) {
            return await this.HandleDataAsync(incomingSink, cancellationToken);
        }

        return false;
    }

    protected virtual ValueTask<bool> HandleDataAsync(TIncomingSink message, CancellationToken cancellationToken) {
        return ValueTask.FromResult<bool>(false);
    }

    protected override bool Dispose(bool disposing) {
        if (base.Dispose(disposing)) {
            using (var cts = this._ExecuteCTS) {
                if (disposing) {
                    this._ExecuteCTS = null;
                    this._Channel = null;
                }
            }
            return true;
        } else {
            return false;
        }
    }
}

public abstract class MessageProcessorTransformGrouping<TIncomingSink, TGroupValue, TOutgoingSource>
    : MessageProcessorTransform<TIncomingSink, TOutgoingSource>
    where TIncomingSink : FlowMessage
    where TOutgoingSource : FlowMessage {
    private readonly Dictionary<long, TGroupValue> _DictGroup;

    protected MessageProcessorTransformGrouping(NodeIdentifier nameId, IMessageFlowLogging messageFlowLogging) : base(nameId, messageFlowLogging) {
        this._DictGroup = new Dictionary<long, TGroupValue>();
    }

    protected override ValueTask<bool> HandleDataAsync(TIncomingSink message, CancellationToken cancellationToken) {
        var groupId = message.MessageId.GroupId;
        if (this._DictGroup.TryGetValue(groupId, out var groupValue)) {
            this.GroupDataAddValue(message, groupValue);
        } else {
            // TODO: error?
            groupValue = this.GroupStartGetInitialValue(default);
            this._DictGroup.Add(groupId, groupValue);
        }

        return ValueTask.FromResult<bool>(true);
    }

    protected override async ValueTask<bool> HandleMessageAsync(FlowMessage message, CancellationToken cancellationToken) {
        if (await base.HandleMessageAsync(message, cancellationToken)) { return true; }
        if (await this.HandleMessageGroupStartEnd(message, cancellationToken)) { return true; }
        return true;
    }

    protected virtual async ValueTask<bool> HandleMessageGroupStartEnd(FlowMessage message, CancellationToken cancellationToken) {
        if (message is MessageGroupStart messageGroupStart) {
            var groupId = message.MessageId.GroupId;
            if (this._DictGroup.TryGetValue(groupId, out var groupValue)) {
                // TODO: error?
            } else {
                groupValue = this.GroupStartGetInitialValue(messageGroupStart);
                this._DictGroup.Add(groupId, groupValue);
            }
            return true;
        }
        if (message is MessageGroupEnd messageGroupEnd) {
            var groupId = message.MessageId.GroupId;
            if (this._DictGroup.TryGetValue(groupId, out var groupValue)) {
                this._DictGroup.Remove(groupId);
                await this.GroupEndHandleValueAsync(messageGroupEnd, groupValue, cancellationToken);
            } else {
                // TODO: error?
            }
            return true;
        }
        return false;
    }

    protected abstract TGroupValue GroupStartGetInitialValue(MessageGroupStart? messageGroupStart);
    protected abstract void GroupDataAddValue(TIncomingSink message, TGroupValue groupValue);
    protected abstract ValueTask GroupEndHandleValueAsync(MessageGroupEnd messageGroupEnd, TGroupValue groupValue, CancellationToken cancellationToken);
}