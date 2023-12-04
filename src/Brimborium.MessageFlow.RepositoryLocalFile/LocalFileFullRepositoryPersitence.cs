namespace Brimborium.MessageFlow.RepositoryLocalFile;
public class LocalFileFullRepositoryPersitence<T>(
    string folderPath,
    JsonUtilities jsonUtilities,
    ILogger logger
    )
    where T : class {
    private bool _FolderPathExists = false;

    public async Task<Optional<T>> LoadAsync(CancellationToken cancellationToken) {
        Optional<T> result = new();
        System.IO.DirectoryInfo di = new DirectoryInfo(folderPath);
        var listFileInfo = di.GetFiles("*.json");
        var optStateDiffFileNames = FileNameUtilities.GetLatestStateFileName(listFileInfo);
        if (optStateDiffFileNames.TryGetNoValue()) {
            return NoValue.Value;
        }
        if (optStateDiffFileNames.TryGetValue(out var stateDiffFileNames)) {
            T state;
            if (!string.IsNullOrEmpty(stateDiffFileNames.StateFileName)) {
                var stateFullName = System.IO.Path.Combine(folderPath, stateDiffFileNames.StateFileName);
                result = await this.LoadFullStateAsync(stateFullName, cancellationToken);
                if (result.TryGetValue(out var resultState)) {
                    state = resultState;
                } else { 
                    return NoValue.Value;
                }
            } else {
                return NoValue.Value;
            }
            if (stateDiffFileNames.ListDiffFileName.Count > 0) {
                // TODO: nice
                throw new Exception();
            }
        }

        return result;
    }

    private async Task<Optional<T>> LoadFullStateAsync(string filename, CancellationToken cancellationToken) {
        var fi = new System.IO.FileInfo(filename);
        if (fi.Exists) {
            using (var fileStream = System.IO.File.OpenRead(filename)) {
                var result = await jsonUtilities.DeserializeAsync<T>(fileStream, cancellationToken);
                return result;
            }
        } else {
            return new();
        }

    }

    public async Task<ICommitable> SaveFullStateAsync(T value, CancellationToken cancellationToken) {
        var utcNow = System.DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss-ffffff");
        string tempFileName = System.IO.Path.Combine(folderPath, $"{utcNow}-state.temp");
        string dataFileName = System.IO.Path.Combine(folderPath, $"{utcNow}-state.json");
        if (!this._FolderPathExists) { 
            System.IO.Directory.CreateDirectory(folderPath);
            this._FolderPathExists = true;
        }
        using var stream = new System.IO.FileStream(tempFileName, FileMode.CreateNew, FileAccess.Write);
        await jsonUtilities.SerializeAsync(stream, value, cancellationToken);
        return new LocalFileRepositoryPersitenceSaveCommit(tempFileName, dataFileName, logger);
    }
}