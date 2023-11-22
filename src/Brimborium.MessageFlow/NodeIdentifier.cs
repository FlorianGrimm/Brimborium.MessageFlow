namespace Brimborium.MessageFlow;

internal static class NodeIdentifierInternal {

    private static long _Id = 1;
    internal static long GetNextId() {
        return Interlocked.Increment(ref _Id);
    }
}

public record class NodeIdentifier(
    long Id,
    string Name,
    NodeIdentifier? Parent
    ) {
    public static NodeIdentifier Unknown => new(-1, "?", default);
    public static NodeIdentifier Empty => new(0, string.Empty, default);

    public static NodeIdentifier Create(string name) {
        return new NodeIdentifier(
            NodeIdentifierInternal.GetNextId(),
            name,
            default);
    }

    public static NodeIdentifier CreateChild(NodeIdentifier parent, string name) {
        return new NodeIdentifier(
            NodeIdentifierInternal.GetNextId(),
            name,
            parent);
    }

    public override string ToString()
        => this.ToString(new StringBuilder()).ToString();

    public StringBuilder ToString(StringBuilder sb) {
        if (this.Parent is not null) {
            this.Parent.ToString(sb);
            sb.Append('/');
        }
        sb.Append(this.Name).Append('#').Append(this.Id);
        return sb;
    }

    public static implicit operator NodeIdentifier(string name)
        => new(NodeIdentifierInternal.GetNextId(), name, default);

    public static NodeIdentifier operator +(NodeIdentifier parent, NodeIdentifier right)
        => new(NodeIdentifierInternal.GetNextId(), right.Name, parent);
}
