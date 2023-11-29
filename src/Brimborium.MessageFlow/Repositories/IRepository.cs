namespace Brimborium.MessageFlow.Repositories;

public interface IRepositoryState {
}

public interface IRepositoryTransaction<TRepositoryState>
    where TRepositoryState : class, IRepositoryState {
}

public interface IRepositoryPersitence<TRepositoryState>
    where TRepositoryState : class, IRepositoryState {
    ValueTask<TRepositoryState> LoadAsync(CancellationToken cancellationToken);
    ValueTask SaveAsync(TRepositoryState state, CancellationToken cancellationToken);
    TRepositoryState CreateEmptyState();
}

public interface ITransactionFinalizer<TRepositoryState, TRepositoryTransaction>
    where TRepositoryState : class, IRepositoryState
    where TRepositoryTransaction : class, IRepositoryTransaction<TRepositoryState> {
    ValueTask CommitAsync(TRepositoryState state);
    void Cancel();
}

// TODO:thinkof

public interface IDictionaryRepository {
}

public interface IDictionaryRepository<TKey, TValue>: IDictionaryRepository 
    where TKey:notnull
    {
    ValueTask<IEnumerable<KeyValuePair<TKey, TValue>>> LoadAsync();

    ValueTask SaveAsync(IEnumerable<KeyValuePair<TKey, TValue>> values);

}

public interface IQueueRepository {
}

public interface IQueueRepository<TValue>: IQueueRepository {
    ValueTask<List<TValue>> LoadAsync();
    ValueTask SaveAsync(List<TValue> values);

    ValueTask EnqueuedAsync(TValue value);
    ValueTask DequeuedAsync(TValue value);
}
