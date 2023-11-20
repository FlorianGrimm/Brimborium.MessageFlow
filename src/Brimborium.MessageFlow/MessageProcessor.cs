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

public interface IMessageProcessorWithIncomingSinkInternal
    : IMessageProcessor {
    ValueTask HandleControlMessageAsync(RootMessage message, CancellationToken cancellationToken);
}

public interface IMessageProcessorWithIncomingSinkInternal<TInput>
    : IMessageProcessorWithIncomingSink<TInput>
    , IMessageProcessorWithIncomingSinkInternal
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

public class MessageProcessor
    : DisposableWithState
    , IMessageProcessor {
    protected readonly NodeIdentifier _NameId;

    protected MessageProcessor(
        NodeIdentifier nameId,
        ILogger? logger
        ) : base(logger) {
        this._NameId = nameId;
    }

    public NodeIdentifier NameId => this._NameId;

    public virtual bool CollectCoordinatorNode(HashSet<CoordinatorNode> listTarget) {
        return listTarget.Add(
            new CoordinatorNode(
                this._NameId,
                new(), 
                new(), 
                new()
                )
            );
    }
}