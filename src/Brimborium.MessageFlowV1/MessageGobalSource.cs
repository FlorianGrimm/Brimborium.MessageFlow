namespace Brimborium.MessageFlow;

public class MessageGobalSource(
        NodeIdentifier nameId,
        ILogger? logger)
    : DisposableWithState(
        logger)
    , IWithCoordinatorNode {
    protected readonly NodeIdentifier _NameId = nameId;
    protected readonly Dictionary<string, IMessageOutgoingSource> _DictOutgoingSource = new(StringComparer.Ordinal);
    protected ImmutableArray<IMessageOutgoingSource> _ListOutgoingSource = [];

    public NodeIdentifier NameId => this._NameId;

    public IMessageOutgoingSource<T>? GetOutgoingSource<T>(string? name = default)
        where T : RootMessage {
        var childName = $"{name}-{Internal.TypeNameHelper.GetTypeDisplayNameCached(typeof(T))}";
        lock (this._DictOutgoingSource) {
            if (this._DictOutgoingSource.TryGetValue(childName, out var result)) {
                if (result is IMessageOutgoingSource<T> resultT) {
                    return resultT;
                } else {
                    return null;
                }
            } else {
                var resultT = new MessageOutgoingSourceMultiTarget<T>(
                    NodeIdentifier.CreateChild(this._NameId, childName),
                    this.Logger
                    );
                if (this._DictOutgoingSource.TryAdd(childName, resultT)) {
                    this.StateVersion++;
                    this._ListOutgoingSource = this._DictOutgoingSource.Values.ToImmutableArray();
                    return resultT;
                } else {
                    return null;
                }
            }
        }
    }

    public IMessageOutgoingSource<T> GetOutgoingSourceD<T>(string? name = default)
        where T : RootMessage
        => this.GetOutgoingSource<T>(name) ?? throw new ArgumentException("cannot resolve name to a source", nameof(name));

    public async ValueTask SendControlAsync(RootMessage message, CancellationToken cancellationToken) {
        var listOutgoingSource = this._ListOutgoingSource;
        foreach (var outgoingSource in listOutgoingSource) {
            await outgoingSource.SendControlAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }

    public bool CollectCoordinatorNode(HashSet<CoordinatorNode> listTarget) {
#warning TODO
        return false;
    }
}
public interface IMessageGobalSource {
    ValueTask SendControlAsync(RootMessage message, CancellationToken cancellationToken);
}
