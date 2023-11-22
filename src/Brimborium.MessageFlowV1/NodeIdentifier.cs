namespace Brimborium.MessageFlow;


internal static class MessageEdgeIdentifierInternal {

    private static long _Id = 1;
    internal static long GetNextId() {
        return Interlocked.Increment(ref _Id);
    }
}

public record NodeIdentifier(
    long Id,
    string Name
    ) {
    public static NodeIdentifier Unknown => new(-1, "?");
    public static NodeIdentifier Empty => new(0, string.Empty);
    
    public static NodeIdentifier Create(string name) {
        return new NodeIdentifier(
            MessageEdgeIdentifierInternal.GetNextId(),
            name);
    }

    public static NodeIdentifier CreateChild(NodeIdentifier parent, string name) {
        return new NodeIdentifier(
            MessageEdgeIdentifierInternal.GetNextId(),
            $"{parent.Name}/{name}");
    }

    public override string ToString() => $"{this.Name}#{this.Id}";

    public StringBuilder ToString(StringBuilder sb) => sb.Append(this.Name).Append('#').Append(this.Id);
}
