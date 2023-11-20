namespace Brimborium.MessageFlow;

public record class RootMessage(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt
    ) {
    public DateTimeOffset SendAt { get; set; } = DateTimeOffset.UnixEpoch;

    public MessageLog ToRootMessageLog() => new(this);

    public virtual string? GetExtraInfo() => default;
}

public readonly struct MessageLog(RootMessage rootMessage) {
    private readonly RootMessage _RootMessage = rootMessage;

    public string MessageType
        => Brimborium.MessageFlow.Internal.TypeNameHelper.GetTypeDisplayNameCached(this._RootMessage.GetType(), fullName: false);

    public MessageIdentifier MessageId
        => this._RootMessage.MessageId;

    public NodeIdentifier SourceId
        => this._RootMessage.SourceId;

    public DateTimeOffset CreatedAt
        => this._RootMessage.CreatedAt;

    public DateTimeOffset SendAt
        => this._RootMessage.SendAt;

    public string? ExtraInfo
        => this._RootMessage.GetExtraInfo();
}


public sealed record class MessageFlowStart(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt
    ) : RootMessage(MessageId, SourceId, CreatedAt) {

    public static MessageFlowStart CreateStart(NodeIdentifier? nameId)
        => new(MessageIdentifier.CreateGroupMessageIdentifier(), nameId ?? NodeIdentifier.Empty, DateTimeOffset.UtcNow);

    public MessageFlowEnd CreateEnd(Exception? error = default)
        => new(this.MessageId.GetNextGroupMessageIdentifier(), this.SourceId, DateTimeOffset.UtcNow, error);
}

public sealed record class MessageFlowEnd(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt,
    Exception? Error
    ) : RootMessage(MessageId, SourceId, CreatedAt) {
}

public sealed record class MessageFlowReport(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt,
    CoordinatorCollector CoordinatorCollector
    ) : RootMessage(MessageId, SourceId, CreatedAt) {
    public static MessageFlowReport Create(NodeIdentifier? nameId, CoordinatorCollector? coordinatorCollector)
        => new(
            MessageIdentifier.CreateMessageIdentifier(),
            nameId ?? NodeIdentifier.Empty,
            DateTimeOffset.UtcNow,
            coordinatorCollector ?? new());
}


public record class MessageData<TData>(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt,
    TData Data
    ) : RootMessage(MessageId, SourceId, CreatedAt) {
}

public record class MessageGroupStart(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt
    ) : RootMessage(MessageId, SourceId, CreatedAt) {

    public static MessageGroupStart CreateStart(NodeIdentifier? nameId)
        => new(
            MessageIdentifier.CreateGroupMessageIdentifier(),
            nameId ?? NodeIdentifier.Empty,
            DateTimeOffset.UtcNow);

    public MessageGroupData<TData> CreateData<TData>(TData data)
        => new(
            this.MessageId.GetNextGroupMessageIdentifier(),
            this.SourceId,
            DateTimeOffset.UtcNow,
            data);

    public MessageGroupEnd CreateEnd(Exception? error = default)
        => new(
            this.MessageId.GetNextGroupMessageIdentifier(),
            this.SourceId,
            DateTimeOffset.UtcNow,
            error);
}

public record class MessageGroupData<TData>(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt,
    TData Data
    ) : MessageData<TData>(MessageId, SourceId, CreatedAt, Data) {
}

public record class MessageGroupEnd(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt,
    Exception? Error
    ) : RootMessage(MessageId, SourceId, CreatedAt) {
}
