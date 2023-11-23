namespace Brimborium.MessageFlow;

public record MessageFlowGraph(
    List<MessageGraphNode> ListNode,
    List<MessageGraphConnection> ListConnection
    ) {
    public MessageFlowGraph() : this(new(), new()) { }
}

public record MessageGraphNode(
    NodeIdentifier NameId,
    List<NodeIdentifier> ListOutgoingSourceId,
    List<NodeIdentifier> ListIncomingSinkId,
    List<NodeIdentifier> ListChildren
) {
    public int Order { get; set; } = 0;
    public StringBuilder ToString(StringBuilder sb) {
        this.NameId.ToString(sb);

        if (this.ListOutgoingSourceId is not null && this.ListOutgoingSourceId.Count > 0) {
            sb.Append(", Source:[");
            foreach (var item in this.ListOutgoingSourceId) {
                item.ToString(sb);
            }
            sb.Append(']');
        }

        if (this.ListIncomingSinkId is not null && this.ListIncomingSinkId.Count > 0) {
            sb.Append(", Sink:[");
            foreach (var item in this.ListIncomingSinkId) {
                item.ToString(sb);
            }
            sb.Append(']');
        }

        if (this.ListChildren is not null && this.ListChildren.Count > 0) {
            sb.Append(", Children:[");
            foreach (var item in this.ListChildren) {
                item.ToString(sb);
            }
            sb.Append(']');
        }
        return sb;
    }
    public override string ToString() => this.ToString(new StringBuilder()).ToString();
}

public sealed class EqualityComparerCoordinatorNodeNameId : IEqualityComparer<MessageGraphNode> {
    private static EqualityComparerCoordinatorNodeNameId? _Instance;
    public static EqualityComparerCoordinatorNodeNameId Instance => _Instance ??= new EqualityComparerCoordinatorNodeNameId();

    public bool Equals(MessageGraphNode? x, MessageGraphNode? y) {
        if (ReferenceEquals(x, y)) { return true; }
        if (x is null) { return false; }
        if (y is null) { return false; }
        return x.NameId.Equals(y.NameId);
    }

    public int GetHashCode([DisallowNull] MessageGraphNode obj) => obj.GetHashCode();
}

public record MessageGraphConnection(
    NodeIdentifier SourceId,
    NodeIdentifier SourceNodeId,
    NodeIdentifier SinkId,
    NodeIdentifier SinkNodeId);