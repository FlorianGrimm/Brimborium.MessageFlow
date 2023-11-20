namespace Brimborium.MessageFlow;

public interface IMessageOutgoingSource
    : IDisposableAndCancellation {
    NodeIdentifier SourceId { get; }

    // bool TryGetMessageSinkConnection([MaybeNullWhen(false)] out IMessageEdgeConnection<T> connection);

    bool IsConnected { get; }

    // ValueTask<MessageConnectResult<T>> ConnectAsync(IMessageSink<T> messageSink, CancellationToken cancellationToken);

    void Disconnect();

    // ValueTask SendDataAsync(T message, CancellationToken cancellationToken);
    ValueTask SendControlAsync(RootMessage message, CancellationToken cancellationToken);
}

public abstract class MessageOutgoingSource
    : DisposableAndCancellation
    , IMessageOutgoingSource {
    protected NodeIdentifier _SourceId;

    public MessageOutgoingSource(
        NodeIdentifier sourceId,
        ILogger? logger = default
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
    // IMessageSource
    // NodeIdentifier SourceId { get; }

    bool TryGetMessageSinkConnection(
        [MaybeNullWhen(false)] out IMessageEdgeConnection<T> connection);

    // IMessageSource
    // bool IsConnected { get; }
    ValueTask<MessageConnectResult<T>> ConnectAsync(IMessageIncomingSink<T> messageSink, CancellationToken cancellationToken);

    // IMessageSource
    // void Disconnect();

    ValueTask SendDataAsync(T message, CancellationToken cancellationToken);

    // IMessageSource
    // ValueTask SendControlAsync(RootMessage message, CancellationToken cancellationToken);
}

public abstract class MessageOutgoingSource<T>
    : MessageOutgoingSource
    , IMessageOutgoingSource<T>
    where T : RootMessage {

    public MessageOutgoingSource(
        NodeIdentifier sourceId,
        ILogger? logger = default
        ) : base(sourceId, logger) {
    }

    // public NodeIdentifier SourceId => _SourceId;

    // public abstract bool IsConnected { get; }

    public abstract ValueTask<MessageConnectResult<T>> ConnectAsync(IMessageIncomingSink<T> messageSink, CancellationToken cancellationToken);

    // public abstract void Disconnect();

    public abstract ValueTask SendDataAsync(T message, CancellationToken cancellationToken);

    // public abstract ValueTask SendControlAsync(RootMessage message, CancellationToken cancellationToken);

    public abstract bool TryGetMessageSinkConnection([MaybeNullWhen(false)] out IMessageEdgeConnection<T> connection);
}
