namespace Brimborium.MessageFlow.RepositoryLocalFile;

public record FullDiffState<TFull, TDiff>(
    TFull State,
    int DiffCount
    )
    where TFull : class
    where TDiff : class;

public interface IFullDiffRepositoryStateOperation<TFull, TDiff>
    where TFull : class
    where TDiff : class {

    int GetFullCount(TFull state);

    FullDiffState<TFull, TDiff> SetFullState(TFull resultState);

    FullDiffState<TFull, TDiff> AddDiffState(FullDiffState<TFull, TDiff> state, TDiff diffState);
}


public class LocalFileFullDiffRepositoryPersitence<TFull, TDiff>(
    string folderPath,
    JsonUtilities jsonUtilities,
    ILogger logger
    )
    where TFull : class
    where TDiff : class {
    private bool _FolderPathExists = false;

    public async Task<Optional<FullDiffState<TFull, TDiff>>> LoadAsync(
        IFullDiffRepositoryStateOperation<TFull, TDiff> operation,
        CancellationToken cancellationToken) {
        var optStateDiffFileNames = FileNameUtilities.GetLatestStateFileNameFromFolder(folderPath);
        if (optStateDiffFileNames.TryGetNoValue()) {
            return NoValue.Value;
        }
        if (optStateDiffFileNames.TryGetValue(out var stateDiffFileNames)) {
            FullDiffState<TFull, TDiff> state;
            if (!string.IsNullOrEmpty(stateDiffFileNames.StateFileName)) {
                var stateFullName = System.IO.Path.Combine(folderPath, stateDiffFileNames.StateFileName);
                var optFull = await this.LoadFullStateAsync(stateFullName, cancellationToken);
                if (optFull.TryGetValue(out var resultState)) {
                    state = operation.SetFullState(resultState);
                        
                } else {
                    return NoValue.Value;
                }
            } else {
                return NoValue.Value;
            }
            if (stateDiffFileNames.ListDiffFileName.Count > 0) {
                foreach (var diffFileName in stateDiffFileNames.ListDiffFileName) {
                    var diffFullName = System.IO.Path.Combine(folderPath, diffFileName);
                    var optDiffState = await this.LoadDiffStateAsync(diffFullName, cancellationToken);
                    if (optDiffState.TryGetValue(out var diffState)) {
                        state = operation.AddDiffState(state, diffState);
                    } else {
                        return NoValue.Value;
                    }
                }
            }
            return new(state);
        }
        return NoValue.Value;
    }

    private async Task<Optional<TFull>> LoadFullStateAsync(string filename, CancellationToken cancellationToken) {
        var fi = new System.IO.FileInfo(filename);
        if (fi.Exists) {
            using (var fileStream = System.IO.File.OpenRead(filename)) {
                var result = await jsonUtilities.DeserializeAsync<TFull>(fileStream, cancellationToken);
                return result;
            }
        } else {
            return new();
        }

    }
    private async Task<Optional<TDiff>> LoadDiffStateAsync(string filename, CancellationToken cancellationToken) {
        var fi = new System.IO.FileInfo(filename);
        if (fi.Exists) {
            // TODO:
            await Task.CompletedTask;
            //using (var fileStream = System.IO.File.OpenRead(filename)) {
            //    var result = await jsonUtilities.Deserialize<T>(fileStream, cancellationToken);
            //    return result;
            //}
            return new();
        } else {
            return new();
        }

    }

    public async Task<ICommitable> SaveFullStateAsync(TFull value, CancellationToken cancellationToken) {
        var utcNow = System.DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss-ffffff");
        string tempFileName = System.IO.Path.Combine(folderPath, $"{utcNow}-state.temp");
        string dataFileName = System.IO.Path.Combine(folderPath, $"{utcNow}-state.json");
        if (!this._FolderPathExists) {
            System.IO.Directory.CreateDirectory(folderPath);
            this._FolderPathExists = true;
        }
        using var stream = new System.IO.FileStream(tempFileName, FileMode.CreateNew, FileAccess.Write);
        await jsonUtilities.SerializeAsync<TFull>(stream, value, cancellationToken);
        return new LocalFileRepositoryPersitenceSaveCommit(tempFileName, dataFileName, logger);
    }

    public async Task<ICommitable> SaveDiffStateAsync(TDiff value, CancellationToken cancellationToken) {
        var utcNow = System.DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss-ffffff");
        string tempFileName = System.IO.Path.Combine(folderPath, $"{utcNow}-diff.temp");
        string dataFileName = System.IO.Path.Combine(folderPath, $"{utcNow}-diff.json");
        if (!this._FolderPathExists) {
            System.IO.Directory.CreateDirectory(folderPath);
            this._FolderPathExists = true;
        }
        using var stream = new System.IO.FileStream(tempFileName, FileMode.CreateNew, FileAccess.Write);
        await jsonUtilities.SerializeAsync<TDiff>(stream, value, cancellationToken);
        return new LocalFileRepositoryPersitenceSaveCommit(tempFileName, dataFileName, logger);
    }
}


