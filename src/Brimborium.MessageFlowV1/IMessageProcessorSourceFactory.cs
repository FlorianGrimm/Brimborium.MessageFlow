namespace Brimborium.MessageFlow;

public interface IMessageProcessorSourceFactory {
    public static IMessageProcessorSourceFactory Instance => MessageOutgoingSourceMultiTargetFactory.Instance;

    IMessageOutgoingSource<TOutput> Create<TOutput>(NodeIdentifier nameId, NodeIdentifier sourceId, ILogger? logger) where TOutput : RootMessage;
}
