namespace Brimborium.MessageFlow;

public sealed class MessageConnectionChannel<T>
    : MessageConnection<T>
    where T : RootMessage {
    private ChannelWriter<RootMessage>? _Writer;

    public MessageConnectionChannel(
        NodeIdentifier sourceId,
        MessageSink<T> messageSink,
        ChannelWriter<RootMessage> writer,
        ILogger logger
        )
        : base(
            sourceId: sourceId,
            messageSink: messageSink,
            logger: logger
            ) {
        this._Writer = writer;
    }

    public override async ValueTask SendDataAsync(T message, CancellationToken cancellationToken) {
        var messageSink = this._MessageSink;
        var writer = this._Writer;
        ObjectDisposedException.ThrowIf(
            this.GetIsDisposed()
            || messageSink is null
            || writer is null,
            this
            );

        T messageToSend;
        if (message.SourceId.Id != this._SourceId.Id) {
            messageToSend = message with { SourceId = this._SourceId };
        } else {
            messageToSend = message;
        }
        this.Logger.LogMessageConnectionSendData(this._SourceId, messageSink.SinkId, messageToSend.ToRootMessageLog());
        if (!writer.TryWrite(messageToSend)) {
            await writer.WriteAsync(messageToSend, cancellationToken);
        }
    }

    public override async ValueTask SendControlAsync(RootMessage message, CancellationToken cancellationToken) {
        var messageSink = this._MessageSink;
        var writer = this._Writer;
        ObjectDisposedException.ThrowIf(this.GetIsDisposed()
            || messageSink is null
            || writer is null
            , this);
        RootMessage messageToSend = (message.SourceId.Id == this._SourceId.Id)
            ? message
            : message with { SourceId = this._SourceId };
        this.Logger.LogMessageConnectionSendControl(this._SourceId, messageSink.SinkId, messageToSend.ToRootMessageLog());
        if (!writer.TryWrite(messageToSend)) {
            await writer.WriteAsync(messageToSend, cancellationToken);
        }
    }

    public override void Disconnect() {
        this._Writer = null;
        base.Disconnect();
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