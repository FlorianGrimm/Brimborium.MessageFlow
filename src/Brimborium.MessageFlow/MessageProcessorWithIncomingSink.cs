#pragma warning disable IDE0031 // Use null propagation
// warning disable IDE0290 // Use primary constructor

namespace Brimborium.MessageFlow;

public class MessageProcessorWithIncomingSink<TInput>
    : MessageProcessor
    , IWithCoordinatorNode
    , IMessageProcessorWithIncomingSinkInternal<TInput>
    where TInput : RootMessage {
    protected readonly ITraceDataService? _TraceDataService;
    protected readonly string _IncomingSinkIdName;
    protected IMessageSinkInternal<TInput> _IncomingSink;

    protected MessageProcessorWithIncomingSink(
        string name,
        string nameIncomingSink,
        ITraceDataService? traceDataService,
        ILogger logger
        ) : this(
            name: name,
            nameIncomingSink: nameIncomingSink,
            incomingSinkFactory: IMessageProcessorSinkFactory.Instance,
            traceDataService: traceDataService,
            logger: logger
            ) {
    }

    protected MessageProcessorWithIncomingSink(
        string name,
        string nameIncomingSink,
        IMessageProcessorSinkFactory incomingSinkFactory,
        ITraceDataService? traceDataService,
        ILogger logger
        ) : this(
            nameId: NodeIdentifier.Create(name),
            nameIncomingSink: nameIncomingSink,
            incomingSinkFactory: incomingSinkFactory,
            traceDataService: traceDataService,
            logger: logger
            ) {
    }

    protected MessageProcessorWithIncomingSink(
        NodeIdentifier nameId,
        string nameIncomingSink,
        IMessageProcessorSinkFactory incomingSinkFactory,
        ITraceDataService? traceDataService,
        ILogger logger
        ) : base(
            nameId: nameId,
            logger: logger
            ) {
        this._TraceDataService = traceDataService;
        this._IncomingSink = incomingSinkFactory.Create<TInput>(this, NodeIdentifier.CreateChild(nameId, nameIncomingSink), logger);
        this._IncomingSinkIdName = this._IncomingSink.SinkId.ToString();
    }

    protected MessageProcessorWithIncomingSink(
        NodeIdentifier nameId,
        IMessageSinkInternal<TInput> incomingSink,
        ITraceDataService? traceDataService,
        ILogger logger
    ) : base(
            nameId: nameId,
            logger: logger
            ) {
        this._TraceDataService = traceDataService;
        this._IncomingSink = incomingSink;
        this._IncomingSinkIdName = this._IncomingSink.SinkId.ToString();
    }


    public IMessageIncomingSink<TInput>? IncomingSink => this._IncomingSink;
    public IMessageIncomingSink<TInput> IncomingSinkD => this._IncomingSink ?? throw new InvalidOperationException(nameof(this.IncomingSinkD));

    public override bool CollectCoordinatorNode(HashSet<CoordinatorNode> listTarget) {
        return listTarget.Add(
            new CoordinatorNode(
                this._NameId,
                new (),
                (this._IncomingSink is not null)
                    ? [this._IncomingSink.GetCoordinatorNodeSink()]
                    : [],
                new()));
    }

    ValueTask IMessageProcessorWithIncomingSinkInternal<TInput>.HandleDataMessageAsync(TInput message, CancellationToken cancellationToken)
        => this.HandleDataMessageAsync(message, cancellationToken);

    protected virtual ValueTask HandleDataMessageAsync(TInput message, CancellationToken cancellationToken) {
        this.TraceData(message);
        return ValueTask.CompletedTask;
    }

    ValueTask IMessageProcessorWithIncomingSinkInternal.HandleControlMessageAsync(RootMessage message, CancellationToken cancellationToken)
        => this.HandleControlMessageAsync(message, cancellationToken);

    protected virtual async ValueTask HandleControlMessageAsync(RootMessage message, CancellationToken cancellationToken) {
        this.TraceData(message);
        await this.ForwardControlMessageAsync(message, cancellationToken);
    }

    protected virtual void TraceData(RootMessage message) {
        var traceDataService = this._TraceDataService;
        if (traceDataService is not null) {
            traceDataService.TraceData(this._IncomingSinkIdName, message.MessageId);
        }
    }
    protected virtual async ValueTask ForwardControlMessageAsync(RootMessage message, CancellationToken cancellationToken) {
        // var listIncomingSink = this.GetListIncomingSink();
        var listOutgoingSource = this.GetListOutgoingSource();
        if (listOutgoingSource is null) {
        } else {
            //if (listIncomingSink.Count()==1)
            foreach (var messageOutgoingSource in listOutgoingSource) {
                try {
                    //if (listIncomingSink is null) {
                    //} else {
                    //  if (listIncomingSink.Any(sink => sink.Si )) { }
                    //}
                    //message.SourceId
                    await messageOutgoingSource.SendControlAsync(message, cancellationToken);
                } catch (Exception error) {
                    this.Logger.LogMessageProcessorForwardControlFailed(error, message.ToRootMessageLog());
                }
            }
        }
    }

    protected virtual List<IMessageIncomingSink> GetListIncomingSink() {
        return (this._IncomingSink is null) ? [] : [this._IncomingSink];
    }

    protected virtual List<IMessageOutgoingSource> GetListOutgoingSource() {
        return [];
    }

    protected override bool Dispose(bool disposing) {
        if (base.Dispose(disposing)) {
            using (var sink = this._IncomingSink) {
                if (disposing) {
                    this._IncomingSink = null!;
                }
            }
            return true;
        } else {
            return false;
        }
    }
}

