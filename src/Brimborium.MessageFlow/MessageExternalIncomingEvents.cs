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
public class MessageExternalIncomingEvents<TOutput>
    : MessageProcessorTransform<RootMessage, TOutput>
    where TOutput : RootMessage {
    public MessageExternalIncomingEvents(
        string name,
        string nameIncomingSink,
        string nameOutgoingSource,
        ITraceDataService? traceDataService,
        ILogger logger
        ): base(
            name: name,
            nameIncomingSink: nameIncomingSink,
            nameOutgoingSource: nameOutgoingSource,
            traceDataService:traceDataService, 
            logger:logger) {
    }
}
