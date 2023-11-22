namespace Brimborium.MessageFlow;

public class MessageGobalSink(
        string name,
        IMessageProcessorExamine? messageProcessorExamine,
        ITraceDataService? traceDataService,
        ILogger logger
    )
    : MessageProcessorWithIncomingSink<RootMessage>(
            NodeIdentifier.Create(name),
            "Sink",
            MessageGlobalIncomingSinkFactory.Instance,
            messageProcessorExamine,
            traceDataService,
            logger) {
    protected readonly Dictionary<string, IMessageIncomingSink> _DictSink = new(StringComparer.Ordinal);
    protected Queue<RootMessage> _QueueMessages = new();

    public IMessageIncomingSink<TInput>? GetIncomingSink<TInput>(
            string? name = default
        )
        where TInput : RootMessage {
        var childName = $"{name}-{Internal.TypeNameHelper.GetTypeDisplayNameCached(typeof(TInput))}";
        lock (this._DictSink) {
            if (this._DictSink.TryGetValue(childName, out var result)) {
                if (result is IMessageIncomingSink<TInput> resultT) {
                    return resultT;
                } else {
                    return default;
                }
            } else {
                var resultT = new MessageGlobalIncomingSink<TInput>(
                    this,
                    NodeIdentifier.CreateChild(this.NameId, childName),
                    this.Logger
                    );
                if (this._DictSink.TryAdd(childName, resultT)) {
                    this.StateVersion++;
                    this._ListIncomingSink = this.GetListIncomingSink().ToImmutableArray();
                    return resultT;
                } else {
                    return default;
                }
            }
        }
    }

    public IMessageIncomingSink<TInput> GetIncomingSinkD<TInput>(
            string? name = default
        )
        where TInput : RootMessage
        => this.GetIncomingSink<TInput>(name)
            ?? throw new ArgumentException("Cannot resolve name to sink", nameof(name));

    protected override List<IMessageIncomingSink> GetListIncomingSink()
        => base.GetListIncomingSink()
            .AddRangeIfNotNull(this._DictSink.Values);

    protected Task _TaskExecute = Task.CompletedTask;
    public virtual Task StartAsync(CancellationToken cancellationToken) {
        if (this._TaskExecute.IsCompleted) {
            this._TaskExecute = this.ExecuteLoopAsync(cancellationToken);
        }
        return Task.CompletedTask;
    }

    public virtual Task ExecuteAsync(CancellationToken cancellationToken) {
        if (this._TaskExecute.IsCompleted) {
            this._TaskExecute = this.ExecuteLoopAsync(cancellationToken);
        }
        return this._TaskExecute;
    }

    public void SendControl(RootMessage message) {
        lock (this._QueueMessages) {
            this._QueueMessages.Enqueue(message);
        }
    }

    protected virtual async Task ExecuteLoopAsync(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            RootMessage message;
            lock (this._QueueMessages) {
                if (this._QueueMessages.TryDequeue(out var messageRead)) {
                    message = messageRead;
                } else {
                    this._TaskExecute = Task.CompletedTask;
                    return;
                }
            }
            await this.HandleControlMessageAsync(message, cancellationToken);
        }
    }

    protected override ValueTask HandleDataMessageAsync(RootMessage message, CancellationToken cancellationToken) {
        // Do not foreward this message
        this.ExamineDataMessage(message);
        return ValueTask.CompletedTask;
    }

    protected override ValueTask HandleControlMessageAsync(RootMessage message, CancellationToken cancellationToken) {
        // Do not foreward this message
        this.ExamineControlMessage(message);
        return ValueTask.CompletedTask;
    }
}

public class MessageGlobalIncomingSinkFactory : IMessageProcessorSinkFactory {
    private static MessageGlobalIncomingSinkFactory? _Instance;
    public static MessageGlobalIncomingSinkFactory Instance => _Instance ??= new MessageGlobalIncomingSinkFactory();

    private MessageGlobalIncomingSinkFactory() {
    }

