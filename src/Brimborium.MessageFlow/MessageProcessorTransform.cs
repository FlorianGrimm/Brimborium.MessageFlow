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
        ITraceDataService? traceDataService,
        ILogger logger
    ) : this(
        name: name,
        nameIncomingSink: "Sink",
        nameOutgoingSource: "Source",
        incomingSinkFactory: IMessageProcessorSinkFactory.Instance,
        outgoingSourceFactory: IMessageProcessorSourceFactory.Instance,
        traceDataService: traceDataService,
        logger: logger) {
    }

    protected MessageProcessorTransform(
        string name,
        string nameIncomingSink,
        string nameOutgoingSource,
        ITraceDataService? traceDataService,
        ILogger logger
    ) : this(
            name: name,
            nameIncomingSink: nameIncomingSink,
            nameOutgoingSource: nameOutgoingSource,
            incomingSinkFactory: IMessageProcessorSinkFactory.Instance,
            outgoingSourceFactory: IMessageProcessorSourceFactory.Instance,
            traceDataService: traceDataService,
            logger: logger) {
    }

    protected MessageProcessorTransform(
        string name,
        string nameIncomingSink,
        string nameOutgoingSource,
        IMessageProcessorSinkFactory incomingSinkFactory,
        IMessageProcessorSourceFactory outgoingSourceFactory,
        ITraceDataService? traceDataService,
        ILogger logger
        ) : base(
            name: name,
            nameIncomingSink: nameIncomingSink,
            incomingSinkFactory: incomingSinkFactory,
            traceDataService: traceDataService,
            logger: logger) {
        this._OutgoingSource = outgoingSourceFactory
            .Create<TOutput>(this.NameId, NodeIdentifier.CreateChild(this.NameId, nameOutgoingSource), logger);
        this._OutgoingSourceIdName = this._OutgoingSource.SourceId.ToString();
    }

    public IMessageOutgoingSource<TOutput>? OutgoingSource => this._OutgoingSource;
    public IMessageOutgoingSource<TOutput> OutgoingSourceD => this._OutgoingSource ?? throw new InvalidOperationException(nameof(this.OutgoingSourceD));

    protected override List<IMessageOutgoingSource> GetListOutgoingSource() {
        var result = base.GetListOutgoingSource();
        if (this._OutgoingSource is not null) {
            result.Add(this._OutgoingSource);
        }
        return result;
    }

    public override bool CollectCoordinatorNode(HashSet<CoordinatorNode> listTarget) {
        return listTarget.Add(
            new CoordinatorNode(
                this._NameId,
                (this._OutgoingSource is not null)
                   ? [this._OutgoingSource.SourceId]
                   : [],
                (this._IncomingSink is not null)
                    ? [this._IncomingSink.GetCoordinatorNodeSink()]
                    : [],
                new()));
    }

    protected override bool Dispose(bool disposing) {
        if (base.Dispose(disposing)) {
            using (var source = this._OutgoingSource) {
                if (disposing) {
                    this._OutgoingSource = null!;
                }
            }
            return true;
        } else {
            return false;
        }
    }
}

public interface IMessageProcessorSourceFactory {
    public static IMessageProcessorSourceFactory Instance => MessageOutgoingSourceMultiTargetFactory.Instance;

    IMessageOutgoingSource<TOutput> Create<TOutput>(NodeIdentifier nameId, NodeIdentifier sourceId, ILogger? logger) where TOutput : RootMessage;
}

public sealed class MessageOutgoingSourceMultiTargetFactory : IMessageProcessorSourceFactory {
    private static IMessageProcessorSourceFactory? _Instance;
    public static IMessageProcessorSourceFactory Instance => _Instance ??= new MessageOutgoingSourceMultiTargetFactory();

    private MessageOutgoingSourceMultiTargetFactory() {
    }

    public IMessageOutgoingSource<TOutput> Create<TOutput>(
            NodeIdentifier nameId,
            NodeIdentifier sourceId,
            ILogger? logger
        )
        where TOutput : RootMessage
        => new MessageOutgoingSourceMultiTarget<TOutput>(
            sourceId: sourceId,
            logger: logger);
}
