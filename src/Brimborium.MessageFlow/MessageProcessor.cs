#pragma warning disable IDE0031 // Use null propagation

namespace Brimborium.MessageFlow;

public interface IMessageProcessor
    : IDisposableWithState
    , IWithCoordinatorNode {
    NodeIdentifier NameId { get; }
}

public interface IMessageProcessorWithIncomingSink<TInput>
    : IMessageProcessor
    where TInput : RootMessage {
    IMessageIncomingSink<TInput>? IncomingSink { get; }
    IMessageIncomingSink<TInput> IncomingSinkD { get; }
}

public interface IMessageProcessorInternal
    : IMessageProcessor {
    ValueTask HandleControlMessageAsync(RootMessage message, CancellationToken cancellationToken);

    void OnConnected(IMessageConnection connection);

    void OnDisconnect(IMessageConnection connection);
}

public interface IMessageProcessorWithIncomingSinkInternal<TInput>
    : IMessageProcessorWithIncomingSink<TInput>
    , IMessageProcessorInternal
    where TInput : RootMessage {
    ValueTask HandleDataMessageAsync(TInput message, CancellationToken cancellationToken);
}

public interface IMessageProcessorWithOutgoingSource<TOutput>
    : IMessageProcessor
    where TOutput : RootMessage {
    IMessageOutgoingSource<TOutput>? OutgoingSource { get; }
    IMessageOutgoingSource<TOutput> OutgoingSourceD { get; }
}

public interface IMessageProcessor<TInput, TOutput>
    : IMessageProcessorWithIncomingSink<TInput>
    , IMessageProcessorWithOutgoingSource<TOutput>
    where TInput : RootMessage
    where TOutput : RootMessage {
}

public class MessageProcessor(
        NodeIdentifier nameId,
        IMessageProcessorExamine? messageProcessorExamine,
        ITraceDataService? traceDataService,
        ILogger? logger
        )
    : DisposableWithState(logger)
    , IMessageProcessor
    , IMessageProcessorInternal {
    private static readonly ImmutableArray<IMessageIncomingSink> _ListIncomingSinkEmpty = [];
    private static readonly ImmutableArray<IMessageOutgoingSource> _ListOutgoingSourceEmpty = [];

    protected readonly NodeIdentifier _NameId = nameId;
    protected readonly string _NameIdToString = nameId.ToString();
    protected readonly ITraceDataService? _TraceDataService = traceDataService;

    protected ImmutableArray<IMessageIncomingSink> _ListIncomingSink = _ListIncomingSinkEmpty;
    protected ImmutableArray<IMessageOutgoingSource> _ListOutgoingSource = _ListOutgoingSourceEmpty;

    protected MessageProcessorExamine _MessageProcessorExamine = new(messageProcessorExamine, logger ?? DummyILogger.Instance);

    public NodeIdentifier NameId => this._NameId;

    public virtual bool CollectCoordinatorNode(HashSet<CoordinatorNode> listTarget)
        => listTarget.Add(
            new CoordinatorNode(
                this._NameId,
                CoordinatorCollector.ToListCoordinatorNodeSourceId(this._ListOutgoingSource),
                CoordinatorCollector.ToListCoordinatorNodeSink(this._ListIncomingSink),
                new()
                )
            );

    protected virtual List<IMessageIncomingSink> GetListIncomingSink()
      => new();

    protected virtual List<IMessageOutgoingSource> GetListOutgoingSource()
        => new();

    void IMessageProcessorInternal.OnConnected(IMessageConnection connection)
        => this.OnConnected(connection);

    protected virtual void OnConnected(IMessageConnection connection) {
#warning TODO
        //connection.SourceId
    }

    ValueTask IMessageProcessorInternal.HandleControlMessageAsync(RootMessage message, CancellationToken cancellationToken)
        => this.HandleControlMessageAsync(message, cancellationToken);

    protected virtual async ValueTask HandleControlMessageAsync(RootMessage message, CancellationToken cancellationToken) {
        this.TraceData(message);
        await this.ForwardControlMessageAsync(message, cancellationToken);
    }

    protected virtual void TraceData(RootMessage message) {
        var traceDataService = this._TraceDataService;
        if (traceDataService is not null) {
            traceDataService.TraceData(this._NameIdToString, message.MessageId);
        }
    }

    protected virtual async ValueTask ForwardControlMessageAsync(
        RootMessage message,
        CancellationToken cancellationToken) {
        var listOutgoingSource = this._ListOutgoingSource;
        if (listOutgoingSource.IsDefaultOrEmpty) {
            //
        } else {
            var messageAction = this.ExamineControlMessage(message);
            foreach (var messageOutgoingSource in listOutgoingSource) {
                try {
                    await messageOutgoingSource.SendControlAsync(message, cancellationToken);
                } catch (Exception error) {
                    this.Logger.LogMessageProcessorForwardControlFailed(error, message.ToRootMessageLog());
                }
            }
            await this.HandleMessageActionAsync(messageAction, cancellationToken);
        }
    }

    protected virtual MessageAction ExamineDataMessage(RootMessage message) {
        MessageAction messageAction = this._MessageProcessorExamine.ExamineDataMessage(message, message.GetMessageAction());
        return messageAction;
    }

    protected virtual MessageAction ExamineControlMessage(RootMessage message) {
        MessageAction messageAction = this._MessageProcessorExamine.ExamineControlMessage(message, message.GetMessageAction());
        //if (messageAction == MessageAction.FlowStart) { }
        //if (messageAction == MessageAction.FlowEnd) { }
        return messageAction;
    }

    protected virtual async ValueTask HandleMessageActionAsync(MessageAction messageAction, CancellationToken cancellationToken) {
        if ((messageAction & MessageAction.Disconnect) == MessageAction.Disconnect) {
            await this.DisconnectAsync(cancellationToken);
        }

    }

    protected virtual ValueTask DisconnectAsync(CancellationToken cancellationToken) {
        //this._ListIncomingSink[0].Disconnect()
        //this._ListOutgoingSource[0].Disconnect()
        return ValueTask.CompletedTask;
    }

}

