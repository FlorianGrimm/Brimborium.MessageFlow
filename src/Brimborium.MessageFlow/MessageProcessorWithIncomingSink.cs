namespace Brimborium.MessageFlow;

public class MessageProcessorWithIncomingSink<TInput>
    : MessageProcessor
    , IWithCoordinatorNode
    , IMessageProcessorWithIncomingSinkInternal<TInput>
    where TInput : RootMessage {
    protected IMessageSinkInternal<TInput> _IncomingSink;
    protected readonly string _IncomingSinkIdName;
  
    protected MessageProcessorWithIncomingSink(
        NodeIdentifier nameId,
        string nameIncomingSink,
        IMessageProcessorSinkFactory incomingSinkFactory,
        IMessageProcessorExamine? messageProcessorExamine,
        ITraceDataService? traceDataService,
        ILogger? logger
        ) : base(nameId, messageProcessorExamine, traceDataService, logger) {
        this._IncomingSink = incomingSinkFactory.Create<TInput>(this, NodeIdentifier.CreateChild(nameId, nameIncomingSink), this.Logger);
        this._IncomingSinkIdName = this._IncomingSink.SinkId.ToString();
        this._ListIncomingSink = this.GetListIncomingSink().ToImmutableArray();
    }

    public IMessageIncomingSink<TInput>? IncomingSink => this._IncomingSink;
    public IMessageIncomingSink<TInput> IncomingSinkD => this._IncomingSink ?? throw new InvalidOperationException(nameof(this.IncomingSinkD));

    public override bool CollectCoordinatorNode(HashSet<CoordinatorNode> listTarget)
        => listTarget.Add(
            new CoordinatorNode(
                this._NameId,
                new(),
                CoordinatorCollector.ToListCoordinatorNodeSink(this._ListIncomingSink),
                new()));

    ValueTask IMessageProcessorWithIncomingSinkInternal<TInput>.HandleDataMessageAsync(TInput message, CancellationToken cancellationToken)
        => this.HandleDataMessageAsync(message, cancellationToken);

    protected virtual ValueTask HandleDataMessageAsync(TInput message, CancellationToken cancellationToken) {
        this.TraceData(message);
        this.ExamineDataMessage(message);
        return ValueTask.CompletedTask;
    }

    ValueTask IMessageProcessorInternal.HandleControlMessageAsync(RootMessage message, CancellationToken cancellationToken)
        => this.HandleControlMessageAsync(message, cancellationToken);

    protected override List<IMessageIncomingSink> GetListIncomingSink()
        => base.GetListIncomingSink().AddValueIfNotNull(this._IncomingSink);

    protected override List<IMessageOutgoingSource> GetListOutgoingSource()
        => base.GetListOutgoingSource();

    protected override bool Dispose(bool disposing) {
        if (base.Dispose(disposing)) {
            using (var sink = this._IncomingSink) {
                if (disposing) {
                    this._IncomingSink = null!;
                    this._ListIncomingSink = [];
                    this.StateVersion++;
                }
            }
            return true;
        } else {
            return false;
        }
    }
}
