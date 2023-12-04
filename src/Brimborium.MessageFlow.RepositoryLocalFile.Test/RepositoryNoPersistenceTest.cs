#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.
namespace Brimborium.MessageFlow.RepositoryLocalFile.Test;

public sealed record class HackNoPersistenceRepositoryState
    (
    ImmutableDictionary<int, string> SomeThing,
    ImmutableDictionary<int, string> AnyThing
    )
    : IRepositoryState {
    public static HackNoPersistenceRepositoryState Create()
        => new HackNoPersistenceRepositoryState(
            ImmutableDictionary<int, string>.Empty,
            ImmutableDictionary<int, string>.Empty
            );
}

public sealed class HackNoPersistenceRepositoryPersitence(
    ) : IRepositoryPersitence<HackNoPersistenceRepositoryState, HackNoPersistenceRepositoryTransaction> {
    public HackNoPersistenceRepositoryState State = HackNoPersistenceRepositoryState.Create();
    public HackNoPersistenceRepositoryTransaction? Transaction = default;

    public HackNoPersistenceRepositoryState CreateEmptyState()
        => HackNoPersistenceRepositoryState.Create();

    public async ValueTask<Optional<HackNoPersistenceRepositoryState>> LoadAsync(CancellationToken cancellationToken) {
        await ValueTask.CompletedTask;
        return new Optional<HackNoPersistenceRepositoryState>(this.State);
    }

    public ValueTask SaveAsync(
        RepositorySaveMode saveMode,
        HackNoPersistenceRepositoryTransaction transaction,
        HackNoPersistenceRepositoryState oldState,
        HackNoPersistenceRepositoryState nextState,
        CancellationToken cancellationToken) {
        this.State = nextState;
        this.Transaction = transaction;
        return ValueTask.CompletedTask;
    }
}

public sealed class HackNoPersistenceRepositoryTransaction : BaseRepositoryTransaction<HackNoPersistenceRepositoryState> {
    private ITransactionFinalizer<HackNoPersistenceRepositoryState, HackNoPersistenceRepositoryTransaction>? _TransactionFinalizer;
    private HackNoPersistenceRepositoryState _State;
    private ItemRepositoryTransaction<int, string> _AnyThing;
    private ItemRepositoryTransaction<int, string> _SomeThing;

    public HackNoPersistenceRepositoryTransaction(
        ITransactionFinalizer<HackNoPersistenceRepositoryState, HackNoPersistenceRepositoryTransaction> transactionFinalizer,
        HackNoPersistenceRepositoryState state,
        ILogger logger) : base(logger) {
        this._TransactionFinalizer = transactionFinalizer;
        this._State = state;
        this._AnyThing = new ItemRepositoryTransaction<int, string>(state.AnyThing);
        this._SomeThing = new ItemRepositoryTransaction<int, string>(state.SomeThing);
    }
    public List<RepositoryChange<int, string>>? AnyThingListChange => this._AnyThing.ListChange;
    public List<RepositoryChange<int, string>>? SomeThingListChange => this._SomeThing.ListChange;


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

    public override async ValueTask CommitAsync(RepositorySaveMode saveMode, CancellationToken cancellationToken) {
        ObjectDisposedException.ThrowIf(this._TransactionFinalizer is null, this);

        var transactionFinalizer = this._TransactionFinalizer;
        this._TransactionFinalizer = null;
        var nextState = this._State = this._State with {
            SomeThing = ItemRepositoryTransaction.Finalize(ref this._SomeThing),
            AnyThing = ItemRepositoryTransaction.Finalize(ref this._AnyThing)
        };
        await transactionFinalizer.CommitAsync(saveMode, nextState, cancellationToken);
    }

    public override void Cancel() {
        var transactionFinalizer = this._TransactionFinalizer;
        this._TransactionFinalizer = null;
        if (transactionFinalizer is not null) {
            transactionFinalizer.Cancel();
        }
    }
}

public sealed class HackNoPersistenceRepository : BaseRepository<HackNoPersistenceRepositoryState, HackNoPersistenceRepositoryTransaction> {
    public HackNoPersistenceRepository(
        IRepositoryPersitence<HackNoPersistenceRepositoryState, HackNoPersistenceRepositoryTransaction> repositoryPersitence,
        HackNoPersistenceRepositoryState? repositoryState,
        ILogger logger
        ) : base(repositoryPersitence, repositoryState, logger) {
    }

    protected override HackNoPersistenceRepositoryTransaction CreateRepositoryTransaction(ITransactionFinalizer<HackNoPersistenceRepositoryState, HackNoPersistenceRepositoryTransaction> transactionFinalizer) {
        return new HackNoPersistenceRepositoryTransaction(transactionFinalizer, this._State, this.Logger);
    }

