#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.

namespace Brimborium.MessageFlow.RepositoryLocalFile.Test;

public class LocalFileRepositoryPersitenceTest {
    private readonly ITestOutputHelper _TestOutputHelper;

    public LocalFileRepositoryPersitenceTest(ITestOutputHelper testOutputHelper) {
        this._TestOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task LocalFileRepositoryPersitence_FullState() {
        var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var inMemoryLoggerProvider = new Meziantou.Extensions.Logging.InMemory.InMemoryLoggerProvider();
        var xunitLoggerProvider = new Meziantou.Extensions.Logging.Xunit.XUnitLoggerProvider(this._TestOutputHelper);
        serviceCollection.AddLogging((loggingBuilder) => {
            loggingBuilder.AddProvider(inMemoryLoggerProvider);
            loggingBuilder.AddProvider(xunitLoggerProvider);
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
        });
        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<FileNameUtilitiesTest>>();

        var folderPath = TestUtility.GetTestDataPath("001");
        var magicValue = Guid.NewGuid().ToString("N");
        {
            var sut = new LocalFileRepositoryPersitence(folderPath, logger);
            var localFileRepositoryPersitence = new LocalFileRepositoryPersitence(folderPath, logger);
            var hackRepositoryPersitence = new HackRepositoryPersitence(localFileRepositoryPersitence);
            var hackRepository = new HackRepository(hackRepositoryPersitence, hackRepositoryPersitence.CreateEmptyState(), logger);
            using (var transaction = await hackRepository.CreateTransaction(CancellationToken.None)) {
                _ = transaction.AnyThingUpdate(1, "one");
                _ = transaction.AnyThingUpdate(2, magicValue);
                await transaction.CommitAsync(CancellationToken.None);
            }
        }

        {
            var sut = new LocalFileRepositoryPersitence(folderPath, logger);
            var localFileRepositoryPersitence = new LocalFileRepositoryPersitence(folderPath, logger);
            var hackRepositoryPersitence = new HackRepositoryPersitence(localFileRepositoryPersitence);
            var hackRepository = new HackRepository(hackRepositoryPersitence, hackRepositoryPersitence.CreateEmptyState(), logger);
            await hackRepository.LoadAsync(CancellationToken.None);
            Assert.NotNull(hackRepository.State);
            Assert.True(hackRepository.State.AnyThing.Count >= 2);
            Assert.True(hackRepository.State.AnyThing.TryGetValue(2, out var actValue));
            Assert.Equal(magicValue, actValue);
        }
    }

}