namespace Brimborium.MessageFlow;

public interface IMessageProcessorSinkFactory {
    public static IMessageProcessorSinkFactory Instance => MessageProcessorSinkChannelFactory.Instance;

    IMessageSinkInternal<TInput> Create<TInput>(
        IMessageProcessorWithIncomingSink<TInput> messageProcessor,
        NodeIdentifier nameId,
        ILogger logger) where TInput : RootMessage;
}
