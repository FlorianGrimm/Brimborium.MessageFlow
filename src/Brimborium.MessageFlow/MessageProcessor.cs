namespace Brimborium.MessageFlow;

public interface IMessageProcessor<TInput>
    : IDisposableAndCancellation
    , IWithCoordinatorNode
    where TInput : RootMessage {
    NodeIdentifier NameId { get; }

    IMessageIncomingSink<TInput>? IncomingSink { get; }
    IMessageIncomingSink<TInput> IncomingSinkD { get; }
}

public interface IMessageProcessor<TInput, TOutput>
    : IMessageProcessor<TInput>
    where TInput : RootMessage
    where TOutput : RootMessage {
    IMessageOutgoingSource<TOutput>? OutgoingSource { get; }
    IMessageOutgoingSource<TOutput> OutgoingSourceD { get; }
}

public class MessageProcessor<TInput>
    : DisposableAndCancellation
    , IWithCoordinatorNode
    , IMessageProcessor<TInput>
    where TInput : RootMessage {
    protected readonly ITraceDataService? _TraceDataService;
    protected readonly ILogger _Logger;
    protected readonly NodeIdentifier _NameId;
    protected readonly string _IncomingSinkIdName;
    protected MessageProcessor<TInput>.MessageProcessorSinkChannel? _IncomingSink;

    protected MessageProcessor(
        string name,
        string nameIncomingSink,
        ITraceDataService? traceDataService,
        ILogger logger
        ) {
        this._TraceDataService = traceDataService;
        this._Logger = logger;
        this._NameId = NodeIdentifier.Create(name);
        this._IncomingSink = new MessageProcessorSinkChannel(this, nameIncomingSink);
        this._IncomingSinkIdName = this._IncomingSink.SinkId.ToString();
    }

    public NodeIdentifier NameId => this._NameId;

    public IMessageIncomingSink<TInput>? IncomingSink => this._IncomingSink;
    public IMessageIncomingSink<TInput> IncomingSinkD => this._IncomingSink ?? throw new InvalidOperationException(nameof(this.IncomingSinkD));

    public virtual void CollectCoordinatorNode(HashSet<CoordinatorNode> listTarget) {
        listTarget.Add(
            new CoordinatorNode(
                this._NameId,
                null,
                (this._IncomingSink is not null)
                    ? new List<CoordinatorNodeSink>() { this._IncomingSink.GetCoordinatorNodeSink() }
                    : null));
    }

    protected virtual Task HandleMessageAsync(TInput message, CancellationToken cancellationToken) {
        this.TraceData(message);
        return Task.CompletedTask;
    }

    protected virtual async Task HandleControlMessageAsync(RootMessage message, CancellationToken cancellationToken) {
        this.TraceData(message);
        await this.ForwardControlMessageAsync(message, cancellationToken);
    }

    protected void TraceData(RootMessage message) {
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
                    this._Logger.LogMessageProcessorForwardControlFailed(error, message.ToRootMessageLog());
                }
            }
        }
    }

    protected virtual IEnumerable<IMessageIncomingSink>? GetListIncomingSink() {
        return default;
    }

    protected virtual IEnumerable<IMessageOutgoingSource>? GetListOutgoingSource() {
        return default;
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

    public class MessageProcessorSinkChannel
      : MessageSinkChannel<TInput> {
        private readonly MessageProcessor<TInput> _Processor;

        public MessageProcessorSinkChannel(MessageProcessor<TInput> processor, string name)
            : base(NodeIdentifier.CreateChild(processor._NameId, name),
                  default,
                  processor._Logger
            ) {
            this._Processor = processor;
        }

        public override async ValueTask HandleDataMessageAsync(
            TInput message,
            CancellationToken cancellationToken) {
            await this._Processor.HandleMessageAsync(message, cancellationToken);
        }

        public override async ValueTask HandleControlMessageAsync(RootMessage message, CancellationToken cancellationToken) {
            await this._Processor.HandleControlMessageAsync(message, cancellationToken);
        }
    }
}

public class MessageProcessor<TInput, TOutput>
    : MessageProcessor<TInput>
    , IMessageProcessor<TInput, TOutput>
    where TInput : RootMessage
    where TOutput : RootMessage {
    protected MessageSourceMultiTarget<TOutput>? _OutgoingSource;
    protected readonly string _OutgoingSourceIdName;

    public MessageProcessor(
        string name,
        string nameIncomingSink,
        string nameOutgoingSource,
        ITraceDataService? traceDataService,
        ILogger logger
        ) : base(name, nameIncomingSink, traceDataService, logger) {
        this._OutgoingSource = new MessageSourceMultiTarget<TOutput>(
            NodeIdentifier.CreateChild(this._NameId, nameOutgoingSource),
            logger);
        this._OutgoingSourceIdName = this._OutgoingSource.SourceId.ToString();
    }

    public IMessageOutgoingSource<TOutput>? OutgoingSource => this._OutgoingSource;
    public IMessageOutgoingSource<TOutput> OutgoingSourceD => this._OutgoingSource ?? throw new InvalidOperationException(nameof(this.OutgoingSourceD));

    public override void CollectCoordinatorNode(HashSet<CoordinatorNode> listTarget) {
        listTarget.Add(
            new CoordinatorNode(
                this._NameId,
                (this._OutgoingSource is not null)
                   ? [this._OutgoingSource.SourceId]
                   : null,
                (this._IncomingSink is not null)
                    ? [this._IncomingSink.GetCoordinatorNodeSink()]
                    : null));
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
