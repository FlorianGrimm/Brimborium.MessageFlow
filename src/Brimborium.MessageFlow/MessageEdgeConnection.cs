using Brimborium.MessageFlow.Disposable;
using Brimborium.MessageFlow.Internal;

namespace Brimborium.MessageFlow;

public interface IMessageEdgeConnection<T>
    : IDisposableAndCancellation
    where T : RootMessage {
    NodeIdentifier SourceId { get; }

    ValueTask SendDataAsync(T message, CancellationToken cancellationToken);

    ValueTask SendControlAsync(RootMessage message, CancellationToken cancellationToken);

    void Disconnect();
}

public class MessageEdgeConnection<T>
    : DisposableAndCancellation
    , IMessageEdgeConnection<T>
    where T : RootMessage {
    private readonly ILogger _Logger;
    private readonly NodeIdentifier _SourceId;
    private MessageSink<T>? _MessageSink;
    private ChannelWriter<RootMessage>? _Writer;

    public MessageEdgeConnection(
        ILogger logger,
        NodeIdentifier sourceId,
        MessageSink<T> messageSink,
        ChannelWriter<RootMessage> writer) {
        this._Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._SourceId = sourceId;
        this._MessageSink = messageSink;
        this._Writer = writer;
    }

    public NodeIdentifier SourceId => this._SourceId;
    public NodeIdentifier? SinkId => this._MessageSink?.SinkId;

    public async ValueTask SendDataAsync(T message, CancellationToken cancellationToken) {
        var messageSink = this._MessageSink;
        var writer = this._Writer;
        if (this.GetIsDisposed()
            || messageSink is null
            || writer is null
            ) {
            throw new ObjectDisposedException("MessageEdgeConnection");
        }

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

    public async ValueTask SendControlAsync(RootMessage message, CancellationToken cancellationToken) {
        var messageSink = this._MessageSink;
        var writer = this._Writer;
        if (this.GetIsDisposed()
            || messageSink is null
            || writer is null
            ) {
            throw new ObjectDisposedException("MessageEdgeConnection");
        }
        RootMessage messageToSend;
        if (message.SourceId.Id != this._SourceId.Id) {
            messageToSend = message with { SourceId = this._SourceId };
        } else {
            messageToSend = message;
        }
        this.Logger.LogMessageConnectionSendControl(this._SourceId, messageSink.SinkId, messageToSend.ToRootMessageLog());
        if (!writer.TryWrite(messageToSend)) {
            await writer.WriteAsync(messageToSend, cancellationToken);
        }
    }

    public void Disconnect() {
        var messageSink = this._MessageSink;
        this._Writer = null;
        this._MessageSink = null;
        messageSink?.Disconnect(this);
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
