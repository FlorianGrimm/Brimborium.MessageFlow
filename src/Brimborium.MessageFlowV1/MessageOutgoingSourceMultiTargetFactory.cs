namespace Brimborium.MessageFlow;

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
            sourceId, logger);
}
