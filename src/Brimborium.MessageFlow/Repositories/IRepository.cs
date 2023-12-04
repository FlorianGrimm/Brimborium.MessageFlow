namespace Brimborium.MessageFlow.Repositories;


public interface IRepository<TRepositoryState, TRepositoryTransaction>
    where TRepositoryState : class, IRepositoryState
    where TRepositoryTransaction : class, IRepositoryTransaction<TRepositoryState> {
    TRepositoryState State { get; }
}

public interface IRepositoryState {
}

public interface IRepositoryStateDiff {
}


public interface IRepositoryTransaction<TRepositoryState>
    : IDisposableWithState
    where TRepositoryState : class, IRepositoryState {

    ValueTask CommitAsync(RepositorySaveMode saveMode, CancellationToken cancellationToken);

    void Cancel();
}

public enum RepositorySaveMode { Auto, Full, Diff }

public interface IRepositoryPersitence<TRepositoryState, TRepositoryTransaction>
    where TRepositoryState : class, IRepositoryState
    where TRepositoryTransaction : class, IRepositoryTransaction<TRepositoryState> {
    ValueTask<Optional<TRepositoryState>> LoadAsync(
        CancellationToken cancellationToken);

    ValueTask SaveAsync(
        RepositorySaveMode saveMode,
        TRepositoryTransaction transaction,
        TRepositoryState oldState,
        TRepositoryState nextState,
        CancellationToken cancellationToken);

    TRepositoryState CreateEmptyState();
}

public interface ITransactionFinalizer<TRepositoryState, TRepositoryTransaction>
    where TRepositoryState : class, IRepositoryState
    where TRepositoryTransaction : class, IRepositoryTransaction<TRepositoryState> {
    ValueTask CommitAsync(RepositorySaveMode saveMode, TRepositoryState state, CancellationToken cancellationToken);
    void Cancel();
}

// TODO:thinkof

public interface IDictionaryRepository {
}

public interface IDictionaryRepository<TKey, TValue> : IDictionaryRepository
    where TKey : notnull {
    ValueTask<IEnumerable<KeyValuePair<TKey, TValue>>> LoadAsync();

    ValueTask SaveAsync(IEnumerable<KeyValuePair<TKey, TValue>> values);

}

public interface IQueueRepository {
}

public interface IQueueRepository<TValue> : IQueueRepository {
    ValueTask<List<TValue>> LoadAsync();
    ValueTask SaveAsync(List<TValue> values);

    ValueTask EnqueuedAsync(TValue value);
    ValueTask DequeuedAsync(TValue value);
}
