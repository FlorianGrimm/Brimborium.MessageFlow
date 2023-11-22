namespace Brimborium.MessageFlow;

public sealed class MessageOutgoingSourceMultiTarget<T>(
        NodeIdentifier sourceId,
        IMessageProcessorInternal? processorOwner,
        ILogger? logger)
    : MessageOutgoingSource<T>(sourceId, logger)
    , IMessageConnection<T>
    where T : RootMessage {
    private readonly List<IMessageConnection<T>> _ListMessageSinkConnection = new();
    private IMessageProcessorInternal? _ProcessorOwner = processorOwner;

    public override async ValueTask<MessageConnectResult<T>> ConnectAsync(IMessageIncomingSink<T> messageSink, CancellationToken cancellationToken) {
        var result = await messageSink.ConnectAsync(this._SourceId, cancellationToken);
        lock (this._ListMessageSinkConnection) {
            this._ListMessageSinkConnection.Add(result.Connection);
        }
        this.Logger.LogMessageSinkConnectionConnectSource(this.SourceId, messageSink.SinkId);
        this._ProcessorOwner?.OnConnected(result.Connection);
        return result;
    }

    public override bool IsConnected => this._ListMessageSinkConnection.Count > 0;

    public override void Disconnect() {
        IMessageConnection<T>[] listConnectionToDisconnect;
        lock (this) {
            if (this._ListMessageSinkConnection.Count == 0) {
                return;
            }
            listConnectionToDisconnect = this._ListMessageSinkConnection.ToArray();
        }
        foreach (var connection in listConnectionToDisconnect) {
            bool isRemoved;
            lock (this) {
                isRemoved = this._ListMessageSinkConnection.Remove(connection);
            }
            if (isRemoved) {
                if (connection is MessageConnectionChannel<T> messageConnectionChannel) {
                    this.Logger.LogMessageSinkConnectionDisconnectSource(this.SourceId, messageConnectionChannel.SinkId);
                }
                connection.Disconnect();
                this._ProcessorOwner?.OnDisconnect(connection);
            }
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
            if (disposing) {
                this.Disconnect();
            }
            this._ProcessorOwner = null;
            return true;
        } else {
            return false;
        }
    }
}
