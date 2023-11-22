
namespace Brimborium.MessageFlow;

public interface IWithName {
    NodeIdentifier NameId { get; }
}

public interface IMessageProcessor
    : IDisposableWithState
    , IWithName {
    List<IMessageIncomingSink> GetListIncomingSink();
    List<IMessageOutgoingSource> GetListOutgoingSource();

    ValueTask StartAsync(CancellationToken cancellationToken);

    ValueTask ExecuteAsync(CancellationToken cancellationToken);
}

public interface IMessageOutgoingSource
    : IWithName {
    ValueTask SendMessageAsync(RootMessage message, CancellationToken cancellationToken);
}

public interface IMessageOutgoingSource<T>
    : IMessageOutgoingSource
    where T : RootMessage {
    ValueTask SendDataAsync(T message, CancellationToken cancellationToken);
}

public interface IMessageOutgoingSourceInternal : IMessageOutgoingSource {
    void CollectMessageProcessor(System.Collections.Generic.HashSet<IMessageProcessor> htMessageProcessor);
    void Connect(IMessageConnectionAccessor connectionAccessor);
}

public interface IMessageConnectionAccessor {
    bool TryGetSinks(
       NodeIdentifier sourceId,
       [MaybeNullWhen(false)] out ImmutableArray<IMessageIncomingSink> result);
}

public interface IMessageIncomingSink
    : IWithName {
    ValueTask ReceiveMessageAsync(RootMessage message, CancellationToken cancellationToken);
}

public interface IMessageIncomingSink<T>
    : IMessageIncomingSink
    where T : RootMessage {
    ValueTask ReceiveDataAsync(T message, CancellationToken cancellationToken);
}

public interface IMessageIncomingSinkInternal : IMessageIncomingSink {
    void CollectMessageProcessor(System.Collections.Generic.HashSet<IMessageProcessor> htMessageProcessor);
}


public interface IMessageConnection {
    IMessageOutgoingSource OutgoingSource { get; }
    IMessageIncomingSink IncomingSink { get; }
}

public interface IMessageConnection<T>
    where T : RootMessage {
    IMessageOutgoingSource<T> OutgoingSourceData { get; }
    IMessageIncomingSink<T> IncomingSinkData { get; }
}

public interface IMessageConnectionInternal : IMessageConnection {
    void CollectMessageProcessor(System.Collections.Generic.HashSet<IMessageProcessor> htMessageProcessor);
}

public interface IMessageEngine
    : IDisposableWithState {
    IMessageOutgoingSource GlobalOutgoingSource { get; }
    IMessageIncomingSink GlobalIncomingSink { get; }
    void ConnectMessage(IMessageOutgoingSource outgoingSource, IMessageIncomingSink incomingSink);
    void ConnectData<T>(IMessageOutgoingSource<T> outgoingSource, IMessageIncomingSink<T> incomingSink)
        where T : RootMessage;

    //ValueTask BootAsync(CancellationToken cancellationToken);

    ValueTask StartAsync(CancellationToken cancellationToken);

    ValueTask ExecuteAsync(CancellationToken cancellationToken);

    //ValueTask ShutdownAsync(CancellationToken cancellationToken);
}
