namespace Brimborium.MessageFlow;

internal static class MessageIdentifierInternal {

    private static long _MessageId = 1;
    internal static long GetNextMessageId() {
        return Interlocked.Increment(ref _MessageId);
    }
}

public readonly record struct MessageIdentifier(
    long MessageId,
    long GroupId
    ) {
    public static MessageIdentifier CreateMessageIdentifier() {
        return new MessageIdentifier(MessageIdentifierInternal.GetNextMessageId(), 0);
    }

    public static MessageIdentifier CreateGroupMessageIdentifier() {
        var messageId = MessageIdentifierInternal.GetNextMessageId();
        return new MessageIdentifier(messageId, messageId);
    }

    public MessageIdentifier GetNextGroupMessageIdentifier() {
        return new MessageIdentifier(MessageIdentifierInternal.GetNextMessageId(), this.GroupId);
    }
}
