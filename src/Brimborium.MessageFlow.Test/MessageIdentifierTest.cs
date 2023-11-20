namespace Brimborium.MessageFlow.Test;

public class MessageIdentifierTest {
    [Fact]
    public void MessageIdentifierTest001CreateMessageIdentifier() {
        var a = MessageIdentifier.CreateMessageIdentifier();
        var b = MessageIdentifier.CreateMessageIdentifier();
        Assert.True(a.MessageId < b.MessageId);
    }

    [Fact]
    public void MessageIdentifierTest002() {
        var a = MessageIdentifier.CreateGroupMessageIdentifier();
        var b = a.GetNextGroupMessageIdentifier();
        var c = b.GetNextGroupMessageIdentifier();
        Assert.True(a.MessageId < b.MessageId);
        Assert.True(b.MessageId < c.MessageId);
        Assert.Equal(a.GroupId, b.GroupId);
        Assert.Equal(b.GroupId, c.GroupId);
    }
}