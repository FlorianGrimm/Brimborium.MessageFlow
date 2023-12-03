namespace Brimborium.MessageFlow.Repositories;

public abstract class BaseRepository<TRepositoryState, TRepositoryTransaction> 
    : DisposableWithState
    , IRepository<TRepositoryState, TRepositoryTransaction>
    where TRepositoryState : class, IRepositoryState
    where TRepositoryTransaction : class, IRepositoryTransaction<TRepositoryState> {

    protected SemaphoreSlim _Lock;
    protected TRepositoryState _State;
    protected readonly IRepositoryPersitence<TRepositoryState, TRepositoryTransaction> _RepositoryPersitence;
    protected TRepositoryTransaction? _CurrentTransaction;

    protected BaseRepository(
        IRepositoryPersitence<TRepositoryState, TRepositoryTransaction> repositoryPersitence,
        TRepositoryState? repositoryState,
        ILogger logger
        ) : base(logger) {
        this._Lock = new SemaphoreSlim(1, 1);
        this._State = repositoryState ?? repositoryPersitence.CreateEmptyState();
        this._RepositoryPersitence = repositoryPersitence;
    }

    public TRepositoryState State => this._State;

    protected abstract TRepositoryTransaction CreateRepositoryTransaction(ITransactionFinalizer<TRepositoryState, TRepositoryTransaction> transactionFinalizer);

    public async ValueTask<TRepositoryTransaction> CreateTransaction(CancellationToken cancellationToken) {
        try { 
            await this._Lock.WaitAsync(cancellationToken);
        } catch (OperationCanceledException){
            // TODO: log
            throw;
        }
        try {
            var transactionFinalizer = new TransactionFinalizer(this);
            var result = this.CreateRepositoryTransaction(transactionFinalizer);
            transactionFinalizer.SetTransaction(result);
            this._CurrentTransaction = result;
            return result;
        } catch {
            this._CurrentTransaction = default;
            this._Lock.Release();
            throw;
        }
    }

    protected override bool Dispose(bool disposing) {
        if (base.Dispose(disposing)) {
            using (var l = this._Lock) {
                if (disposing) {
                    this._Lock = null!;
                }
            }
            return true;
        } else {
            return false;
        }
    }

    internal async ValueTask CommitAsync(TRepositoryTransaction transaction, TRepositoryState state, CancellationToken cancellationToken) {
        var oldState = this._State;
        this._State = state;
        this._Lock.Release();
        await this.SaveAsync(transaction, oldState, state, cancellationToken);
        //this._Logger.LogInformation("RepositoryState saved");
    }

    internal void Cancel(TRepositoryTransaction transaction) {
        if (ReferenceEquals(this._CurrentTransaction, transaction)) {
            this._Lock.Release();
        }
    }

    public async ValueTask LoadAsync(CancellationToken cancellationToken) {
        await this._Lock.WaitAsync(cancellationToken);
        try {
            this._State = await this._RepositoryPersitence.LoadAsync(cancellationToken);
        } finally {
            this._Lock.Release();
        }
    }
    protected abstract ValueTask SaveAsync(
        TRepositoryTransaction transaction,
        TRepositoryState oldState,
        TRepositoryState nextState,
        CancellationToken cancellationToken);

    internal sealed class TransactionFinalizer(
        BaseRepository<TRepositoryState, TRepositoryTransaction> owner
        ) : ITransactionFinalizer<TRepositoryState, TRepositoryTransaction> {
        private BaseRepository<TRepositoryState, TRepositoryTransaction>? _Owner = owner;
        private TRepositoryTransaction? _Transaction;

        internal void SetTransaction(TRepositoryTransaction transaction) {
            this._Transaction = transaction;
        }

        public async ValueTask CommitAsync(TRepositoryState state, CancellationToken cancellationToken) {
            var owner = this._Owner;
            this._Owner = default;
            var transaction = this._Transaction;
            this._Transaction = default;

            if (owner is not null && transaction is not null) {
                await owner.CommitAsync(transaction, state, cancellationToken);
            }
        }

        public void Cancel() {
            var owner = this._Owner;
            this._Owner = default;
            var transaction = this._Transaction;
            this._Transaction = default;

            if (owner is not null && transaction is not null) {
                owner.Cancel(transaction);
            }
        }
    }
}

public abstract class BaseRepositoryTransaction<TRepositoryState>(ILogger logger)
    : DisposableWithState(logger)
    , IRepositoryTransaction<TRepositoryState>
    where TRepositoryState : class, IRepositoryState {

    public abstract ValueTask CommitAsync(CancellationToken cancellationToken);
    
    public abstract void Cancel();
}

public enum RepositoryChangeMode {
    Add,
    Update,
    Remove
}
public record RepositoryChange<TKey, TValue>(
    RepositoryChangeMode Mode,
    TKey Key,
    TValue Value)
    where TKey : notnull;


public struct ItemRepositoryTransaction<TKey, TValue>(
    ImmutableDictionary<TKey, TValue> state
    ) where TKey : notnull {
    public ImmutableDictionary<TKey, TValue> State = state;
    public ImmutableDictionary<TKey, TValue>.Builder? Builder = default;
    public List<RepositoryChange<TKey, TValue>>? ListChange = default;
}

public static partial class ItemRepositoryTransaction {

    public static ImmutableDictionary<TKey, TValue>.Builder GetBuilder<TKey, TValue>(ref ItemRepositoryTransaction<TKey, TValue> that) where TKey : notnull {
        return that.Builder ??= that.State.ToBuilder();
    }

    public static bool Add<TKey, TValue>(
        ref ItemRepositoryTransaction<TKey, TValue> that,
        TKey key, TValue value
        ) where TKey : notnull {
        var builder = GetBuilder(ref that);
        bool result;
        if (builder.ContainsKey(key)) {
            result = false;
        } else {
            result = true;
            builder.Add(key, value);
        }
        var listChange = that.ListChange ??= new();
        listChange.Add(new RepositoryChange<TKey, TValue>(RepositoryChangeMode.Add, key, value));
        return result;
    }

    public static bool Update<TKey, TValue>(
        ref ItemRepositoryTransaction<TKey, TValue> that,
        TKey key, TValue value
    ) where TKey : notnull {
        var builder = GetBuilder(ref that);
        bool result;
        if (builder.ContainsKey(key)) {
            result = false;
            builder.Remove(key);
            builder.Add(key, value);
        } else {
            result = true;
            builder.Add(key, value);
        }
        var listChange = that.ListChange ??= new();
        listChange.Add(new RepositoryChange<TKey, TValue>(RepositoryChangeMode.Update, key, value));
        return result;
    }

    public static bool Remove<TKey, TValue>(
        ref ItemRepositoryTransaction<TKey, TValue> that,
        TKey key
    ) where TKey : notnull {
        var builder = GetBuilder(ref that);
        bool result;
        if (builder.TryGetValue(key, out var oldValue)) {
            builder.Remove(key);
            var listChange = that.ListChange ??= new();
            listChange.Add(new RepositoryChange<TKey, TValue>(RepositoryChangeMode.Remove, key, oldValue));
            result = true;
        } else {
            result = false;
        }
        return result;
    }

    public static ImmutableDictionary<TKey, TValue> Finalize<TKey, TValue>(
        ref ItemRepositoryTransaction<TKey, TValue> that
    ) where TKey : notnull {
        if (that.Builder is null) { 
            return that.State;
        } else {
            that.State = that.Builder.ToImmutable();
            that.Builder = null;
            return that.State;
        }
    }
}
