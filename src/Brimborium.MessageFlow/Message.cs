namespace Brimborium.MessageFlow;

public record class RootMessage(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt
    ) {
    public MessageLog ToRootMessageLog()
        => new MessageLog(this);

    public virtual string? GetExtraInfo() => default;
}

public struct MessageLog {
    private readonly RootMessage _RootMessage;

    public MessageLog(RootMessage rootMessage) {
        this._RootMessage = rootMessage;
    }

    public string MessageType
        => Brimborium.MessageFlow.Internal.TypeNameHelper.GetTypeDisplayNameCached(this._RootMessage.GetType(), fullName: false);

    public MessageIdentifier MessageId
        => this._RootMessage.MessageId;

    public NodeIdentifier SourceId
        => this._RootMessage.SourceId;

    public DateTimeOffset CreatedAt
        => this._RootMessage.CreatedAt;

    public string? ExtraInfo
        => this._RootMessage.GetExtraInfo();
}


public record class MessageData<TData>(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt,
    TData Data
    ) : RootMessage(MessageId, SourceId, CreatedAt) {
}

public record class GroupMessageStart(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt
    ) : RootMessage(MessageId, SourceId, CreatedAt) {
}

public record class GroupMessageStart<TData>(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt,
    TData Data
    ) : GroupMessageStart(MessageId, SourceId, CreatedAt) {
}


public record class GroupMessageData<TData>(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt,
    TData Data
    ) : RootMessage(MessageId, SourceId, CreatedAt) {
}

public record class GroupMessageStop(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt
    ) : RootMessage(MessageId, SourceId, CreatedAt) {
}

public record class GroupMessageStop<TData>(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt
    ) : GroupMessageStop(MessageId, SourceId, CreatedAt) {
}
