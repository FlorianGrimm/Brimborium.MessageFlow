#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.

namespace Brimborium.MessageFlow.RepositoryLocalFile.Test;

public sealed record class HackFullDiffRepositoryState
    (
    ImmutableDictionary<int, string> SomeThing,
    ImmutableDictionary<int, string> AnyThing
    )
    : IRepositoryState {
    public static HackFullDiffRepositoryState Create()
        => new HackFullDiffRepositoryState(
            ImmutableDictionary<int, string>.Empty,
            ImmutableDictionary<int, string>.Empty
            );
}
public sealed record class HackFullDiffRepositoryDiffState(
    List<RepositoryChange<int, string>>? SomeThing,
    List<RepositoryChange<int, string>>? AnyThing
    )
    : IRepositoryStateDiff {
    public static HackFullDiffRepositoryDiffState Create()
        => new HackFullDiffRepositoryDiffState(null, null);
}

public sealed class HackFullDiffRepositoryPersitence(
    LocalFileRepositoryPersitence? localFileRepositoryPersitence,
    ILogger logger
    )
    : IRepositoryPersitence<HackFullDiffRepositoryState, HackFullDiffRepositoryTransaction>
    , IFullDiffRepositoryStateOperation<HackFullDiffRepositoryState, HackFullDiffRepositoryDiffState> {
    private readonly ILogger _Logger = logger;
    public HackFullDiffRepositoryState State = HackFullDiffRepositoryState.Create();
    public HackFullDiffRepositoryTransaction? Transaction = default;
    private int _DiffCount = 0;
    private readonly LocalFileFullDiffRepositoryPersitence<HackFullDiffRepositoryState, HackFullDiffRepositoryDiffState>? _Persitence = localFileRepositoryPersitence?.GetFullDiffRepositoryPersitenceForType<HackFullDiffRepositoryState, HackFullDiffRepositoryDiffState>("Hack");

    public HackFullDiffRepositoryState CreateEmptyState()
        => HackFullDiffRepositoryState.Create();

    public async ValueTask<Optional<HackFullDiffRepositoryState>> LoadAsync(CancellationToken cancellationToken) {
        if (this._Persitence is null) {
            return new Optional<HackFullDiffRepositoryState>(this.State);
        } else {
            var result = await this._Persitence.LoadAsync(this, cancellationToken);
            if (result.TryGetValue(out var resultState)) {
                this._DiffCount = resultState.DiffCount;
                return new(resultState.State);
            } else {
                return new();
            }
        }
    }

    public async ValueTask SaveAsync(HackFullDiffRepositoryTransaction transaction, HackFullDiffRepositoryState oldState, HackFullDiffRepositoryState nextState, CancellationToken cancellationToken) {
        this.State = nextState;
        this.Transaction = transaction;
        if (this._Persitence is null) {
        } else {
            var countTransaction = (transaction.AnyThingListChange?.Count ?? 0)
                + (transaction.SomeThingListChange?.Count ?? 0);
            var countNextState = nextState.AnyThing.Count + nextState.SomeThing.Count;
            if (countNextState > 100 && countTransaction < 10 && this._DiffCount < 100) {
                var diffState = new HackFullDiffRepositoryDiffState(
                    SomeThing: transaction.SomeThingListChange,
                    AnyThing: transaction.AnyThingListChange
                    );
                var commitable = await this._Persitence.SaveDiffStateAsync(diffState, cancellationToken);
                if (commitable is not null) {
                    commitable.Commit();
                    this._DiffCount += countTransaction;
                }
            } else {
                var commitable = await this._Persitence.SaveFullStateAsync(nextState, cancellationToken);
                if (commitable is not null) {
                    commitable.Commit();
                    this._DiffCount = 0;
                }
            }
        }
    }

    public FullDiffState<HackFullDiffRepositoryState, HackFullDiffRepositoryDiffState> SetFullState(HackFullDiffRepositoryState resultState) {
        return new(resultState, 0);
    }

    public int GetFullCount(HackFullDiffRepositoryState state) {
        return state.AnyThing.Count + state.SomeThing.Count;
    }

    public FullDiffState<HackFullDiffRepositoryState, HackFullDiffRepositoryDiffState> AddDiffState(
        FullDiffState<HackFullDiffRepositoryState, HackFullDiffRepositoryDiffState> fullDiffState,
        HackFullDiffRepositoryDiffState diffState) {
        var (state, diffCount) = fullDiffState;
        var nextState = new HackFullDiffRepositoryState(
            SomeThing: FullDiffRepositoryStateOperationUtitity.ApplyChanges<int, string>(state.SomeThing, diffState.SomeThing, ref diffCount),
            AnyThing: FullDiffRepositoryStateOperationUtitity.ApplyChanges<int, string>(state.AnyThing, diffState.AnyThing, ref diffCount)
            );
        return new FullDiffState<HackFullDiffRepositoryState, HackFullDiffRepositoryDiffState>(
            nextState,
            diffCount
            );
    }
}

