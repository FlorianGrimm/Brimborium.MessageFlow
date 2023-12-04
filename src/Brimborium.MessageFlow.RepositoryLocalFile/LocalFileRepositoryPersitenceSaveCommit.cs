namespace Brimborium.MessageFlow.RepositoryLocalFile;
public interface ICommitable {
    void Commit();
}

public class LocalFileRepositoryPersitenceSaveCommit(
    string tempFileName,
    string dataFileName,
    ILogger logger)
    : DisposableWithState(logger), ICommitable {
    private bool _IsMoved = false;

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