public interface IMessageProcessorExamine {
    MessageAction ExamineDataMessage(RootMessage message, MessageAction messageAction);

    MessageAction ExamineControlMessage(RootMessage message, MessageAction messageAction);
}

public readonly struct MessageProcessorExamine(
    IMessageProcessorExamine? messageProcessorExamine,
    ILogger logger
    )
    : IMessageProcessorExamine {
    private readonly ILogger _Logger = logger;
    private readonly IMessageProcessorExamine? _MessageProcessorExamine = messageProcessorExamine;
    private readonly Dictionary<long, RootMessage> _DictMessageGroup = new();

    //public MessageProcessorExamine(
    //    IMessageProcessorExamine? messageProcessorExamine
    //    ) {
    //    this._MessageProcessorExamine = messageProcessorExamine;
    //}

    public MessageAction ExamineDataMessage(RootMessage message, MessageAction messageAction) {
        var groupId = message.MessageId.GroupId;
        if (groupId > 0) {
            if (this._DictMessageGroup.ContainsKey(groupId)) {
                //
            } else {
                // error
                this._Logger.LogError("unexpected group {groupId} {message}", groupId, message);
            }
        }
        if (this._MessageProcessorExamine is not null) {
            messageAction = this._MessageProcessorExamine.ExamineDataMessage(message, messageAction);
        }
        return messageAction;
    }

    public MessageAction ExamineControlMessage(RootMessage message, MessageAction messageAction) {
        var groupId = message.MessageId.GroupId;
        if (groupId > 0) {
            if (message is MessageGroupStart messageGroupStart) {
                if (groupId > 0) {
                    if (this._DictMessageGroup.TryGetValue(groupId, out var oldGroupMessage)) {
                        // oldGroupMessage
                        this._Logger.LogError("unexpected group {groupId} {oldGroupMessage}", groupId, oldGroupMessage);
                    } else {
                        this._Logger.LogDebug("group {groupId} starts {messageGroupStart}", groupId, messageGroupStart);
                        this._DictMessageGroup.TryAdd(groupId, messageGroupStart);
                    }
                }
            } else if (message is MessageGroupEnd messageGroupEnd) {
                if (groupId > 0) {
                    if (this._DictMessageGroup.Remove(groupId)) {
                        // OK
                        this._Logger.LogDebug("group {groupId} ends {messageGroupEnd}", groupId, messageGroupEnd);
                    } else {
                        // error
                        this._Logger.LogError("TODO {groupId} {messageGroupEnd}", groupId, messageGroupEnd);
                    }
                }
            } else {
                if (this._DictMessageGroup.ContainsKey(groupId)) {
                    //
                } else {
                    // error
                    this._Logger.LogError("unexpected group {groupId} {message}", groupId, message);
                }
            }
        }

        if (this._MessageProcessorExamine is not null) {
            messageAction = this._MessageProcessorExamine.ExamineDataMessage(message, messageAction);
        }

        return messageAction;
    }
}