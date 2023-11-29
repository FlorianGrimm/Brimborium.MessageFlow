namespace Brimborium.MessageFlow.Test.Repositories;

public record class HackRepositoryState
    (
    ImmutableDictionary<int, string> SomeThing,
    ImmutableDictionary<int, string> AnyThing
    )
    : IRepositoryState {
    public static HackRepositoryState Create()
        => new HackRepositoryState(
            ImmutableDictionary<int, string>.Empty,
            ImmutableDictionary<int, string>.Empty
            );
}

public class HackRepositoryPersitence : IRepositoryPersitence<HackRepositoryState> {
    public HackRepositoryState CreateEmptyState()
        => HackRepositoryState.Create();

    public ValueTask<HackRepositoryState> LoadAsync(CancellationToken cancellationToken) {
        throw new NotImplementedException();
    }

    public ValueTask SaveAsync(HackRepositoryState state, CancellationToken cancellationToken) {
        throw new NotImplementedException();
    }
}

public class HackRepositoryTransaction : BaseRepositoryTransaction<HackRepositoryState> {
    private ITransactionFinalizer<HackRepositoryState, HackRepositoryTransaction>? _TransactionFinalizer;
    private HackRepositoryState _State;
    private ItemRepositoryTransaction<int, string> _AnyThing;
    private ItemRepositoryTransaction<int, string> _SomeThing;

    public HackRepositoryTransaction(
        ITransactionFinalizer<HackRepositoryState, HackRepositoryTransaction> transactionFinalizer,
        HackRepositoryState state) {
        this._TransactionFinalizer = transactionFinalizer;
        this._State = state;
        this._AnyThing = new ItemRepositoryTransaction<int, string>(state.AnyThing);
        this._SomeThing = new ItemRepositoryTransaction<int, string>(state.SomeThing);
    }

    public bool SomeThingAdd(int key, string value) {
        ObjectDisposedException.ThrowIf(this._TransactionFinalizer is null, this);

        return ItemRepositoryTransaction.Add(ref this._SomeThing, key, value);
    }

    public bool AnyThingAdd(int key, string value) {
        ObjectDisposedException.ThrowIf(this._TransactionFinalizer is null, this);

        return ItemRepositoryTransaction.Add(ref this._AnyThing, key, value);
    }

    public bool SomeThingUpdate(int key, string value) {
        ObjectDisposedException.ThrowIf(this._TransactionFinalizer is null, this);

        return ItemRepositoryTransaction.Update(ref this._SomeThing, key, value);
    }

    public bool AnyThingUpdate(int key, string value) {
        ObjectDisposedException.ThrowIf(this._TransactionFinalizer is null, this);

        return ItemRepositoryTransaction.Update(ref this._AnyThing, key, value);
    }

    public bool SomeThingRemove(int key) {
        ObjectDisposedException.ThrowIf(this._TransactionFinalizer is null, this);

        return ItemRepositoryTransaction.Remove(ref this._SomeThing, key);
    }

    public bool AnyThingRemove(int key) {
        ObjectDisposedException.ThrowIf(this._TransactionFinalizer is null, this);

        return ItemRepositoryTransaction.Remove(ref this._AnyThing, key);
    }

    public async ValueTask CommitAsync() {
        ObjectDisposedException.ThrowIf(this._TransactionFinalizer is null, this);

        var transactionFinalizer = this._TransactionFinalizer;
        this._TransactionFinalizer = null;
        if (transactionFinalizer is not null) {
            var nextState = this._State with {
                SomeThing = ItemRepositoryTransaction.Finalize(ref this._SomeThing),
                AnyThing = ItemRepositoryTransaction.Finalize(ref this._AnyThing)
            };
            await transactionFinalizer.CommitAsync(nextState);
        }
    }

    public void Cancel() {
        var transactionFinalizer = this._TransactionFinalizer;
        this._TransactionFinalizer = null;
        if (transactionFinalizer is not null) {
            transactionFinalizer.Cancel();
        }
    }
}

public class HackRepository : BaseRepository<HackRepositoryState, HackRepositoryTransaction> {
    public HackRepository(
        IRepositoryPersitence<HackRepositoryState> repositoryPersitence,
        HackRepositoryState? repositoryState,
        ILogger logger
        ) : base(repositoryPersitence, repositoryState, logger) {
    }

    protected override HackRepositoryTransaction CreateRepositoryTransaction(ITransactionFinalizer<HackRepositoryState, HackRepositoryTransaction> transactionFinalizer) {
        return new HackRepositoryTransaction(transactionFinalizer, this._State);
    }

    protected override ValueTask SaveAsync(
        HackRepositoryTransaction transaction,
        HackRepositoryState oldState,
        HackRepositoryState nextState) {
        throw new NotImplementedException();
    }
}
