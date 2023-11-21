namespace Brimborium.MessageFlow;

public interface IMessageConnection
    : IDisposableWithState {
    NodeIdentifier SourceId { get; }

    //NodeIdentifier SinkId { get; }

    ValueTask SendControlAsync(RootMessage message, CancellationToken cancellationToken);

    void Disconnect();
}

public interface IMessageConnection<T>
    : IMessageConnection
    where T : RootMessage {

    ValueTask SendDataAsync(T message, CancellationToken cancellationToken);
}

public abstract class MessageConnection<T>
    : DisposableWithState
    , IMessageConnection<T>
    where T : RootMessage {
    protected readonly NodeIdentifier _SourceId;
    protected MessageSink<T>? _MessageSink;

    protected MessageConnection(
        NodeIdentifier sourceId,
        MessageSink<T> messageSink,
        ILogger logger
        ) : base(logger) {
        this._SourceId = sourceId;
        this._MessageSink = messageSink;
    }

    public NodeIdentifier SourceId => this._SourceId;
    public NodeIdentifier? SinkId => this._MessageSink?.SinkId;

    public abstract ValueTask SendDataAsync(T message, CancellationToken cancellationToken);

    public abstract ValueTask SendControlAsync(RootMessage message, CancellationToken cancellationToken);

    public virtual void Disconnect() {
        var messageSink = this._MessageSink;
        if (messageSink is not null) {
            this._MessageSink = null;
            messageSink.Disconnect(this);
        }
    }

    protected override bool Dispose(bool disposing) {
        if (base.Dispose(disposing)) {
            this.Disconnect();
            return true;
        } else {
            return false;
        }
    }
}
