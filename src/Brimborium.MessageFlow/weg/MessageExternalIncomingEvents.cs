namespace Brimborium.MessageFlow;

#if false
public class MessageExternalIncomingEvents<TOutput>
    : MessageProcessorWithOutgoingSource<TOutput>
    where TOutput : RootMessage {

    public MessageExternalIncomingEvents(
            NodeIdentifier nameId,
            string nameOutgoingSource,
            IMessageProcessorSourceFactory outgoingSourceFactory,
            ILogger? logger
        ) : this(
            nameId: nameId,
            outgoingSource: outgoingSourceFactory.Create<TOutput>(nameId, NodeIdentifier.CreateChild(nameId, nameOutgoingSource), logger),
            logger: logger
        ) {
    }

    public MessageExternalIncomingEvents(
           NodeIdentifier nameId,
           IMessageOutgoingSource<TOutput> outgoingSource,
            ILogger? logger
       ) : base(
           nameId: nameId,
           outgoingSource: outgoingSource,
           logger: logger
           ) {
    }
}
#endif
public class MessageExternalIncomingEvents<TOutput>(
        NodeIdentifier nameId,
        string nameIncomingSink,
        string nameOutgoingSource,
        IMessageProcessorExamine? messageProcessorExamine,
        ITraceDataService? traceDataService,
        ILogger logger
        )
    : MessageProcessorTransform<RootMessage, TOutput>(
            nameId,
            nameIncomingSink,
            nameOutgoingSource,
            messageProcessorExamine,
            traceDataService,
            logger)
    where TOutput : RootMessage {
    /*
    public MessageExternalIncomingEvents(
        NodeIdentifier nameId,
        string nameIncomingSink,
        string nameOutgoingSource,
        IMessageProcessorExamine? messageProcessorExamine,
        ITraceDataService? traceDataService,
        ILogger logger
        ) : base(
            nameId,
            nameIncomingSink,
            nameOutgoingSource,
            messageProcessorExamine,
            traceDataService,
            logger) {
    }
    */
}
