
namespace Brimborium.MessageFlow;

public interface IWithCoordinatorNode {
    void CollectCoordinatorNode(HashSet<CoordinatorNode> listTarget);
}

public record CoordinatorNode(
    NodeIdentifier NameId,
    List<NodeIdentifier>? ListSourceId = default,
    List<CoordinatorNodeSink>? ListSink = default,
    List<NodeIdentifier>? ListChildren = default
) {
    public int Order { get; set; } = 0;
    public StringBuilder ToString(StringBuilder sb) {
        this.NameId.ToString(sb);

        if (this.ListSourceId is not null && this.ListSourceId.Count > 0) {
            sb.Append(", Source:[");
            foreach (var item in this.ListSourceId) {
                item.ToString(sb);
            }
            sb.Append(']');
        }

        if (this.ListSink is not null && this.ListSink.Count > 0) {
            sb.Append(", Sink:[");
            foreach (var item in this.ListSink) {
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

public class EqualityComparerCoordinatorNodeNameId : IEqualityComparer<CoordinatorNode> {
    public bool Equals(CoordinatorNode? x, CoordinatorNode? y) {
        if (ReferenceEquals(x, y)) { return true; }
        if (x is null) { return false; }
        if (y is null) { return false; }
        return x.NameId.Equals(y.NameId);
    }

    public int GetHashCode([DisallowNull] CoordinatorNode obj) => obj.GetHashCode();
}

public record CoordinatorNodeSink(
    NodeIdentifier SinkId,
    List<NodeIdentifier>? ListSourceId = default) {
    public StringBuilder ToString(StringBuilder sb) {
        this.SinkId.ToString(sb);

        if (this.ListSourceId is not null && this.ListSourceId.Count > 0) {
            sb.Append(", ConnectedFrom: [");
            foreach (var item in this.ListSourceId) {
                item.ToString(sb);
                sb.Append(", ");
            }
            sb.Append(']');
        }

        return sb;
    }
    public override string ToString() => this.ToString(new StringBuilder()).ToString();
}

public class CoordinatorCollector {
    private readonly HashSet<CoordinatorNode> _HashSetTarget;

    public CoordinatorCollector() {
        this._HashSetTarget = new HashSet<CoordinatorNode>();
    }

    public CoordinatorCollector CollectCoordinatorNode(
        IWithCoordinatorNode? withCoordinatorNode
        ) {
        withCoordinatorNode?.CollectCoordinatorNode(this._HashSetTarget);
        return this;
    }
}