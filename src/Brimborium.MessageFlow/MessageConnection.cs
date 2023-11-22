
namespace Brimborium.MessageFlow;

public sealed class MessageConnection(
    IMessageOutgoingSource outgoingSource,
    IMessageIncomingSink incomingSink
    ) 
    : IMessageConnection
    , IMessageConnectionInternal {
    private readonly IMessageOutgoingSource _OutgoingSource = outgoingSource;
    private readonly IMessageIncomingSink _IncomingSink = incomingSink;

    public IMessageOutgoingSource OutgoingSource => this._OutgoingSource;

    public IMessageIncomingSink IncomingSink => this._IncomingSink;

    public void CollectMessageProcessor(HashSet<IMessageProcessor> htMessageProcessor) {
        if (this._OutgoingSource is IMessageOutgoingSourceInternal messageOutgoingSourceInternal) {
            messageOutgoingSourceInternal.CollectMessageProcessor(htMessageProcessor);
        }
        if (this._IncomingSink is IMessageIncomingSinkInternal messageIncomingSinkInternal) {
            messageIncomingSinkInternal.CollectMessageProcessor(htMessageProcessor);
        }
    }
}


public sealed class MessageConnection<T>(
    IMessageOutgoingSource<T> outgoingSourceData,
    IMessageIncomingSink<T> incomingSinkData
    )
    : IMessageConnection
    , IMessageConnection<T>
    , IMessageConnectionInternal
    where T : RootMessage {
    private readonly IMessageOutgoingSource<T> _OutgoingSourceData = outgoingSourceData;
    private readonly IMessageIncomingSink<T> _IncomingSinkData = incomingSinkData;

    public IMessageOutgoingSource OutgoingSource => this._OutgoingSourceData;
    public IMessageOutgoingSource<T> OutgoingSourceData => this._OutgoingSourceData;

    public IMessageIncomingSink IncomingSink => this._IncomingSinkData;
    public IMessageIncomingSink<T> IncomingSinkData => this._IncomingSinkData;

    public void CollectMessageProcessor(HashSet<IMessageProcessor> htMessageProcessor) {
        if (this._OutgoingSourceData is IMessageOutgoingSourceInternal messageOutgoingSourceInternal) {
            messageOutgoingSourceInternal.CollectMessageProcessor(htMessageProcessor);
        }
        if (this._IncomingSinkData is IMessageIncomingSinkInternal messageIncomingSinkInternal) {
            messageIncomingSinkInternal.CollectMessageProcessor(htMessageProcessor);
        }
    }
}
