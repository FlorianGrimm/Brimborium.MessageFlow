namespace Brimborium.MessageFlow.Test;

public class MessageFlowTest {
    class DoNothingProcessor(NodeIdentifier nameId, ILogger logger)
        : MessageProcessorTransform<RootMessage, RootMessage>(nameId, logger) {
        protected override async ValueTask HandleDataAsync(RootMessage message, CancellationToken cancellationToken) {
            ObjectDisposedException.ThrowIf(this.GetIsDisposed() || this._OutgoingSource is null, this);

            await this._OutgoingSource.SendDataAsync(message, cancellationToken);
        }

        protected override async ValueTask HandleMessageAsync(RootMessage message, CancellationToken cancellationToken) {
            ObjectDisposedException.ThrowIf(this.GetIsDisposed() || this._OutgoingSource is null, this);

            await this._OutgoingSource.SendMessageAsync(message, cancellationToken);
        }
    }

    private readonly ITestOutputHelper _TestOutputHelper;

    public MessageFlowTest(ITestOutputHelper testOutputHelper) {
        this._TestOutputHelper = testOutputHelper;
    }

    [Fact]
    public async void MessageFlowTest001() {
        var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var inMemoryLoggerProvider = new Meziantou.Extensions.Logging.InMemory.InMemoryLoggerProvider();
        var xunitLoggerProvider = new Meziantou.Extensions.Logging.Xunit.XUnitLoggerProvider(this._TestOutputHelper);
        serviceCollection.AddLogging((loggingBuilder) => {
            loggingBuilder.AddProvider(inMemoryLoggerProvider);
            loggingBuilder.AddProvider(xunitLoggerProvider);
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
        });
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(this.MessageFlowTest001));
        logger.LogInformation("Start");

        var e = new MessageEngine(nameof(this.MessageFlowTest001), logger);
        TaskCompletionSource< RootMessage> tcsMessage = new();
        e.GlobalIncomingSinkWrite = (message) => tcsMessage.TrySetResult(message);

        var p1 = new DoNothingProcessor("p1", logger);
        e.ConnectMessage(e.GlobalOutgoingSource, p1.IncomingSink);
        e.ConnectMessage(p1.OutgoingSource, e.GlobalIncomingSink);

        await e.StartAsync(cancellationToken);

        var message = new RootMessage(
            MessageIdentifier.CreateMessageIdentifier(),
            NodeIdentifier.Empty,
            DateTimeOffset.UtcNow);
        await e.GlobalOutgoingSource.SendMessageAsync(
            message,
            cancellationToken);
        var lastMessage = await tcsMessage.Task;
        Assert.NotNull(lastMessage);
        cts.Cancel();
        await e.ExecuteAsync(cancellationToken);
    }
}
