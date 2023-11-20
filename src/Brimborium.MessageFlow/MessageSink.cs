namespace Brimborium.MessageFlow;

public interface IMessageIncomingSink
    : IDisposableAndCancellation {
    NodeIdentifier SinkId { get; }

    CoordinatorNodeSink GetCoordinatorNodeSink();
}

public interface IMessageIncomingSink<T>
    : IMessageIncomingSink
    where T : RootMessage {

    ValueTask<MessageConnectResult<T>> ConnectAsync(
        NodeIdentifier senderId,
        CancellationToken cancellationToken);
}

public record MessageConnectResult<T>(
    IMessageEdgeConnection<T> Connection,
    IMessageSinkExecution MessageSinkExecution
    ) where T : RootMessage;

public interface IMessageSinkInternal<T>
    : IMessageIncomingSink<T>
    where T : RootMessage {

    bool Disconnect(
        IMessageEdgeConnection<T> messageEdgeConnection);
}

public abstract class MessageSink<T>
    : DisposableAndCancellation
    , IMessageIncomingSink<T>
    , IMessageSinkInternal<T>
    , IMessageSinkExecution
    where T : RootMessage {
    protected NodeIdentifier _SinkId;
    protected CancellationTokenSource? _ExecuteTokenSource;
    protected Task _ExecuteTask = Task.CompletedTask;
    protected readonly List<KeyValuePair<NodeIdentifier, WeakReference<IMessageEdgeConnection<T>>>> _ListSource;

    public MessageSink(
        NodeIdentifier sinkId,
        ILogger logger
        ) : base(logger) {
        this._SinkId = sinkId;
        this._ListSource = new ();
    }

    public NodeIdentifier SinkId => this._SinkId;

    public abstract ValueTask<MessageConnectResult<T>> ConnectAsync(NodeIdentifier senderId, CancellationToken cancellationToken);

    public abstract bool Disconnect(IMessageEdgeConnection<T> messageEdgeConnection);


    public virtual Task StartAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public virtual Task ExecuteAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;


    public virtual ValueTask HandleDataMessageAsync(T message, CancellationToken cancellationToken) {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask HandleControlMessageAsync(RootMessage message, CancellationToken cancellationToken) {
        return ValueTask.CompletedTask;
    }

    public virtual CoordinatorNodeSink GetCoordinatorNodeSink() {
        List<NodeIdentifier> listSourceId = new();
        lock (this._ListSource) {
            foreach (var item in this._ListSource) {
                if (item.Value.TryGetTarget(out var connection)) {
                    listSourceId.Add(connection.SourceId);
                }
            }
        }
        return new CoordinatorNodeSink(
            this._SinkId,
            listSourceId
        );
    }
}
