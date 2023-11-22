namespace Brimborium.MessageFlow;

public interface IMessageIncomingSink
    : IDisposableWithState {
    NodeIdentifier SinkId { get; }

    CoordinatorNodeSink GetCoordinatorNodeSink();
}

public interface IMessageIncomingSink<TInput>
    : IMessageIncomingSink
    where TInput : RootMessage {

    ValueTask<MessageConnectResult<TInput>> ConnectAsync(
        NodeIdentifier senderId,
        CancellationToken cancellationToken);
}

public record MessageConnectResult<TValue>(
    IMessageConnection<TValue> Connection,
    IMessageSinkExecution MessageSinkExecution
    ) where TValue : RootMessage;

public interface IMessageSinkInternal<TInput>
    : IMessageIncomingSink<TInput>
    where TInput : RootMessage {

    bool Disconnect(
        IMessageConnection<TInput> messageConnection);
}

public abstract class MessageSink<TInput>(
        NodeIdentifier sinkId,
        ILogger logger
    )
    : DisposableWithState(logger)
    , IMessageIncomingSink<TInput>
    , IMessageSinkInternal<TInput>
    , IMessageSinkExecution
    where TInput : RootMessage {
    protected NodeIdentifier _SinkId = sinkId;
    // protected Task _ExecuteTask = Task.CompletedTask;
    protected readonly List<KeyValuePair<NodeIdentifier, WeakReference<IMessageConnection<TInput>>>> _ListSource = [];

    public NodeIdentifier SinkId => this._SinkId;

    public abstract ValueTask<MessageConnectResult<TInput>> ConnectAsync(NodeIdentifier senderId, CancellationToken cancellationToken);

    public abstract bool Disconnect(IMessageConnection<TInput> messageConnection);


    public virtual Task StartAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public virtual Task ExecuteAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public virtual ValueTask HandleDataMessageAsync(TInput message, CancellationToken cancellationToken) 
        => ValueTask.CompletedTask;

    public virtual ValueTask HandleControlMessageAsync(RootMessage message, CancellationToken cancellationToken) 
        => ValueTask.CompletedTask;

    public virtual CoordinatorNodeSink GetCoordinatorNodeSink() {
        List<NodeIdentifier> listSourceId = [];
        lock (this._ListSource) {
            foreach (var item in this._ListSource) {
                if (item.Value.TryGetTarget(out var connection)) {
                    var sourceId = connection.SourceId;
                    if (sourceId.Id > 0) { 
                        listSourceId.Add(sourceId);
                    }
                }
            }
        }
        return new CoordinatorNodeSink(
            this._SinkId,
            listSourceId
        );
    }
}
