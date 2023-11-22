namespace Brimborium.MessageFlow;

public class MessageProcessorTransform<TInput, TOutput>
    : MessageProcessorWithIncomingSink<TInput>
    , IMessageProcessor<TInput, TOutput>
    where TInput : RootMessage
    where TOutput : RootMessage {

    protected IMessageOutgoingSource<TOutput>? _OutgoingSource;
    protected readonly string _OutgoingSourceIdName;

    protected MessageProcessorTransform(
        string name,
        IMessageProcessorExamine? messageProcessorExamine,
        ITraceDataService? traceDataService,
        ILogger logger
    ) : this(
        NodeIdentifier.Create(name),
        "Sink",
        "Source",
        IMessageProcessorSinkFactory.Instance,
        IMessageProcessorSourceFactory.Instance,
        messageProcessorExamine,
        traceDataService,
        logger) {
    }

    protected MessageProcessorTransform(
        NodeIdentifier nameId,
        string nameIncomingSink,
        string nameOutgoingSource,
        IMessageProcessorExamine? messageProcessorExamine,
        ITraceDataService? traceDataService,
        ILogger logger
    ) : this(
            nameId,
            nameIncomingSink,
            nameOutgoingSource,
            IMessageProcessorSinkFactory.Instance,
            IMessageProcessorSourceFactory.Instance,
            messageProcessorExamine,
            traceDataService,
            logger) {
    }

    protected MessageProcessorTransform(
        NodeIdentifier nameId,
        string nameIncomingSink,
        string nameOutgoingSource,
        IMessageProcessorSinkFactory incomingSinkFactory,
        IMessageProcessorSourceFactory outgoingSourceFactory,
        IMessageProcessorExamine? messageProcessorExamine,
        ITraceDataService? traceDataService,
        ILogger logger
        ) : base(
            nameId,
            nameIncomingSink,
            incomingSinkFactory,
            messageProcessorExamine,
            traceDataService,
            logger) {
        this._OutgoingSource = outgoingSourceFactory
            .Create<TOutput>(this.NameId, NodeIdentifier.CreateChild(this.NameId, nameOutgoingSource), logger);
        this._OutgoingSourceIdName = this._OutgoingSource.SourceId.ToString();
    }

    public IMessageOutgoingSource<TOutput>? OutgoingSource => this._OutgoingSource;
    public IMessageOutgoingSource<TOutput> OutgoingSourceD => this._OutgoingSource ?? throw new InvalidOperationException(nameof(this.OutgoingSourceD));

    protected override List<IMessageOutgoingSource> GetListOutgoingSource()
        => base.GetListOutgoingSource()
            .AddValueIfNotNull(this._OutgoingSource);

    public override bool CollectCoordinatorNode(HashSet<CoordinatorNode> listTarget)
        => listTarget.Add(
            new CoordinatorNode(
                this._NameId,
                CoordinatorCollector.ToListCoordinatorNodeSourceId(this._ListOutgoingSource),
                CoordinatorCollector.ToListCoordinatorNodeSink(this._ListIncomingSink),
                new()));

    protected override bool Dispose(bool disposing) {
        if (base.Dispose(disposing)) {
            using (var source = this._OutgoingSource) {
                if (disposing) {
                    this._OutgoingSource = null;
                    this.StateVersion++;
                    this._ListOutgoingSource = [];
                }
            }
            return true;
        } else {
            return false;
        }
    }
}
