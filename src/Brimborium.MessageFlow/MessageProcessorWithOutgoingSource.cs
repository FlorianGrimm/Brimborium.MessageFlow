namespace Brimborium.MessageFlow;

public class MessageProcessorWithOutgoingSource<TOutput>
    : MessageProcessor
    , IMessageProcessorWithOutgoingSource<TOutput>
    where TOutput : RootMessage {
    private IMessageOutgoingSource<TOutput>? _OutgoingSource;

    public MessageProcessorWithOutgoingSource(
            NodeIdentifier nameId,
            string nameOutgoingSource,
            IMessageProcessorSourceFactory outgoingSourceFactory,
            ILogger? logger
        ) : this(
            nameId: nameId,
            outgoingSource: outgoingSourceFactory.Create<TOutput>(nameId, NodeIdentifier.CreateChild(nameId, nameOutgoingSource), logger),
            logger: logger
        ) {
    }

    public MessageProcessorWithOutgoingSource(
           NodeIdentifier nameId,
           IMessageOutgoingSource<TOutput> outgoingSource,
           ILogger? logger
       ) : base(
           nameId: nameId,
           logger: logger
           ) {
        this._OutgoingSource = outgoingSource;
    }

    public IMessageOutgoingSource<TOutput>? OutgoingSource => this._OutgoingSource;

    public IMessageOutgoingSource<TOutput> OutgoingSourceD => this._OutgoingSource ?? throw new InvalidOperationException(nameof(this.OutgoingSourceD));

    protected override bool Dispose(bool disposing) {
        if (base.Dispose(disposing)) {
            this._OutgoingSource = null;
            return true;
        } else {
            return false;
        }
    }
}