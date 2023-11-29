

namespace Brimborium.MessageFlow;

public interface IWithName {
    NodeIdentifier NameId { get; }
}

public interface IMessageProcessor
    : IDisposableWithState
    , IWithName 
    , IMessageFlowLogging
    {
    List<IMessageIncomingSink> GetListIncomingSink();
    List<IMessageOutgoingSource> GetListOutgoingSource();

    ValueTask StartAsync(CancellationToken cancellationToken);

    ValueTask ExecuteAsync(CancellationToken cancellationToken);

    ValueTask TearDownAsync(CancellationToken cancellationToken);

    MessageGraphNode ToMessageGraphNode();
}

public interface IMessageOutgoingSource
    : IWithName {
    NodeIdentifier NodeNameId { get; }

    ValueTask SendMessageAsync(FlowMessage message, CancellationToken cancellationToken);
}

public interface IMessageOutgoingSource<T>
    : IMessageOutgoingSource
    where T : FlowMessage {
    ValueTask SendDataAsync(T message, CancellationToken cancellationToken);
}

public interface IMessageOutgoingSourceInternal : IMessageOutgoingSource {
    void CollectMessageProcessor(System.Collections.Generic.HashSet<IMessageProcessor> htMessageProcessor);
    void Connect(IMessageConnectionAccessor connectionAccessor);
}

public interface IMessageFlowLogging {
    void LogSendMessage(NodeIdentifier nameId, FlowMessage message);
    void LogHandleMessage(NodeIdentifier nameId, FlowMessage message);

    ILogger GetLogger();
}

public interface IMessageConnectionAccessor 
    : IMessageFlowLogging {
    bool TryGetSinks(
       NodeIdentifier sourceId,
       [MaybeNullWhen(false)] out ImmutableArray<IMessageIncomingSink> result);
    
    void SetMessageFlowEnd(IMessageProcessor owner);
}

public interface IMessageIncomingSink
    : IWithName {
    NodeIdentifier NodeNameId { get; }

    ValueTask ReceiveMessageAsync(FlowMessage message, CancellationToken cancellationToken);
}

public interface IMessageIncomingSink<T>
    : IMessageIncomingSink
    where T : FlowMessage {
    ValueTask ReceiveDataAsync(T message, CancellationToken cancellationToken);
}

public interface IMessageIncomingSinkInternal : IMessageIncomingSink {
    void CollectMessageProcessor(System.Collections.Generic.HashSet<IMessageProcessor> htMessageProcessor);
}


public interface IMessageConnection {
    IMessageOutgoingSource OutgoingSource { get; }
    IMessageIncomingSink IncomingSink { get; }

    MessageGraphConnection ToMessageGraphConnection();
}

public interface IMessageConnection<T>
    where T : FlowMessage {
    IMessageOutgoingSource<T> OutgoingSourceData { get; }
    IMessageIncomingSink<T> IncomingSinkData { get; }
}

public interface IMessageConnectionInternal : IMessageConnection {
    void CollectMessageProcessor(System.Collections.Generic.HashSet<IMessageProcessor> htMessageProcessor);
}

public interface IMessageEngine
    : IDisposableWithState {
    IMessageFlowLogging MessageFlowLogging { get; }

    IMessageOutgoingSource GlobalOutgoingSource { get; }

    IMessageIncomingSink GlobalIncomingSink { get; }

    void ConnectMessage(IMessageOutgoingSource outgoingSource, IMessageIncomingSink incomingSink);

    void ConnectData<T>(IMessageOutgoingSource<T> outgoingSource, IMessageIncomingSink<T> incomingSink)
        where T : FlowMessage;

    //ValueTask BootAsync(CancellationToken cancellationToken);

    ValueTask StartAsync(CancellationToken cancellationToken);

    ValueTask ExecuteAsync(CancellationToken cancellationToken);

    //ValueTask ShutdownAsync(CancellationToken cancellationToken);
    MessageFlowGraph ToMessageFlowGraph();
    ValueTask<bool> SendFlowEnd(Exception? error = null, CancellationToken cancellationToken = default);

    void HandleApplicationStopping();
}
