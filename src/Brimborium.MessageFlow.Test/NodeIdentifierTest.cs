namespace Brimborium.MessageFlow.Test;

public class NodeIdentifierTest {
    [Fact]
    public void NodeIdentifierTest001() {
        var a = NodeIdentifier.Create("abc");
        Assert.StartsWith("abc#", a.ToString());

        var b = NodeIdentifier.Create("def");
        Assert.StartsWith("def#", b.ToString());
        Assert.True(a.Id < b.Id);

        var c = NodeIdentifier.CreateChild(a, "ghi");
        Assert.StartsWith("abc#", c.ToString());
        Assert.Contains("ghi#", c.ToString());
    }
}
