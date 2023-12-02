#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.

namespace Brimborium.MessageFlow.Test;

public class MessageFlowTest {
    class DoOneProcessor(NodeIdentifier nameId, IMessageFlowLogging messageFlowLogging)
        : MessageProcessorTransform<MessageData<int>, MessageData<int>>(nameId, messageFlowLogging) {

        protected override async ValueTask<bool> HandleDataAsync(MessageData<int> message, CancellationToken cancellationToken) {
            this.Logger.LogInformation("Handle Data {NameId} : {message} ", this.NameId, message.ToRootMessageLog());

            if (this._OutgoingSource is not null) {
                await this._OutgoingSource.SendDataAsync(message, cancellationToken);
            }

            return true;
        }

        protected override async ValueTask<bool> HandleMessageAsync(FlowMessage message, CancellationToken cancellationToken) {
            if (await base.HandleMessageAsync(message, cancellationToken)) { return true; }

            this.Logger.LogInformation("Handle Message {NameId} : {message} ", this.NameId, message.ToRootMessageLog());

            if (this._OutgoingSource is not null) {
                await this._OutgoingSource.SendMessageAsync(message, cancellationToken);
            }

            return true;
        }
    }
    // 
    class Container<T>(T value) {
        public T Value = value;
    }
    //
    class DoTwoProcessor(NodeIdentifier nameId, IMessageFlowLogging messageFlowLogging)
        : MessageProcessorTransformGrouping<MessageData<int>, Container<int>, MessageData<int>>(nameId, messageFlowLogging) {

        protected override Container<int> GroupStartGetInitialValue(MessageGroupStart? messageGroupStart) {
            return new(0);
        }

        protected override void GroupDataAddValue(MessageData<int> message, Container<int> groupValue) {
            groupValue.Value += message.Data;
        }

        protected override async ValueTask GroupEndHandleValueAsync(MessageGroupEnd messageGroupEnd, Container<int> groupValue, CancellationToken cancellationToken) {
            if (this._OutgoingSource is not null) { 
                await this._OutgoingSource.SendDataAsync(MessageData<int>.Create(groupValue.Value), cancellationToken);
            }
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
        List<FlowMessage> listGlobalSinkMessages = new();
        TaskCompletionSource<FlowMessage> tcsMessage = new();
        e.GlobalIncomingSinkWrite = (message) => {
            listGlobalSinkMessages.Add(message);
        };

        var p1 = new DoOneProcessor("p1", e.MessageFlowLogging);
        var p11 = new DoTwoProcessor("p11", e.MessageFlowLogging);
        var p12 = new DoOneProcessor("p12", e.MessageFlowLogging);
        var p2 = new DoOneProcessor("p2", e.MessageFlowLogging);
        e.ConnectMessage(e.GlobalOutgoingSource, p1.IncomingSink);
        e.ConnectMessage(p1.OutgoingSource, e.GlobalIncomingSink);
        e.ConnectMessage(p1.OutgoingSource, p12.IncomingSink);
        e.ConnectMessage(p12.OutgoingSource, p2.IncomingSink);
        e.ConnectMessage(p1.OutgoingSource, p11.IncomingSink);
        e.ConnectMessage(p11.OutgoingSource, e.GlobalIncomingSink);

        await e.BootAsync(cancellationToken);
        await e.StartAsync(cancellationToken);
        var taskExecute = e.ExecuteAsync(cancellationToken);

        MessageGroupStart messageGroupStart = MessageGroupStart.CreateStart(NodeIdentifier.Empty);
        await e.GlobalOutgoingSource.SendMessageAsync(messageGroupStart, cancellationToken);
        try {
            for (int iLoop = 0; iLoop < 100; iLoop++) {
                var messageGroupData = messageGroupStart.CreateData<int>(iLoop);
                await e.GlobalOutgoingSource.SendMessageAsync(messageGroupData, cancellationToken);
            }
            var messageGroupEnd = messageGroupStart.CreateEnd();
            await e.GlobalOutgoingSource.SendMessageAsync(messageGroupEnd, cancellationToken);
        } catch (Exception error) {
            var messageGroupEnd = messageGroupStart.CreateEnd(error);
            await e.GlobalOutgoingSource.SendMessageAsync(messageGroupEnd, cancellationToken);
        }
        await e.WaitUntilEmptyAsync(cancellationToken);
        await e.ShutdownAsync(cancellationToken);

        await e.GetTaskExecute(CancellationToken.None);

        await taskExecute;
        Assert.Equal(1+100+1+1, listGlobalSinkMessages.Count);

        // Assert.NotNull(lastMessage);
    }
}
