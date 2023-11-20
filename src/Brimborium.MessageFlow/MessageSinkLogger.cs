namespace Brimborium.MessageFlow;

public class MessageSinkLogger<T>
    : MessageSinkChannel<T>
    where T : RootMessage {
    private readonly ILogger _Logger;

    [ActivatorUtilitiesConstructor]
    public MessageSinkLogger(
        ILogger<MessageSinkLogger<T>> logger
        ) : base(
            NodeIdentifier.Create("Logger"),
            default,
            logger
            ) {
        this._Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public MessageSinkLogger(
        NodeIdentifier sinkId,
        ChannelOptions? channelOptions,
        ILogger logger
        ) : base(sinkId, channelOptions, logger) {
        this._Logger = logger;
    }

    public override ValueTask HandleDataMessageAsync(T message, CancellationToken cancellationToken) {
        this._Logger.LogSinkLoggerMessage(message.ToRootMessageLog());
        return ValueTask.CompletedTask;
    }

    public override ValueTask HandleControlMessageAsync(RootMessage message, CancellationToken cancellationToken) {
        this._Logger.LogSinkLoggerControlMessage(message.ToRootMessageLog());
        return ValueTask.CompletedTask;
    }
}
