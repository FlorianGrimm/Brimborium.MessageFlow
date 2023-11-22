namespace Brimborium.MessageFlow;

public sealed class MessageProcessorSinkChannel<TInput>
    : MessageSinkChannel<TInput>
    where TInput : RootMessage {
    private IMessageProcessorWithIncomingSinkInternal<TInput>? _Processor;

    public MessageProcessorSinkChannel(
        IMessageProcessorWithIncomingSinkInternal<TInput> processorOwner,
        NodeIdentifier nameId,
        ChannelOptions? channelOptions,
        ILogger logger)
        : base(nameId, channelOptions, processorOwner, logger) {
        this._Processor = processorOwner;
    }

    public override async ValueTask HandleDataMessageAsync(TInput message, CancellationToken cancellationToken) {
        if (this._Processor is not null) { 
            await this._Processor.HandleDataMessageAsync(message, cancellationToken);
        }
    }
    
    public override async ValueTask HandleControlMessageAsync(RootMessage message, CancellationToken cancellationToken) {
        if (this._Processor is not null) {
            await this._Processor.HandleControlMessageAsync(message, cancellationToken);
        }
    }

    protected override bool Dispose(bool disposing) {
        if (base.Dispose(disposing)) {
            this._Processor = null;
            return true;
        } else {
            return false;
        }
    }
}
