namespace Brimborium.MessageFlow.Test;

public class MessageFlowTest {
    class DoNothingProcessor(NodeIdentifier nameId, IMessageFlowLogging messageFlowLogging)
        : MessageProcessorTransform<FlowMessage, FlowMessage>(nameId, messageFlowLogging) {

        protected override async ValueTask HandleDataAsync(FlowMessage message, CancellationToken cancellationToken) {
            ObjectDisposedException.ThrowIf(this.GetIsDisposed() || this._OutgoingSource is null, this);

            this.Logger.LogInformation("Handle {NameId} : {message} ", this.NameId, message.ToRootMessageLog());

            await this._OutgoingSource.SendDataAsync(message, cancellationToken);
        }

        protected override async ValueTask HandleMessageAsync(FlowMessage message, CancellationToken cancellationToken) {
            ObjectDisposedException.ThrowIf(this.GetIsDisposed() || this._OutgoingSource is null, this);

            this.Logger.LogInformation("Handle {NameId} : {message} ", this.NameId, message.ToRootMessageLog());

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
        List<FlowMessage> listRootMessages = new();
        TaskCompletionSource<FlowMessage> tcsMessage = new();
        e.GlobalIncomingSinkWrite = (message) => {
            listRootMessages.Add(message);
            //if (message is MessageFlowEnd) {
            //    tcsMessage.TrySetResult(message);
            //}
        };

        var p1 = new DoNothingProcessor("p1", e.MessageFlowLogging);
        var p2 = new DoNothingProcessor("p2", e.MessageFlowLogging);
        var p3 = new DoNothingProcessor("p3", e.MessageFlowLogging);
        e.ConnectMessage(e.GlobalOutgoingSource, p1.IncomingSink);
        e.ConnectMessage(p1.OutgoingSource, p2.IncomingSink);
        e.ConnectMessage(p1.OutgoingSource, p3.IncomingSink);
        e.ConnectMessage(p2.OutgoingSource, e.GlobalIncomingSink);
        e.ConnectMessage(p3.OutgoingSource, e.GlobalIncomingSink);

        await e.StartAsync(cancellationToken);
        var task = e.ExecuteAsync(cancellationToken);
        
        var messageFlowStart = MessageFlowStart.CreateStart(NodeIdentifier.Empty);
        await e.GlobalOutgoingSource.SendMessageAsync(messageFlowStart, cancellationToken);
        
        {
            var message = new FlowMessage(
                MessageIdentifier.CreateMessageIdentifier(),
                NodeIdentifier.Empty,
                DateTimeOffset.UtcNow);
            await e.GlobalOutgoingSource.SendMessageAsync(message, cancellationToken);
        }

        {
            var messageFlowEnd = messageFlowStart.CreateFlowEnd();
            await e.GlobalOutgoingSource.SendMessageAsync(messageFlowEnd, cancellationToken);
        }

        await e.TaskExecute;
        await task;
        // Assert.NotNull(lastMessage);
    }
}
