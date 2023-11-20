namespace Brimborium.MessageFlow;

public class MessageSourceMultiTarget<T>
    : DisposableAndCancellation
    , IMessageOutgoingSource<T>
    , IMessageEdgeConnection<T>
    where T : RootMessage {
    private readonly ILogger _Logger;
    private readonly NodeIdentifier _SourceId;
    private readonly List<IMessageEdgeConnection<T>> _ListMessageSinkConnection;

    public MessageSourceMultiTarget(
        NodeIdentifier sourceId,
        ILogger logger
        ) : base(logger) {
        this._Logger = logger;
        this._SourceId = sourceId;
        this._ListMessageSinkConnection = new List<IMessageEdgeConnection<T>>();
    }

    public NodeIdentifier SourceId => this._SourceId;

    public async ValueTask<MessageConnectResult<T>> ConnectAsync(IMessageIncomingSink<T> messageSink, CancellationToken cancellationToken) {
        var result = await messageSink.ConnectAsync(this._SourceId, cancellationToken);
        lock (this._ListMessageSinkConnection) {
            this._ListMessageSinkConnection.Add(result.Connection);
        }
        this._Logger.LogMessageSinkConnectionConnectSource(this.SourceId, messageSink.SinkId);
        return result;
    }

    public bool IsConnected => this._ListMessageSinkConnection.Count > 0;

    public void Disconnect() {
        List<IMessageEdgeConnection<T>> listConnectionToDisconnect;
        lock (this) {
            if (this._ListMessageSinkConnection.Count == 0) {
                return;
            }
            listConnectionToDisconnect = new(this._ListMessageSinkConnection);
            this._ListMessageSinkConnection.Clear();
        }
        foreach (var connection in listConnectionToDisconnect) {
            if (connection is MessageEdgeConnection<T> messageEdgeConnection) {
                this._Logger.LogMessageSinkConnectionDisconnectSource(this.SourceId, messageEdgeConnection.SinkId);
            }
            connection.Disconnect();
        }
    }

    public bool TryGetMessageSinkConnection(
            [MaybeNullWhen(false)] out IMessageEdgeConnection<T> connection) {
        if (this._ListMessageSinkConnection.Count == 0
            || this.GetIsDisposed()) {
            connection = default;
            return false;
        } else {
            connection = this;
            return true;
        }
    }

    public async ValueTask SendDataAsync(T message, CancellationToken cancellationToken) {
        if (this.GetIsDisposed()) {
            throw new ObjectDisposedException(this.GetType().Name);
        }
        List<IMessageEdgeConnection<T>> listConnection;
        lock (this) {
            if (this._ListMessageSinkConnection.Count == 0) {
                return;
            }
            listConnection = new(this._ListMessageSinkConnection);
        }
        foreach (var connection in listConnection) {
            try {
                await connection.SendDataAsync(message, cancellationToken);
            } catch (System.Exception error){
                if (connection is MessageEdgeConnection<T> edgeConnection) {
                    this._Logger.LogMessageSourceSendData(error, this._SourceId, edgeConnection.SinkId ?? NodeIdentifier.Unknown, message.ToRootMessageLog());
                } else { 
                    this._Logger.LogMessageSourceSendControl(error, this._SourceId, NodeIdentifier.Unknown, message.ToRootMessageLog());
                }
            }
        }
    }

    public async ValueTask SendControlAsync(RootMessage message, CancellationToken cancellationToken) {
        if (this.GetIsDisposed()) {
            throw new ObjectDisposedException(this.GetType().Name);
        }
        List<IMessageEdgeConnection<T>> listConnection;
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
