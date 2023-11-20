namespace Brimborium.MessageFlow.Disposable;

public interface IDisposableAndCancellation
    : IDisposable {
    bool GetIsDisposed();
    void ThrowIfDisposed();
}

public class DisposableAndCancellation
    : IDisposableAndCancellation
    , IDisposable {

    private bool _IsDisposed;
    private CancellationTokenSource? _ExecutionCTS;
    protected readonly ILogger Logger;

    protected DisposableAndCancellation(
        ILogger? logger = default
        ) {
        this.Logger = logger ?? DummyILogger.Instance;
    }
    protected virtual bool Dispose(bool disposing) {
        if (!this._IsDisposed) {
            this._IsDisposed = true;
            if (disposing) {
                using (var disposeTokenSource = this._ExecutionCTS) {
                    disposeTokenSource?.Cancel();
                }
            } else {
                this._ExecutionCTS?.Cancel();
                this.Logger.LogWarning("{FullName} is disposed by the destructor", GetType().FullName);
            }
            return true;
        } else {
            return false;
        }
    }

    ~DisposableAndCancellation() {
        this.Dispose(disposing: false);
    }

    public void Dispose() {
        this.Dispose(disposing: true);
        System.GC.SuppressFinalize(this);
    }

    bool IDisposableAndCancellation.GetIsDisposed() => GetIsDisposed();

    protected bool GetIsDisposed() => this._IsDisposed;

    void IDisposableAndCancellation.ThrowIfDisposed() => ThrowIfDisposed();

    protected void ThrowIfDisposed() {
        if (this._IsDisposed) {
            throw new ObjectDisposedException(GetType().FullName ?? string.Empty);
        }
    }

    protected CancellationToken GetExecutionCancellationToken(CancellationToken cancellationToken) {
        if (this._IsDisposed) {
            return new CancellationToken(true);
        }
        if (this._ExecutionCTS is null) {
            this._ExecutionCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }
        return this._ExecutionCTS.Token;
    }

    protected bool TryGetExecutionCancellationTokenSource([MaybeNullWhen(false)] out CancellationTokenSource executionCTS) {
        if (this._ExecutionCTS is CancellationTokenSource cts) {
            executionCTS = cts;
            return true;
        } else {
            executionCTS = default;
            return false;
        }
    }
}
