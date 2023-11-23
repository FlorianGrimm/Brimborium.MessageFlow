

namespace Brimborium.MessageFlow;

public class MessageOutgoingSource(NodeIdentifier nameId, IMessageProcessor owner)
    : IMessageOutgoingSource
    , IMessageOutgoingSourceInternal {
    private readonly NodeIdentifier _NameId = nameId;
    private readonly IMessageProcessor _Owner = owner;
    private IMessageConnectionAccessor? _ConnectionAccessor = default;

    public NodeIdentifier NameId => this._NameId;

    public NodeIdentifier NodeNameId => this._Owner.NameId;

    public void CollectMessageProcessor(HashSet<IMessageProcessor> htMessageProcessor) {
        if (this._Owner.GetIsDisposed()) {
        } else {
            htMessageProcessor.Add(this._Owner);
        }
    }

    public void Connect(IMessageConnectionAccessor connectionAccessorn) {
        this._ConnectionAccessor = connectionAccessorn;
    }

    public async ValueTask SendMessageAsync(RootMessage message, CancellationToken cancellationToken) {
        if (this._ConnectionAccessor is null) {
            //
        } else {
            if (this._ConnectionAccessor.TryGetSinks(this._NameId, out var listSinks)) {
                message = message.Normalize(this._NameId);
                foreach (IMessageIncomingSink sink in listSinks) {
                    await sink.ReceiveMessageAsync(message, cancellationToken);
                }
            }
            if (message is MessageFlowEnd) {
                this._ConnectionAccessor.SetMessageFlowEnd(this._Owner);
            }
        }
    }
}

public class MessageOutgoingSource<T>(NodeIdentifier nameId, IMessageProcessor owner)
    : IMessageOutgoingSource<T>
    , IMessageOutgoingSourceInternal
    where T : RootMessage {
    private readonly NodeIdentifier _NameId = nameId;
    private readonly IMessageProcessor _Owner = owner;
    private IMessageConnectionAccessor? _ConnectionAccessor = default;

    public NodeIdentifier NameId => this._NameId;

    public NodeIdentifier NodeNameId => this._Owner.NameId;

    public void CollectMessageProcessor(HashSet<IMessageProcessor> htMessageProcessor) {
        if (this._Owner.GetIsDisposed()) {
        } else {
            htMessageProcessor.Add(this._Owner);
        }
    }

    public void Connect(IMessageConnectionAccessor connectionAccessor) {
        this._ConnectionAccessor = connectionAccessor;
    }

    public async ValueTask SendDataAsync(T message, CancellationToken cancellationToken) {
        if (this._ConnectionAccessor is null) {
            //
        } else {
            if (this._ConnectionAccessor.TryGetSinks(this._NameId, out var listSinks)) {
                message = message.Normalize(this._NameId);
                foreach (IMessageIncomingSink sink in listSinks) {
                    //if (sink is IMessageIncomingSink<T> messageIncomingSinkT) { 
                    //    await messageIncomingSinkT.ReceiveDataAsync(message, cancellationToken);
                    //} else {
                    //   await sink.ReceiveMessageAsync(message, cancellationToken);
                    //}
                    await sink.ReceiveMessageAsync(message, cancellationToken);
                }
            }
            if (message is MessageFlowEnd) {
                this._ConnectionAccessor.SetMessageFlowEnd(this._Owner);
            }
        }
    }

    public async ValueTask SendMessageAsync(RootMessage message, CancellationToken cancellationToken) {
        if (this._ConnectionAccessor is null) {
            //
        } else {
            if (this._ConnectionAccessor.TryGetSinks(this._NameId, out var listSinks)) {
                message = message.Normalize(this._NameId);
                foreach (IMessageIncomingSink sink in listSinks) {
                    await sink.ReceiveMessageAsync(message, cancellationToken);
                }
            }
            if (message is MessageFlowEnd) {
                this._ConnectionAccessor.SetMessageFlowEnd(this._Owner);
            }
        }
    }
}