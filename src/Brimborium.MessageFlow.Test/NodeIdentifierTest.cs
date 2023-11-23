namespace Brimborium.MessageFlow.Test;

public class NodeIdentifierTest {
    [Fact]
    public void NodeIdentifierTest001ToString() {
        var a = NodeIdentifier.Create("abc");
        Assert.StartsWith("abc#", a.ToString());

        var b = NodeIdentifier.Create("def");
        Assert.StartsWith("def#", b.ToString());
        Assert.True(a.Id < b.Id);

        var c = NodeIdentifier.CreateChild(a, "ghi");
        Assert.StartsWith("abc#", c.ToString());
        Assert.Contains("ghi#", c.ToString());
    }

    [Fact]
    public void NodeIdentifierTest002Parse() {
        var a = NodeIdentifier.Create("abc");
        var b = NodeIdentifier.Create("def");
        var c = NodeIdentifier.CreateChild(a, "ghi");

        {
            var aP = NodeIdentifier.Parse(a.ToString());
            Assert.NotNull(aP);
            Assert.Equal(a.Name, aP.Name);
            Assert.Equal(a.Id, aP.Id);
        }

        {
            var bP = NodeIdentifier.Parse(b.ToString());
            Assert.NotNull(bP);
            Assert.Equal(b.Name, bP.Name);
            Assert.Equal(b.Id, bP.Id);
        }

        {
            var cP = NodeIdentifier.Parse(c.ToString());
            Assert.NotNull(cP);
            Assert.Equal(c.Name, cP.Name);
            Assert.Equal(c.Id, cP.Id);
            Assert.NotNull(c.Parent);
            Assert.NotNull(cP.Parent);
            Assert.Equal(c.Parent.Name, cP.Parent.Name);
            Assert.Equal(c.Parent.Id, cP.Parent.Id);
        }
    }

    [Fact]
    public void NodeIdentifierTest003NodeIdentifierJsonConverter() {
        JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonSerializerOptions.Converters.Add(new NodeIdentifierJsonConverter());
        //
        var a = NodeIdentifier.Create("abc");
        var b = NodeIdentifier.Create("def");
        var c = NodeIdentifier.CreateChild(a, "ghi");
        
        {
            var json = JsonSerializer.Serialize<NodeIdentifier>(a, jsonSerializerOptions);
            Assert.NotNull(json);
            var aP = JsonSerializer.Deserialize<NodeIdentifier>(json, jsonSerializerOptions);

            Assert.NotNull(aP);
            Assert.Equal(a.Name, aP.Name);
            Assert.Equal(a.Id, aP.Id);
        }

        {
            var json = JsonSerializer.Serialize<NodeIdentifier>(b, jsonSerializerOptions);
            Assert.NotNull(json);
            var bP = JsonSerializer.Deserialize<NodeIdentifier>(json, jsonSerializerOptions);

            Assert.NotNull(bP);
            Assert.Equal(b.Name, bP.Name);
            Assert.Equal(b.Id, bP.Id);
        }

        {
            var json = JsonSerializer.Serialize<NodeIdentifier>(c, jsonSerializerOptions);
            Assert.NotNull(json);
            var cP = JsonSerializer.Deserialize<NodeIdentifier>(json, jsonSerializerOptions);

            Assert.NotNull(cP);
            Assert.Equal(c.Name, cP.Name);
            Assert.Equal(c.Id, cP.Id);
            Assert.NotNull(c.Parent);
            Assert.NotNull(cP.Parent);
            Assert.Equal(c.Parent.Name, cP.Parent.Name);
            Assert.Equal(c.Parent.Id, cP.Parent.Id);
        }
    }
}