    protected override async ValueTask SaveAsync(
        RepositorySaveMode saveMode,
        HackNoPersistenceRepositoryTransaction transaction,
        HackNoPersistenceRepositoryState oldState,
        HackNoPersistenceRepositoryState nextState,
        CancellationToken cancellationToken) {
        await this._RepositoryPersitence.SaveAsync(saveMode, transaction, oldState, nextState, cancellationToken);
    }
}

public class RepositoryNoPersistenceTest {
    private readonly ITestOutputHelper _TestOutputHelper;

    public RepositoryNoPersistenceTest(ITestOutputHelper testOutputHelper) {
        this._TestOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task RepositoryFullTest01() {
        var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var inMemoryLoggerProvider = new Meziantou.Extensions.Logging.InMemory.InMemoryLoggerProvider();
        var xunitLoggerProvider = new Meziantou.Extensions.Logging.Xunit.XUnitLoggerProvider(this._TestOutputHelper);
        serviceCollection.AddLogging((loggingBuilder) => {
            loggingBuilder.AddProvider(inMemoryLoggerProvider);
            loggingBuilder.AddProvider(xunitLoggerProvider);
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
        });
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var hackRepositoryPersitence = new HackNoPersistenceRepositoryPersitence();
        var logger = serviceProvider.GetRequiredService<ILogger<RepositoryNoPersistenceTest>>();
        var hackRepository = new HackNoPersistenceRepository(hackRepositoryPersitence, HackNoPersistenceRepositoryState.Create(), logger);

        var cancellationToken = CancellationToken.None;

        using (var transaction = await hackRepository.CreateTransaction(cancellationToken)) {
            transaction.AnyThingAdd(1, "one");
            transaction.AnyThingAdd(2, "two");
            await transaction.CommitAsync(RepositorySaveMode.Auto, cancellationToken);
        }
        Assert.Equal(2, hackRepository.State.AnyThing.Count);

        using (var transaction = await hackRepository.CreateTransaction(cancellationToken)) {
            transaction.AnyThingUpdate(1, "onemore");
            transaction.AnyThingAdd(3, "three");
            await transaction.CommitAsync(RepositorySaveMode.Auto, cancellationToken);
        }
        Assert.Equal(3, hackRepository.State.AnyThing.Count);
        Assert.Equal("onemore", hackRepository.State.AnyThing[1]);

        using (var transaction = await hackRepository.CreateTransaction(cancellationToken)) {
            transaction.AnyThingRemove(1);
            await transaction.CommitAsync(RepositorySaveMode.Auto, cancellationToken);
        }
        Assert.Equal(2, hackRepository.State.AnyThing.Count);
        Assert.Equal("two", hackRepository.State.AnyThing[2]);
        Assert.Equal("three", hackRepository.State.AnyThing[3]);

        using (var transaction = await hackRepository.CreateTransaction(cancellationToken)) {
            transaction.AnyThingRemove(2);
            transaction.Cancel();
        }
        Assert.Equal(2, hackRepository.State.AnyThing.Count);

        using (var transaction = await hackRepository.CreateTransaction(cancellationToken)) {
            transaction.AnyThingRemove(2);
        }
        Assert.Equal(2, hackRepository.State.AnyThing.Count);
    }


    [Fact]
    public async Task RepositoryFullTest02_Parrallel() {
        var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var inMemoryLoggerProvider = new Meziantou.Extensions.Logging.InMemory.InMemoryLoggerProvider();
        var xunitLoggerProvider = new Meziantou.Extensions.Logging.Xunit.XUnitLoggerProvider(this._TestOutputHelper);
        serviceCollection.AddLogging((loggingBuilder) => {
            loggingBuilder.AddProvider(inMemoryLoggerProvider);
            loggingBuilder.AddProvider(xunitLoggerProvider);
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
        });
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var hackRepositoryPersitence = new HackNoPersistenceRepositoryPersitence();
        var logger = serviceProvider.GetRequiredService<ILogger<RepositoryNoPersistenceTest>>();
        var hackRepository = new HackNoPersistenceRepository(hackRepositoryPersitence, HackNoPersistenceRepositoryState.Create(), logger);

        var cancellationToken = CancellationToken.None;
        TaskCompletionSource tcs1 = new();
        var t1 = Task.Run(async () => {
            await tcs1.Task;
            using (var transaction = await hackRepository.CreateTransaction(cancellationToken)) {
                for (int i = 0; i < 10000; i++) {
                    transaction.AnyThingAdd(i, "one");
                }
                await transaction.CommitAsync(RepositorySaveMode.Auto, cancellationToken);
            }
        });

        var t2 = Task.Run(async () => {
            await tcs1.Task;
            using (var transaction = await hackRepository.CreateTransaction(cancellationToken)) {
                for (int i = 10000; i < 20000; i++) {
                    transaction.AnyThingAdd(i, "two");
                }
                await transaction.CommitAsync(RepositorySaveMode.Auto, cancellationToken);
            }
        });

        tcs1.SetResult();
        await t1;
        await t2;
        Assert.Equal(20000, hackRepository.State.AnyThing.Count);
    }
}