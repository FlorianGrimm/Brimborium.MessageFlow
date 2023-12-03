


namespace Brimborium.MessageFlow.RepositoryLocalFile;

public class LocalFileRepositoryPersitence(
    string folderPath,
    ILogger logger
    ) {
    private readonly string _FolderPath = folderPath;

    public LocalFileRepositoryPersitence<T> GetForType<T>(string? subFolder = default)
        where T : class {
        if (subFolder is null) {
            subFolder = typeof(T).Name;
        }
        var fullFolderPath = System.IO.Path.Combine(this._FolderPath, subFolder);
        return new LocalFileRepositoryPersitence<T>(fullFolderPath, logger);
    }
}

public class LocalFileRepositoryPersitence<T>(
    string folderPath,
    ILogger logger
    )
    where T : class {
    public async Task<T?> LoadAsync(CancellationToken cancellationToken) {
        System.IO.DirectoryInfo di = new DirectoryInfo(folderPath);
        var files = di.GetFiles("*.json");
        var currentFile = files.FirstOrDefault();
        if (currentFile is null) { return default; }
        var content = await System.IO.File.ReadAllBytesAsync(currentFile.FullName);
        if (content is null || content.Length == 0) { return default; }
        var result = System.Text.Json.JsonSerializer.Deserialize<T>(content);
        return result;
    }

    public async Task<ICommitable> SaveAsync(T value, CancellationToken cancellationToken) {
        var utcNow = System.DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss-ffffff");
        string tempFileName = System.IO.Path.Combine(folderPath, $"{utcNow}-state.temp");
        string dataFileName = System.IO.Path.Combine(folderPath, $"{utcNow}-state.json");
        JsonSerializerOptions jsonSerializerOptions = JsonSerializerOptions.Default;
        System.IO.Directory.CreateDirectory(folderPath);
        using var stream = new System.IO.FileStream(tempFileName, FileMode.CreateNew, FileAccess.Write);
        await System.Text.Json.JsonSerializer.SerializeAsync<T>(stream, value, jsonSerializerOptions, cancellationToken);
        return new LocalFileRepositoryPersitenceSaveCommit(tempFileName, dataFileName, logger);
    }
}
public interface ICommitable {
    void Commit();
}
public class LocalFileRepositoryPersitenceSaveCommit(
    string tempFileName, 
    string dataFileName,
    ILogger logger)
    : DisposableWithState(logger), ICommitable {
    private bool _IsMoved=false;

    public void Commit() {
        // TODO: Log
        System.IO.File.Move(tempFileName, dataFileName);
        this._IsMoved = true;
        this.Dispose();
    }

    protected override bool Dispose(bool disposing) {
        if (base.Dispose(disposing)) {
            if (this._IsMoved) {
                // OK
            } else {
                // TODO: Log
                System.IO.File.Delete(tempFileName);
            }
            return true;
        } else {
            return false;
        }
    }
}
