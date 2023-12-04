#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.

namespace Brimborium.MessageFlow.RepositoryLocalFile.Test;
public sealed record class HackFullRepositoryState
    (
    ImmutableDictionary<int, string> SomeThing,
    ImmutableDictionary<int, string> AnyThing
    )
    : IRepositoryState {
    public static HackFullRepositoryState Create()
        => new HackFullRepositoryState(
            ImmutableDictionary<int, string>.Empty,
            ImmutableDictionary<int, string>.Empty
            );
}

public sealed class HackFullRepositoryPersitence(
    LocalFileRepositoryPersitence localFileRepositoryPersitence
    ) : IRepositoryPersitence<HackFullRepositoryState, HackFullRepositoryTransaction> {
    public HackFullRepositoryState State = HackFullRepositoryState.Create();
    public HackFullRepositoryTransaction? Transaction = default;
    private readonly LocalFileFullRepositoryPersitence<HackFullRepositoryState> _Persitence = localFileRepositoryPersitence.GetFullRepositoryPersitenceForType<HackFullRepositoryState>("Hack");

    public HackFullRepositoryState CreateEmptyState()
        => HackFullRepositoryState.Create();

    public async ValueTask<Optional<HackFullRepositoryState>> LoadAsync(CancellationToken cancellationToken) {
        if (this._Persitence is null) {
            return new Optional<HackFullRepositoryState>(this.State);
        } else {
            var result = await this._Persitence.LoadAsync(cancellationToken);
            if (result.TryGetValue(out var resultState)) {
                return new(resultState);
            } else {
                return new();
            }
        }
    }

    public async ValueTask SaveAsync(HackFullRepositoryTransaction transaction, HackFullRepositoryState oldState, HackFullRepositoryState nextState, CancellationToken cancellationToken) {
        this.State = nextState;
        this.Transaction = transaction;
        if (this._Persitence is null) {
        } else {
            var commitable = await this._Persitence.SaveFullStateAsync(nextState, cancellationToken);
            if (commitable is not null) {
                commitable.Commit();
            }
        }
    }
}

public sealed class HackFullRepositoryTransaction : BaseRepositoryTransaction<HackFullRepositoryState> {
    private ITransactionFinalizer<HackFullRepositoryState, HackFullRepositoryTransaction>? _TransactionFinalizer;
    private HackFullRepositoryState _State;
    private ItemRepositoryTransaction<int, string> _AnyThing;
    private ItemRepositoryTransaction<int, string> _SomeThing;

    public HackFullRepositoryTransaction(
        ITransactionFinalizer<HackFullRepositoryState, HackFullRepositoryTransaction> transactionFinalizer,
        HackFullRepositoryState state,
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

public sealed class HackFullRepository : BaseRepository<HackFullRepositoryState, HackFullRepositoryTransaction> {
    public HackFullRepository(
        IRepositoryPersitence<HackFullRepositoryState, HackFullRepositoryTransaction> repositoryPersitence,
        HackFullRepositoryState? repositoryState,
        ILogger logger
        ) : base(repositoryPersitence, repositoryState, logger) {
    }

    protected override HackFullRepositoryTransaction CreateRepositoryTransaction(ITransactionFinalizer<HackFullRepositoryState, HackFullRepositoryTransaction> transactionFinalizer) {
        return new HackFullRepositoryTransaction(transactionFinalizer, this._State, this.Logger);
    }

    protected override async ValueTask SaveAsync(
        HackFullRepositoryTransaction transaction,
        HackFullRepositoryState oldState,
        HackFullRepositoryState nextState,
        CancellationToken cancellationToken) {
        await this._RepositoryPersitence.SaveAsync(transaction, oldState, nextState, cancellationToken);
    }
}

public class LocalFileFullRepositoryPersitenceTest {
    private readonly ITestOutputHelper _TestOutputHelper;

    public LocalFileFullRepositoryPersitenceTest(ITestOutputHelper testOutputHelper) {
        this._TestOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task LocalFileFullRepositoryPersitenceTest_001() {
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

        var folderPath = TestUtility.GetTestDataPath("Full001");
        var magicValue = Guid.NewGuid().ToString("N");
        {
            var jsonUtilities= serviceProvider.GetRequiredService<JsonUtilities>();
            var localFileRepositoryPersitence = new LocalFileRepositoryPersitence(folderPath, jsonUtilities, logger);
            var hackRepositoryPersitence = new HackFullRepositoryPersitence(localFileRepositoryPersitence);
            var hackRepository = new HackFullRepository(hackRepositoryPersitence, hackRepositoryPersitence.CreateEmptyState(), logger);
            using (var transaction = await hackRepository.CreateTransaction(CancellationToken.None)) {
                _ = transaction.AnyThingUpdate(1, "one");
                _ = transaction.AnyThingUpdate(2, magicValue);
                await transaction.CommitAsync(CancellationToken.None);
            }
        }

        {
            var jsonUtilities = serviceProvider.GetRequiredService<JsonUtilities>();
            var localFileRepositoryPersitence = new LocalFileRepositoryPersitence(folderPath, jsonUtilities, logger);
            var hackRepositoryPersitence = new HackFullRepositoryPersitence(localFileRepositoryPersitence);
            var hackRepository = new HackFullRepository(hackRepositoryPersitence, hackRepositoryPersitence.CreateEmptyState(), logger);
            await hackRepository.LoadAsync(CancellationToken.None);
            Assert.NotNull(hackRepository.State);
            Assert.True(hackRepository.State.AnyThing.Count >= 2);
            Assert.True(hackRepository.State.AnyThing.TryGetValue(2, out var actValue));
            Assert.Equal(magicValue, actValue);
        }
    }

}