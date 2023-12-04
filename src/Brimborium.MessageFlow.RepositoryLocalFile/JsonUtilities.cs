namespace Brimborium.MessageFlow.RepositoryLocalFile;

public abstract class JsonUtilities {
    protected static readonly Microsoft.IO.RecyclableMemoryStreamManager Manager
        = new Microsoft.IO.RecyclableMemoryStreamManager();
    public JsonUtilities() {
    }

    public abstract Task SerializeAsync<T>(Stream stream, T value, CancellationToken cancellationToken);

    public abstract Task<Optional<T>> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken);

    public abstract Task SerializeLinesAsync<T>(Stream stream, List<T> listValue, CancellationToken cancellationToken);

    public abstract Task<List<T>> DeserializeLines<T>(Stream stream, CancellationToken cancellationToken);
}
