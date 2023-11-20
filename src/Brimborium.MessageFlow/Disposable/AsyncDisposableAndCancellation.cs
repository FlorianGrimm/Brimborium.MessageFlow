namespace Brimborium.MessageFlow.Disposable;

public interface IAsyncDisposableAndCancellation
    : IDisposableAndCancellation
    , IAsyncDisposable {
}

public class AsyncDisposableAndCancellation
    : DisposableAndCancellation
    , IAsyncDisposableAndCancellation
    , IAsyncDisposable
{

    protected AsyncDisposableAndCancellation(ILogger? logger = default)
        : base(logger)
    {
    }

    protected virtual ValueTask<bool> DisposeAsync(bool disposing)
    {
        var result = this.Dispose(disposing);
        return ValueTask.FromResult(result);
    }

    public async ValueTask DisposeAsync()
    {
        await this.DisposeAsync(true);
        System.GC.SuppressFinalize(this);
    }
}