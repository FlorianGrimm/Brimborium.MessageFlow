#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.

using Brimborium.ReturnValue;

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

public class HackRepositoryPersitence : IRepositoryPersitence<HackRepositoryState, HackRepositoryTransaction> {
    public HackRepositoryState State = HackRepositoryState.Create();
    public HackRepositoryTransaction? Transaction = default;

    public HackRepositoryState CreateEmptyState()
        => HackRepositoryState.Create();

    public ValueTask<Optional<HackRepositoryState>> LoadAsync(CancellationToken cancellationToken) {
        return ValueTask.FromResult<Optional<HackRepositoryState>>(new(this.State));
    }

    public ValueTask SaveAsync(
        RepositorySaveMode saveMode,
        HackRepositoryTransaction transaction,
        HackRepositoryState oldState,
        HackRepositoryState nextState,
        CancellationToken cancellationToken) {
        this.State = nextState;
        this.Transaction = transaction;
        return ValueTask.CompletedTask;
    }
}

public class HackRepositoryTransaction : BaseRepositoryTransaction<HackRepositoryState> {
    private ITransactionFinalizer<HackRepositoryState, HackRepositoryTransaction>? _TransactionFinalizer;
    private HackRepositoryState _State;
    private ItemRepositoryTransaction<int, string> _AnyThing;
    private ItemRepositoryTransaction<int, string> _SomeThing;

    public HackRepositoryTransaction(
        ITransactionFinalizer<HackRepositoryState, HackRepositoryTransaction> transactionFinalizer,
        HackRepositoryState state,
        ILogger logger) : base(logger) {
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

public class HackRepository : BaseRepository<HackRepositoryState, HackRepositoryTransaction> {
    public HackRepository(
        IRepositoryPersitence<HackRepositoryState, HackRepositoryTransaction> repositoryPersitence,
        HackRepositoryState? repositoryState,
        ILogger logger
        ) : base(repositoryPersitence, repositoryState, logger) {
    }

    protected override HackRepositoryTransaction CreateRepositoryTransaction(ITransactionFinalizer<HackRepositoryState, HackRepositoryTransaction> transactionFinalizer) {
        return new HackRepositoryTransaction(transactionFinalizer, this._State, this.Logger);
    }

    protected override async ValueTask SaveAsync(
        RepositorySaveMode saveMode,
        HackRepositoryTransaction transaction,
        HackRepositoryState oldState,
        HackRepositoryState nextState,
        CancellationToken cancellationToken) {
        await this._RepositoryPersitence.SaveAsync(saveMode, transaction, oldState, nextState, cancellationToken);
    }
}

public class RepositoryTest {
    private readonly ITestOutputHelper _TestOutputHelper;

    public RepositoryTest(ITestOutputHelper testOutputHelper) {
        this._TestOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task RepositoryTest01() {
        var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var inMemoryLoggerProvider = new Meziantou.Extensions.Logging.InMemory.InMemoryLoggerProvider();
        var xunitLoggerProvider = new Meziantou.Extensions.Logging.Xunit.XUnitLoggerProvider(this._TestOutputHelper);
        serviceCollection.AddLogging((loggingBuilder) => {
            loggingBuilder.AddProvider(inMemoryLoggerProvider);
            loggingBuilder.AddProvider(xunitLoggerProvider);
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
        });
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var hackRepositoryPersitence = new HackRepositoryPersitence();
        var logger = serviceProvider.GetRequiredService<ILogger<RepositoryTest>>();
        var hackRepository = new HackRepository(hackRepositoryPersitence, HackRepositoryState.Create(), logger);

        var cancellationToken = CancellationToken.None;

        using (var transaction = await hackRepository.CreateTransaction(cancellationToken)) {
            transaction.AnyThingAdd(1, "one");
            transaction.AnyThingAdd(2, "two");
            await transaction.CommitAsync(RepositorySaveMode.Auto,cancellationToken);
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
    public async Task RepositoryTest02_Parrallel() {
        var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var inMemoryLoggerProvider = new Meziantou.Extensions.Logging.InMemory.InMemoryLoggerProvider();
        var xunitLoggerProvider = new Meziantou.Extensions.Logging.Xunit.XUnitLoggerProvider(this._TestOutputHelper);
        serviceCollection.AddLogging((loggingBuilder) => {
            loggingBuilder.AddProvider(inMemoryLoggerProvider);
            loggingBuilder.AddProvider(xunitLoggerProvider);
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
        });
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var hackRepositoryPersitence = new HackRepositoryPersitence();
        var logger = serviceProvider.GetRequiredService<ILogger<RepositoryTest>>();
        var hackRepository = new HackRepository(hackRepositoryPersitence, HackRepositoryState.Create(), logger);

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