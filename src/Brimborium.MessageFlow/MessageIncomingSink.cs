﻿namespace Brimborium.MessageFlow;

public delegate ValueTask ChannelWriteAsync(FlowMessage item, CancellationToken cancellationToken);

public class MessageIncomingSink 
    : IMessageIncomingSink
    , IMessageIncomingSinkInternal {
    private readonly NodeIdentifier _NameId;
    private readonly IMessageProcessor _Owner;
    private readonly ChannelWriteAsync _ChannelWriteAsync;

    public MessageIncomingSink(NodeIdentifier nameId, IMessageProcessor owner, ChannelWriteAsync channelWriteAsync) {
        this._NameId = nameId;
        this._Owner = owner;
        this._ChannelWriteAsync = channelWriteAsync;
    }

    public NodeIdentifier NameId => this._NameId;

    public NodeIdentifier NodeNameId => this._Owner.NameId;

    public async ValueTask ReceiveMessageAsync(FlowMessage message, CancellationToken cancellationToken) {
        ObjectDisposedException.ThrowIf(this._Owner.GetIsDisposed(), this);

        await this._ChannelWriteAsync(message, cancellationToken);
    }

    public void CollectMessageProcessor(HashSet<IMessageProcessor> htMessageProcessor) {
        if (this._Owner.GetIsDisposed()) {
        } else {
            htMessageProcessor.Add(this._Owner);
        }
    }
}

public class MessageIncomingSink<T>
    : IMessageIncomingSink<T>
    , IMessageIncomingSinkInternal
    where T : FlowMessage {
    private readonly NodeIdentifier _NameId;
    private readonly IMessageProcessor _Owner;
    private readonly ChannelWriteAsync _ChannelWriteAsync;

    public MessageIncomingSink(
        NodeIdentifier nameId, 
        IMessageProcessor owner, 
        ChannelWriteAsync channelWriteAsync) {
        this._NameId = nameId;
        this._Owner = owner;
        this._ChannelWriteAsync = channelWriteAsync;
    }

    public NodeIdentifier NameId => this._NameId;

    public NodeIdentifier NodeNameId => this._Owner.NameId;

    public async ValueTask ReceiveMessageAsync(FlowMessage message, CancellationToken cancellationToken) {
        ObjectDisposedException.ThrowIf(this._Owner.GetIsDisposed(), this);
        
        await this._ChannelWriteAsync(message, cancellationToken);
    }

    public async ValueTask ReceiveDataAsync(T message, CancellationToken cancellationToken) {
        ObjectDisposedException.ThrowIf(this._Owner.GetIsDisposed(), this);

        await this._ChannelWriteAsync(message, cancellationToken);
    }

    public void CollectMessageProcessor(HashSet<IMessageProcessor> htMessageProcessor) {
        if (this._Owner.GetIsDisposed()) {
        } else {
            htMessageProcessor.Add(this._Owner);
        }
    }
}