using Brimborium.MessageFlow.Disposable;

namespace Brimborium.MessageFlow;

public class MessageSourceSingleTarget<T>
    : DisposableAndCancellation
    , IMessageOutgoingSource<T>
    where T : RootMessage {
    private readonly NodeIdentifier _SourceId;
    private readonly ILogger _Logger;
    private IMessageEdgeConnection<T>? _MessageSinkConnection;

    public MessageSourceSingleTarget(
        NodeIdentifier senderId,
        ILogger logger
        ) : base(logger) {
        this._SourceId = senderId;
        this._Logger = logger;
    }

    public NodeIdentifier SourceId => this._SourceId;

    public async ValueTask<MessageConnectResult<T>> ConnectAsync(IMessageIncomingSink<T> messageSink, CancellationToken cancellationToken) {
        var result=await messageSink.ConnectAsync(this._SourceId, cancellationToken);
        this._MessageSinkConnection = result.Connection;
        this._Logger.LogDebug("Connect Source:{SourceId} Sink:{SinkId}", this.SourceId, messageSink.SinkId);
        this._Logger.LogMessageSourceConnectSource(this.SourceId, messageSink.SinkId);
        return result;
    }

    public bool IsConnected => this._MessageSinkConnection is not null;

    public void Disconnect() {
        var messageSinkConnection = this._MessageSinkConnection;
        this._MessageSinkConnection = null;
        if (messageSinkConnection is not null) { 
            messageSinkConnection.Disconnect();
            if (messageSinkConnection is MessageEdgeConnection<T> messageEdgeConnection) {
                this._Logger.LogMessageSourceDisconnectSource(this.SourceId, messageEdgeConnection.SinkId);
            } else {
                this._Logger.LogMessageSourceDisconnectSource(this.SourceId, default);
            }
        }
    }

    public bool TryGetMessageSinkConnection(
            [MaybeNullWhen(false)] out IMessageEdgeConnection<T> connection) {
        if (this._MessageSinkConnection is null
            || this.GetIsDisposed()) {
            connection = default;
            return false;
        } else {
            connection = this._MessageSinkConnection;
            return true;
        }
    }

    public ValueTask SendDataAsync(T message, CancellationToken cancellationToken) {
        if (this.GetIsDisposed()) {
            throw new ObjectDisposedException(this.GetType().Name);
        }
        if (this._MessageSinkConnection is null) {
            throw new InvalidOperationException("MessageSink is not connected");
        }
        {
            return this._MessageSinkConnection.SendDataAsync(message, cancellationToken);
        }
    }

    public ValueTask SendControlAsync(RootMessage message, CancellationToken cancellationToken) {
        if (this.GetIsDisposed()) {
            throw new ObjectDisposedException(this.GetType().Name);
        }
        if (this._MessageSinkConnection is null) {
            throw new InvalidOperationException("MessageSink is not connected");
        }
        {
            return this._MessageSinkConnection.SendControlAsync(message, cancellationToken);
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
