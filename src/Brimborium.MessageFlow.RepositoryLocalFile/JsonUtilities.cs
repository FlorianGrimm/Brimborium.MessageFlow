namespace Brimborium.MessageFlow.RepositoryLocalFile;

public abstract class JsonUtilities {
    protected static readonly Microsoft.IO.RecyclableMemoryStreamManager Manager
        = new Microsoft.IO.RecyclableMemoryStreamManager();
    public JsonUtilities() {
    }

    public abstract void Serialize<T>(Stream stream, T value);

    public abstract T? Deserialize<T>(Stream stream);


    public abstract Task SerializeLines<T>(Stream stream, List<T> listValue, CancellationToken cancellationToken);

    public abstract Task<List<T>> DeserializeLines<T>(Stream stream, CancellationToken cancellationToken);
}
