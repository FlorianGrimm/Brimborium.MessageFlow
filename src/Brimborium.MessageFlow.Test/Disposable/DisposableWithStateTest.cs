namespace Brimborium.MessageFlow.Test.Disposable;

public class DisposableWithStateTest {
    class DisposableWithStateForTest001 : DisposableWithState {
        public DisposableWithStateForTest001(ILogger logger) : base(logger) {
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

    public DisposableWithStateTest() { }

    [Fact]
    public void DisposableWithStateTest001() {
        var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var inMemoryLoggerProvider = new Meziantou.Extensions.Logging.InMemory.InMemoryLoggerProvider();
        serviceCollection.AddLogging((loggingBuilder) => {
            loggingBuilder.AddProvider(inMemoryLoggerProvider);
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
        });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Test001");
        using var a = new DisposableWithStateForTest001(logger);
        a.Dispose();
        a.Dispose();
    }
}