public sealed class HackFullDiffRepositoryTransaction : BaseRepositoryTransaction<HackFullDiffRepositoryState> {
    private ITransactionFinalizer<HackFullDiffRepositoryState, HackFullDiffRepositoryTransaction>? _TransactionFinalizer;
    private HackFullDiffRepositoryState _State;
    private ItemRepositoryTransaction<int, string> _AnyThing;
    private ItemRepositoryTransaction<int, string> _SomeThing;

    public HackFullDiffRepositoryTransaction(
        ITransactionFinalizer<HackFullDiffRepositoryState, HackFullDiffRepositoryTransaction> transactionFinalizer,
        HackFullDiffRepositoryState state,
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

    public override async ValueTask CommitAsync(CancellationToken cancellationToken) {
        ObjectDisposedException.ThrowIf(this._TransactionFinalizer is null, this);

        var transactionFinalizer = this._TransactionFinalizer;
        this._TransactionFinalizer = null;
        var nextState = this._State = this._State with {
            SomeThing = ItemRepositoryTransaction.Finalize(ref this._SomeThing),
            AnyThing = ItemRepositoryTransaction.Finalize(ref this._AnyThing)
        };
        await transactionFinalizer.CommitAsync(nextState, cancellationToken);
    }

    public override void Cancel() {
        var transactionFinalizer = this._TransactionFinalizer;
        this._TransactionFinalizer = null;
        if (transactionFinalizer is not null) {
            transactionFinalizer.Cancel();
        }
    }
}

public sealed class HackFullDiffRepository : BaseRepository<HackFullDiffRepositoryState, HackFullDiffRepositoryTransaction> {
    public HackFullDiffRepository(
        IRepositoryPersitence<HackFullDiffRepositoryState, HackFullDiffRepositoryTransaction> repositoryPersitence,
        HackFullDiffRepositoryState? repositoryState,
        ILogger logger
        ) : base(repositoryPersitence, repositoryState, logger) {
    }

    protected override HackFullDiffRepositoryTransaction CreateRepositoryTransaction(ITransactionFinalizer<HackFullDiffRepositoryState, HackFullDiffRepositoryTransaction> transactionFinalizer) {
        return new HackFullDiffRepositoryTransaction(transactionFinalizer, this._State, this.Logger);
    }

    protected override async ValueTask SaveAsync(
        HackFullDiffRepositoryTransaction transaction,
        HackFullDiffRepositoryState oldState,
        HackFullDiffRepositoryState nextState,
        CancellationToken cancellationToken) {
        await this._RepositoryPersitence.SaveAsync(
            transaction, 
            oldState, 
            nextState, 
            cancellationToken);
    }
}

public class LocalFileFullDiffRepositoryPersitenceTest {
    private readonly ITestOutputHelper _TestOutputHelper;

