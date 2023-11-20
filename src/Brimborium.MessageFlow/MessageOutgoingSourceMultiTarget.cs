namespace Brimborium.MessageFlow;

public sealed class MessageOutgoingSourceMultiTarget<T>(
        NodeIdentifier sourceId,
        ILogger? logger)
    : MessageOutgoingSource<T>(
        sourceId: sourceId,
        logger: logger
    ), IMessageConnection<T>
    where T : RootMessage {
    private readonly List<IMessageConnection<T>> _ListMessageSinkConnection = new();

    public override async ValueTask<MessageConnectResult<T>> ConnectAsync(IMessageIncomingSink<T> messageSink, CancellationToken cancellationToken) {
        var result = await messageSink.ConnectAsync(this._SourceId, cancellationToken);
        lock (this._ListMessageSinkConnection) {
            this._ListMessageSinkConnection.Add(result.Connection);
        }
        this.Logger.LogMessageSinkConnectionConnectSource(this.SourceId, messageSink.SinkId);
        return result;
    }

    public override bool IsConnected => this._ListMessageSinkConnection.Count > 0;

    public override void Disconnect() {
        List<IMessageConnection<T>> listConnectionToDisconnect;
        lock (this) {
            if (this._ListMessageSinkConnection.Count == 0) {
                return;
            }
            listConnectionToDisconnect = new(this._ListMessageSinkConnection);
            this._ListMessageSinkConnection.Clear();
        }
        foreach (var connection in listConnectionToDisconnect) {
            if (connection is MessageConnectionChannel<T> messageConnectionChannel) {
                this.Logger.LogMessageSinkConnectionDisconnectSource(this.SourceId, messageConnectionChannel.SinkId);
            }
            connection.Disconnect();
        }
    }

    public override bool TryGetMessageSinkConnection(
            [MaybeNullWhen(false)] out IMessageConnection<T> connection) {
        if (this._ListMessageSinkConnection.Count == 0
            || this.GetIsDisposed()) {
            connection = default;
            return false;
        } else {
            connection = this;
            return true;
        }
    }

    public override async ValueTask SendDataAsync(T message, CancellationToken cancellationToken) {
        ObjectDisposedException.ThrowIf(this.GetIsDisposed(), this);
        List<IMessageConnection<T>> listConnection;
        lock (this) {
            if (this._ListMessageSinkConnection.Count == 0) {
                return;
            }
            listConnection = new(this._ListMessageSinkConnection);
        }
        foreach (var connection in listConnection) {
            try {
                await connection.SendDataAsync(message, cancellationToken);
            } catch (System.Exception error) {
                if (connection is MessageConnection<T> messageConnection) {
                    this.Logger.LogMessageSourceSendData(error, this._SourceId, messageConnection.SinkId ?? NodeIdentifier.Unknown, message.ToRootMessageLog());
                } else {
                    this.Logger.LogMessageSourceSendControl(error, this._SourceId, NodeIdentifier.Unknown, message.ToRootMessageLog());
                }
            }
        }
    }

    public override async ValueTask SendControlAsync(RootMessage message, CancellationToken cancellationToken) {
        ObjectDisposedException.ThrowIf(this.GetIsDisposed(), this);
        List<IMessageConnection<T>> listConnection;
        lock (this) {
            if (this._ListMessageSinkConnection.Count == 0) {
                return;
            }
            listConnection = new(this._ListMessageSinkConnection);
        }
        foreach (var connection in listConnection) {
            await connection.SendControlAsync(message, cancellationToken);
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
