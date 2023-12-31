namespace Brimborium.MessageFlow;

//[System.Flags]
//public enum MessageAction {
//    None = 0x00,
//    Disconnect = 0x01,
//    Propagate = 0x02,
//    Control = 0x04,
//    Data = 0x08,
//    Flow = 0x10,
//    Group = 0x20,
//    Start = 0x40,
//    End = 0x80,
//    FlowStart = MessageAction.Flow| MessageAction.Start,
//    FlowEnd = MessageAction.Flow | MessageAction.End | MessageAction.Disconnect | MessageAction.Propagate
//}

public record class FlowMessage(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt
    ) {
    public DateTimeOffset SendAt { get; set; } = DateTimeOffset.UnixEpoch;

    public MessageLog ToRootMessageLog() => new(this);

    public virtual string? GetExtraInfo() => default;

    // public virtual MessageAction GetMessageAction() => MessageAction.None;

}

public static class FlowMessageExtension { 
    public static T Normalize<T>(this T message, NodeIdentifier nameId) 
        where T: FlowMessage {
        if (message.SourceId.Id == 0) {
            message = message with { SourceId = nameId };
        }
        if (message.CreatedAt == DateTimeOffset.MinValue) {
            message = message with { CreatedAt = DateTimeOffset.UtcNow };
        }
        return message;
    }
}

public readonly struct MessageLog(FlowMessage rootMessage) {
    private readonly FlowMessage _RootMessage = rootMessage;

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

    public override string ToString() 
        => this.ToString(new StringBuilder()).ToString();

    public StringBuilder ToString(StringBuilder sb) {
        sb.Append(this.MessageType).Append(':');
        this.MessageId.ToString(sb).Append(' ');
        return sb;
    }
}

public record class MessageData(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt
    ) : FlowMessage(MessageId, SourceId, CreatedAt) {
    //public override MessageAction GetMessageAction() => MessageAction.Data;
}

public record class MessageData<TData>(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt,
    TData Data
    ) : MessageData(MessageId, SourceId, CreatedAt) {
    public static MessageData<TData> Create(TData data)
        => new(MessageIdentifier.CreateMessageIdentifier(), NodeIdentifier.Empty, DateTimeOffset.UtcNow, data);

    //public override MessageAction GetMessageAction() => MessageAction.Data;
}

public record class MessageGroupStart(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt
    ) : FlowMessage(MessageId, SourceId, CreatedAt) {

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

    //public override MessageAction GetMessageAction() => MessageAction.Control | MessageAction.Group | MessageAction.Start;
}

public record class MessageGroupData<TData>(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt,
    TData Data
    ) : MessageData<TData>(MessageId, SourceId, CreatedAt, Data) {
    //public override MessageAction GetMessageAction() => MessageAction.Control | MessageAction.Group | MessageAction.Data;
}

public record class MessageGroupEnd(
    MessageIdentifier MessageId,
    NodeIdentifier SourceId,
    DateTimeOffset CreatedAt,
    Exception? Error
    ) : FlowMessage(MessageId, SourceId, CreatedAt) {
    //public override MessageAction GetMessageAction() => MessageAction.Control | MessageAction.Group | MessageAction.End;
}
