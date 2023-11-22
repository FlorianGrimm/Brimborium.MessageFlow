namespace Brimborium.MessageFlow;

public interface IMessageOutgoingSource
    : IDisposableWithState {
    NodeIdentifier SourceId { get; }

    bool IsConnected { get; }

    void Disconnect();

    ValueTask SendControlAsync(RootMessage message, CancellationToken cancellationToken);
}

public abstract class MessageOutgoingSource
    : DisposableWithState
    , IMessageOutgoingSource {
    protected NodeIdentifier _SourceId;

    protected MessageOutgoingSource(
        NodeIdentifier sourceId,
        ILogger? logger
        ) : base(logger) {
        this._SourceId = sourceId;
    }

    public NodeIdentifier SourceId => _SourceId;

    public abstract bool IsConnected { get; }

    public abstract void Disconnect();

    public abstract ValueTask SendControlAsync(RootMessage message, CancellationToken cancellationToken);
}


public interface IMessageOutgoingSource<T>
    : IMessageOutgoingSource
    where T : RootMessage {

    bool TryGetMessageSinkConnection(
        [MaybeNullWhen(false)] out IMessageConnection<T> connection);

    ValueTask<MessageConnectResult<T>> ConnectAsync(IMessageIncomingSink<T> messageSink, CancellationToken cancellationToken);

    ValueTask SendDataAsync(T message, CancellationToken cancellationToken);
}

public abstract class MessageOutgoingSource<T>
    : MessageOutgoingSource
    , IMessageOutgoingSource<T>
    where T : RootMessage {

    public MessageOutgoingSource(
        NodeIdentifier sourceId,
        ILogger? logger
        ) : base(sourceId, logger) {
    }

    public abstract ValueTask<MessageConnectResult<T>> ConnectAsync(IMessageIncomingSink<T> messageSink, CancellationToken cancellationToken);

    public abstract ValueTask SendDataAsync(T message, CancellationToken cancellationToken);

    public abstract bool TryGetMessageSinkConnection([MaybeNullWhen(false)] out IMessageConnection<T> connection);
}
