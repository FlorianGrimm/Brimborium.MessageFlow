namespace Brimborium.MessageFlow.Disposable;

public interface IAsyncDisposableAndCancellation
    : IDisposableWithState
    , IAsyncDisposable {
}

public class AsyncDisposableAndCancellation
    : DisposableWithState
    , IAsyncDisposableAndCancellation
    , IAsyncDisposable {

    protected AsyncDisposableAndCancellation(
        ILogger? logger)
        : base(
            logger: logger) {
    }

    protected virtual ValueTask<bool> DisposeAsync(bool disposing) {
        var result = this.Dispose(disposing);
        return ValueTask.FromResult(result);
    }

    public async ValueTask DisposeAsync() {
        await this.DisposeAsync(true);
        System.GC.SuppressFinalize(this);
    }
}