    public LocalFileFullDiffRepositoryPersitenceTest(ITestOutputHelper testOutputHelper) {
        this._TestOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task RepositoryFullDiffTest01() {
        var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var inMemoryLoggerProvider = new Meziantou.Extensions.Logging.InMemory.InMemoryLoggerProvider();
        var xunitLoggerProvider = new Meziantou.Extensions.Logging.Xunit.XUnitLoggerProvider(this._TestOutputHelper);
        serviceCollection.AddLogging((loggingBuilder) => {
            loggingBuilder.AddProvider(inMemoryLoggerProvider);
            loggingBuilder.AddProvider(xunitLoggerProvider);
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
        });
        serviceCollection.AddSingleton<JsonUtilities>(new SystemTextJsonUtilities());
        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<LocalFileFullDiffRepositoryPersitenceTest>>();

        var folderPath = TestUtility.GetTestDataPath("FullDiv001");
        var jsonUtilities = serviceProvider.GetRequiredService<JsonUtilities>();
        var localFileRepositoryPersitence = new LocalFileRepositoryPersitence(folderPath, jsonUtilities, logger);
        var hackRepositoryPersitence = new HackFullDiffRepositoryPersitence(localFileRepositoryPersitence, logger);
        var hackRepository = new HackFullDiffRepository(hackRepositoryPersitence, HackFullDiffRepositoryState.Create(), logger);

        var cancellationToken = CancellationToken.None;

        using (var transaction = await hackRepository.CreateTransaction(cancellationToken)) {
            transaction.AnyThingAdd(1, "one");
            transaction.AnyThingAdd(2, "two");
            await transaction.CommitAsync(cancellationToken);
        }
        Assert.Equal(2, hackRepository.State.AnyThing.Count);

        using (var transaction = await hackRepository.CreateTransaction(cancellationToken)) {
            transaction.AnyThingUpdate(1, "onemore");
            transaction.AnyThingAdd(3, "three");
            await transaction.CommitAsync(cancellationToken);
        }
        Assert.Equal(3, hackRepository.State.AnyThing.Count);
        Assert.Equal("onemore", hackRepository.State.AnyThing[1]);

        using (var transaction = await hackRepository.CreateTransaction(cancellationToken)) {
            transaction.AnyThingRemove(1);
            await transaction.CommitAsync(cancellationToken);
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
    public async Task RepositoryFullDiffTest02_Parrallel() {
        var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var inMemoryLoggerProvider = new Meziantou.Extensions.Logging.InMemory.InMemoryLoggerProvider();
        var xunitLoggerProvider = new Meziantou.Extensions.Logging.Xunit.XUnitLoggerProvider(this._TestOutputHelper);
        serviceCollection.AddLogging((loggingBuilder) => {
            loggingBuilder.AddProvider(inMemoryLoggerProvider);
            loggingBuilder.AddProvider(xunitLoggerProvider);
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
        });
        serviceCollection.AddSingleton<JsonUtilities>(new SystemTextJsonUtilities());
        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<FileNameUtilitiesTest>>();

        var folderPath = TestUtility.GetTestDataPath("FullDiv002");
        var jsonUtilities = serviceProvider.GetRequiredService<JsonUtilities>();
        var localFileRepositoryPersitence = new LocalFileRepositoryPersitence(folderPath, jsonUtilities, logger);
        var hackRepositoryPersitence = new HackFullDiffRepositoryPersitence(localFileRepositoryPersitence, logger);
        var hackRepository = new HackFullDiffRepository(hackRepositoryPersitence, HackFullDiffRepositoryState.Create(), logger);

        var cancellationToken = CancellationToken.None;
        TaskCompletionSource tcs1 = new();
        var t1 = Task.Run(async () => {
            await tcs1.Task;
            using (var transaction = await hackRepository.CreateTransaction(cancellationToken)) {
                for (int i = 0; i < 10000; i++) {
                    transaction.AnyThingAdd(i, "one");
                }
                await transaction.CommitAsync(cancellationToken);
            }
        });

        var t2 = Task.Run(async () => {
            await tcs1.Task;
            using (var transaction = await hackRepository.CreateTransaction(cancellationToken)) {
                for (int i = 10000; i < 20000; i++) {
                    transaction.AnyThingAdd(i, "two");
                }
                await transaction.CommitAsync(cancellationToken);
            }
        });

        tcs1.SetResult();
        await t1;
        await t2;
        Assert.Equal(20000, hackRepository.State.AnyThing.Count);
    }
}