namespace Brimborium.MessageFlow;

public sealed class MessageProcessorSinkChannelFactory : IMessageProcessorSinkFactory {
    private static IMessageProcessorSinkFactory? _Instance;
    public static IMessageProcessorSinkFactory Instance => _Instance ??= new MessageProcessorSinkChannelFactory();

    private MessageProcessorSinkChannelFactory() {
    }

    public IMessageSinkInternal<TInput> Create<TInput>(
            IMessageProcessorWithIncomingSink<TInput> messageProcessor,
            NodeIdentifier nameId,
            ILogger logger
        )
        where TInput : RootMessage
        => new MessageProcessorSinkChannel<TInput>(
            processorOwner: (IMessageProcessorWithIncomingSinkInternal<TInput>)messageProcessor,
            nameId: nameId,
            channelOptions: default,
            logger: logger);
}
