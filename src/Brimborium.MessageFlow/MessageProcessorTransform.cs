﻿
namespace Brimborium.MessageFlow;

public abstract class MessageProcessorTransform<TIncomingSink, TOutgoingSource>
    : MessageProcessor
    where TIncomingSink : RootMessage
    where TOutgoingSource : RootMessage {

    protected Channel<RootMessage>? _Channel;
    protected IMessageIncomingSink<TIncomingSink>? _IncomingSink;
    protected IMessageOutgoingSource<TOutgoingSource>? _OutgoingSource;

    public MessageProcessorTransform(
        NodeIdentifier nameId,
        ILogger logger
    ) : base(nameId, logger) {
        this._Channel = Channel.CreateUnbounded<RootMessage>();
        this._IncomingSink = new MessageIncomingSink<TIncomingSink>(nameof(this.IncomingSink), this, this._Channel.Writer.WriteAsync);
        this._OutgoingSource = new MessageOutgoingSource<TOutgoingSource>(nameof(this.OutgoingSource), this);
    }

    public IMessageIncomingSink<TIncomingSink> IncomingSink => this._IncomingSink ?? throw new ObjectDisposedException(TypeNameHelper.GetTypeDisplayName(this));

    public IMessageOutgoingSource<TOutgoingSource> OutgoingSource => this._OutgoingSource ?? throw new ObjectDisposedException(TypeNameHelper.GetTypeDisplayName(this));

    protected Task _TaskExecuteLoop = Task.CompletedTask;

    public override ValueTask StartAsync(CancellationToken cancellationToken) {
        if (this._TaskExecuteLoop.IsCompleted) {
            this._TaskExecuteLoop = this.ExecuteLoopAsync(cancellationToken);
        }
        return ValueTask.CompletedTask;
    }

    public override async ValueTask ExecuteAsync(CancellationToken cancellationToken) {
        await this._TaskExecuteLoop.ConfigureAwait(false);
    }

    protected virtual async Task ExecuteLoopAsync(CancellationToken cancellationToken) {
        var reader = this._Channel?.Reader;
        while (!cancellationToken.IsCancellationRequested && reader is not null && this._Channel is not null) {
            if (!reader.TryRead(out var message)) {
                try {
                    await reader.WaitToReadAsync(cancellationToken);
                } catch (TaskCanceledException) {
                    reader = null;
                }
            } else {
                if (message is TIncomingSink incomingSink) {
                    await this.HandleDataAsync(incomingSink, cancellationToken);
                } else {
                    await this.HandleMessageAsync(message, cancellationToken);
                }
            }
        }

    }

    protected abstract ValueTask HandleMessageAsync(RootMessage message, CancellationToken cancellationToken);

    protected abstract ValueTask HandleDataAsync(TIncomingSink message, CancellationToken cancellationToken);

    protected override bool Dispose(bool disposing) {
        if (base.Dispose(disposing)) {
            if (disposing) {
                this._Channel = null;
            }
            return true;
        } else {
            return false;
        }
    }
}
