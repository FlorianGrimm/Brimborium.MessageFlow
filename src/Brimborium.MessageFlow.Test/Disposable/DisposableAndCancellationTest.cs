namespace Brimborium.MessageFlow.Test.Disposable;

public class DisposableAndCancellationTest {
    class DisposableAndCancellationForTest001 : DisposableAndCancellation {
        public DisposableAndCancellationForTest001(ILogger logger) : base(logger) {
        }

        public async Task<CancellationToken> DoSomething(CancellationToken cancellationToken) {
            var tcs = new TaskCompletionSource<int>();
            var executionCancellationToken = this.GetExecutionCancellationToken(cancellationToken);
            this.Logger.LogInformation($"1:{cancellationToken.IsCancellationRequested}");
            this.Logger.LogInformation($"2:{executionCancellationToken.IsCancellationRequested}");
            await tcs.Task.WaitAsync(executionCancellationToken);
            this.Logger.LogInformation($"3:{cancellationToken.IsCancellationRequested}");
            this.Logger.LogInformation($"4:{executionCancellationToken.IsCancellationRequested}");
            return executionCancellationToken;
        }
        protected override bool Dispose(bool disposing) {
            if (base.Dispose(disposing)) {
                this.Logger.LogInformation("disposing");
                return true;
            } else {
                this.Logger.LogInformation("not disposing");
                return false;
            }
        }
    }

    public DisposableAndCancellationTest() { }

    [Fact]
    public void DisposableAndCancellationTest001() {
        var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var inMemoryLoggerProvider = new Meziantou.Extensions.Logging.InMemory.InMemoryLoggerProvider();
        serviceCollection.AddLogging((loggingBuilder) => {
            loggingBuilder.AddProvider(inMemoryLoggerProvider);
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
        });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Test001");
        using var a = new DisposableAndCancellationForTest001(logger);
        a.Dispose();
        a.Dispose();
    }
}