public sealed class MessageProcessorSinkChannel<TInput>
    : MessageSinkChannel<TInput>
    where TInput : RootMessage {
    private readonly IMessageProcessorWithIncomingSinkInternal<TInput> _Processor;

    public MessageProcessorSinkChannel(
        IMessageProcessorWithIncomingSinkInternal<TInput> processor,
        string name,
        ChannelOptions? channelOptions,
        ILogger logger)
        : this(
              processor,
              NodeIdentifier.CreateChild(processor.NameId, name),
              channelOptions,
              logger
        ) {
    }

    public MessageProcessorSinkChannel(
        IMessageProcessorWithIncomingSinkInternal<TInput> processor,
        NodeIdentifier nameId,
        ChannelOptions? channelOptions,
        ILogger logger)
        : base(nameId,
              channelOptions,
              logger
        ) {
        this._Processor = processor;
    }

    public override ValueTask HandleDataMessageAsync(TInput message, CancellationToken cancellationToken)
        => this._Processor.HandleDataMessageAsync(message, cancellationToken);

    public override ValueTask HandleControlMessageAsync(RootMessage message, CancellationToken cancellationToken)
        => this._Processor.HandleControlMessageAsync(message, cancellationToken);
}

public interface IMessageProcessorSinkFactory {
    public static IMessageProcessorSinkFactory Instance => MessageProcessorSinkFactory.Instance;

    IMessageSinkInternal<TInput> Create<TInput>(
        IMessageProcessorWithIncomingSink<TInput> messageProcessor,
        //string nameIncomingSink,
        NodeIdentifier nameId,
        ILogger logger) where TInput : RootMessage;
}

public sealed class MessageProcessorSinkFactory : IMessageProcessorSinkFactory {
    private static IMessageProcessorSinkFactory? _Instance;
    public static IMessageProcessorSinkFactory Instance => _Instance ??= new MessageProcessorSinkFactory();

    private MessageProcessorSinkFactory() {
    }

    public IMessageSinkInternal<TInput> Create<TInput>(
            IMessageProcessorWithIncomingSink<TInput> messageProcessor,
            //string nameIncomingSink,
            NodeIdentifier nameId,
            ILogger logger
        )
        where TInput : RootMessage
        => new MessageProcessorSinkChannel<TInput>(
            processor: (IMessageProcessorWithIncomingSinkInternal<TInput>)messageProcessor,
            nameId: nameId,
            channelOptions: default,
            logger: logger);
}
