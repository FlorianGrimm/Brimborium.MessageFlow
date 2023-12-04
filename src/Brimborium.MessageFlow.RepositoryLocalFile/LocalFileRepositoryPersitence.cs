namespace Brimborium.MessageFlow.RepositoryLocalFile;

public class LocalFileRepositoryPersitence(
    string folderPath,
    JsonUtilities jsonUtilities,
    ILogger logger
    ) {
    private readonly string _FolderPath = folderPath;

    public LocalFileFullRepositoryPersitence<T> GetFullRepositoryPersitenceForType<T>(string? subFolder = default)
        where T : class {
        if (subFolder is null) {
            subFolder = typeof(T).Name;
        }
        var fullFolderPath = System.IO.Path.Combine(this._FolderPath, subFolder);
        return new LocalFileFullRepositoryPersitence<T>(fullFolderPath, jsonUtilities, logger);
    }

    public LocalFileFullDiffRepositoryPersitence<TFull, TDiff> GetFullDiffRepositoryPersitenceForType<TFull, TDiff>(string? subFolder = default)
        where TFull : class
        where TDiff : class {
        if (subFolder is null) {
            subFolder = typeof(TFull).Name;
        }
        var fullFolderPath = System.IO.Path.Combine(this._FolderPath, subFolder);
        return new LocalFileFullDiffRepositoryPersitence<TFull, TDiff>(fullFolderPath, jsonUtilities, logger);
    }
}
