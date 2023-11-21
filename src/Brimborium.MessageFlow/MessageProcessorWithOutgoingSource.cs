namespace Brimborium.MessageFlow;

public class MessageProcessorWithOutgoingSource<TOutput>(
        NodeIdentifier nameId,
        IMessageOutgoingSource<TOutput> outgoingSource,
        IMessageProcessorExamine? messageProcessorExamine,
        ITraceDataService traceDataService,
        ILogger? logger
    ) : MessageProcessor(nameId, messageProcessorExamine, traceDataService, logger)
    , IMessageProcessorWithOutgoingSource<TOutput>
    where TOutput : RootMessage {
    private IMessageOutgoingSource<TOutput>? _OutgoingSource = outgoingSource;

    public MessageProcessorWithOutgoingSource(
            NodeIdentifier nameId,
            string nameOutgoingSource,
            IMessageProcessorSourceFactory outgoingSourceFactory,
            IMessageProcessorExamine? messageProcessorExamine,
            ITraceDataService traceDataService,
            ILogger? logger
        ) : this(
            nameId,
            outgoingSourceFactory.Create<TOutput>(nameId, NodeIdentifier.CreateChild(nameId, nameOutgoingSource), logger),
            messageProcessorExamine,
            traceDataService,
            logger
        ) {
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