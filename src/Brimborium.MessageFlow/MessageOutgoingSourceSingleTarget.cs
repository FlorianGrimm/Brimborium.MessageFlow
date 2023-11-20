namespace Brimborium.MessageFlow;

public sealed class MessageOutgoingSourceSingleTarget<T>
    : MessageOutgoingSource<T>
    where T : RootMessage {
    private IMessageConnection<T>? _MessageSinkConnection;

    public MessageOutgoingSourceSingleTarget(
        NodeIdentifier sourceId,
        ILogger logger
        ) : base(
            sourceId: sourceId,
            logger: logger) {
    }

    public override async ValueTask<MessageConnectResult<T>> ConnectAsync(IMessageIncomingSink<T> messageSink, CancellationToken cancellationToken) {
        var result=await messageSink.ConnectAsync(this._SourceId, cancellationToken);
        this._MessageSinkConnection = result.Connection;
        this.Logger.LogMessageSourceConnectSource(this.SourceId, messageSink.SinkId);
        return result;
    }

    public override bool IsConnected => this._MessageSinkConnection is not null;

    public override void Disconnect() {
        var messageSinkConnection = this._MessageSinkConnection;
        this._MessageSinkConnection = null;
        if (messageSinkConnection is not null) { 
            messageSinkConnection.Disconnect();
            if (messageSinkConnection is MessageConnection<T> messageConnection) {
                this.Logger.LogMessageSourceDisconnectSource(this.SourceId, messageConnection.SinkId);
            } else {
                this.Logger.LogMessageSourceDisconnectSource(this.SourceId, default);
            }
        }
    }

    public override bool TryGetMessageSinkConnection(
            [MaybeNullWhen(false)] out IMessageConnection<T> connection) {
        if (this._MessageSinkConnection is null
            || this.GetIsDisposed()) {
            connection = default;
            return false;
        } else {
            connection = this._MessageSinkConnection;
            return true;
        }
    }

    public override ValueTask SendDataAsync(T message, CancellationToken cancellationToken) {
        ObjectDisposedException.ThrowIf(this.GetIsDisposed(), this);
        if (this._MessageSinkConnection is null) {
            throw new InvalidOperationException("MessageSink is not connected");
        }
        {
            return this._MessageSinkConnection.SendDataAsync(message, cancellationToken);
        }
    }

    public override ValueTask SendControlAsync(RootMessage message, CancellationToken cancellationToken) {
        ObjectDisposedException.ThrowIf(this.GetIsDisposed(), this);
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