    public IMessageSinkInternal<TInput> Create<TInput>(
        IMessageProcessorWithIncomingSink<TInput> messageProcessor,
        NodeIdentifier nameId,
        ILogger logger)
        where TInput : RootMessage {
        if (messageProcessor is not MessageGobalSink messageGobalSink) {
            throw new ArgumentException("MessageGobalSink expected", nameof(messageProcessor));
        } else {
            return new MessageGlobalIncomingSink<TInput>(messageGobalSink, nameId, logger);
        }
    }
}
public class MessageGlobalIncomingSink<TInput>
    : MessageSink<TInput>
    , IMessageSinkExecution
    where TInput : RootMessage {
    protected MessageGobalSink? _MessageGobalSink;

    public MessageGlobalIncomingSink(
        MessageGobalSink messageGobalSink,
        NodeIdentifier sinkId,
        ILogger logger
        ) : base(sinkId, logger) {
        this._MessageGobalSink = messageGobalSink;
    }

    public override ValueTask<MessageConnectResult<TInput>> ConnectAsync(NodeIdentifier senderId, CancellationToken cancellationToken) {
        ObjectDisposedException.ThrowIf(this.GetIsDisposed(), this);
        var connection = new MessageGlobalConnection<TInput>(
            senderId, this, this.Logger);
        lock (this._ListSource) {
            this._ListSource.Add(new(senderId, new(connection)));
            this.StateVersion++;
        }
        return ValueTask.FromResult(new MessageConnectResult<TInput>(connection, this));
    }

    public override bool Disconnect(IMessageConnection<TInput> messageConnection) {
        lock (this._ListSource) {
            for (var idx = 0; idx < this._ListSource.Count; idx++) {
                var item = this._ListSource[idx];
                if (item.Key.Id == messageConnection.SourceId.Id) {
                    if (item.Value.TryGetTarget(out var target)) {
                        if (ReferenceEquals(target, messageConnection)) {
                            this._ListSource.RemoveAt(idx);
                            messageConnection.Dispose();
                            this.StateVersion++;
                            return true;
                        }
                    } else {
                        this._ListSource.RemoveAt(idx);
                        idx--;
                    }
                }
            }
            messageConnection.Dispose();
            this.StateVersion++;
            return false;
        }
    }

    public override async ValueTask HandleDataMessageAsync(TInput message, CancellationToken cancellationToken) {
        if (this._MessageGobalSink is MessageGobalSink messageGobalSink) {
            messageGobalSink.SendControl(message);
            await messageGobalSink.StartAsync(cancellationToken);
        } else if (this._MessageGobalSink is IMessageProcessorInternal sinkInternal) {
            await sinkInternal.HandleControlMessageAsync(message, cancellationToken);
        }
    }

    public override async ValueTask HandleControlMessageAsync(RootMessage message, CancellationToken cancellationToken) {
        if (this._MessageGobalSink is MessageGobalSink messageGobalSink) {
            messageGobalSink.SendControl(message);
            await messageGobalSink.StartAsync(cancellationToken);
        } else if (this._MessageGobalSink is IMessageProcessorInternal sinkInternal) {
            await sinkInternal.HandleControlMessageAsync(message, cancellationToken);
        }
    }
}

public class MessageGlobalConnection<T>
    : MessageConnection<T>
    where T : RootMessage {
    public MessageGlobalConnection(
        NodeIdentifier sourceId,
        MessageGlobalIncomingSink<T> messageSink,
        ILogger logger) : base(
            sourceId,
            messageSink,
            logger) {
    }

    public override async ValueTask SendDataAsync(T message, CancellationToken cancellationToken) {
        if (this._MessageSink is not null) {
            await this._MessageSink.HandleDataMessageAsync(message, cancellationToken);
        }
    }
    public override async ValueTask SendControlAsync(RootMessage message, CancellationToken cancellationToken) {
        if (this._MessageSink is not null) {
            await this._MessageSink.HandleControlMessageAsync(message, cancellationToken);
        }
    }
}