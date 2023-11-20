namespace Brimborium.MessageFlow.Disposable;

public interface IDisposableWithState
    : IDisposable {
    bool GetIsDisposed();
}

public class DisposableWithState
    : IDisposableWithState
    , IDisposable {
    protected readonly ILogger Logger;
    protected int _StateVersion;
    private bool _IsDisposed;

    protected DisposableWithState(
        ILogger? logger
        ) {
        this.Logger = logger ?? DummyILogger.Instance;
    }

    protected virtual bool Dispose(bool disposing) {
        if (!this._IsDisposed) {
            this._IsDisposed = true;
            this._StateVersion++;
            if (disposing) {
                //
            } else {
                this.Logger.LogWarning("{FullName} is disposed by the destructor", GetType().FullName);
            }
            return true;
        } else {
            return false;
        }
    }

    /*
    ~DisposableAndCancellation() {
        this.Dispose(disposing: false);
    }
    */

    public void Dispose() {
        this.Dispose(disposing: true);
        System.GC.SuppressFinalize(this);
    }

    bool IDisposableWithState.GetIsDisposed() => GetIsDisposed();

    protected bool GetIsDisposed() => this._IsDisposed;
}
