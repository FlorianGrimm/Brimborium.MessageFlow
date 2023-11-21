namespace Brimborium.MessageFlow.Test;

public class MessageFlowTest {

    class MessageProcessor1ForTest1(
            string name,
            ITraceDataService? traceDataService,
            ILogger logger
        )
        : MessageProcessorTransform<MessageData<int>, MessageData<int>>(
            name,
            default,
            traceDataService,
            logger
        ) {
        protected override async ValueTask HandleDataMessageAsync(MessageData<int> message, CancellationToken cancellationToken) {
            await base.HandleDataMessageAsync(message, cancellationToken);
            var outgoingSource = this.OutgoingSource;
            if (outgoingSource is not null) {
                await outgoingSource.SendDataAsync(message with { Data = message.Data + 1 }, cancellationToken);
            } else {
                throw new Exception();
            }
        }

    }
    class MessageProcessor2ForTest1(
            string name,
            ITraceDataService? traceDataService,
            ILogger logger
        )
        : MessageProcessorTransform<MessageData<int>, MessageData<int>>(
            name,
            default,
            traceDataService,
            logger
        ) {
        public TaskCompletionSource<MessageData<int>>? MessageData = new();
        public TaskCompletionSource<RootMessage>? RootMessage = new();

        protected override async ValueTask HandleDataMessageAsync(MessageData<int> message, CancellationToken cancellationToken) {
            this.MessageData?.TrySetResult(message);
            await base.HandleDataMessageAsync(message, cancellationToken);
        }
        protected override async ValueTask HandleControlMessageAsync(RootMessage message, CancellationToken cancellationToken) {
            this.RootMessage?.TrySetResult(message);
            await base.HandleControlMessageAsync(message, cancellationToken);
        }
    }

    private readonly ITestOutputHelper _TestOutputHelper;

    public MessageFlowTest(ITestOutputHelper testOutputHelper) {
        this._TestOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task MessageFlowTest001() {
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

        var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(this.MessageFlowTest001));
        logger.LogInformation("Start");

        var s1MessageGlobalSource = new MessageGobalSource(NodeIdentifier.Create("s1MessageGlobalSource"), logger);
        //var coordinatorCollector = new CoordinatorCollector();

        //var s1 = new MessageOutgoingSourceSingleTarget<MessageData<int>>(NodeIdentifier.Create("s1"), logger);
        var s2Processor = new MessageProcessor1ForTest1("s2Processor", default, logger);
        var s3Processor = new MessageProcessor1ForTest1("s3Processor", default, logger);
        var s4Processor = new MessageProcessor2ForTest1("s4Processor", default, logger);
        var s5MessageGobalSink = new MessageGobalSink("s5MessageGobalSink", default, default, logger);
        var mse = new MessageSinkExecutor();
        var cancellationToken = cts.Token;
        var messageOutgoingSource = s1MessageGlobalSource.GetOutgoingSourceD<MessageData<int>>(default);
        await mse.ConnectAsync(messageOutgoingSource, s2Processor.IncomingSinkD, cancellationToken);
        await mse.ConnectAsync(s2Processor.OutgoingSourceD, s3Processor.IncomingSinkD, cancellationToken);
        await mse.ConnectAsync(s3Processor.OutgoingSourceD, s4Processor.IncomingSinkD, cancellationToken);
        await mse.ConnectAsync(s4Processor.OutgoingSourceD, s5MessageGobalSink.GetIncomingSinkD<MessageData<int>>(null), cancellationToken);
        var task = mse.StartExecuteAsync(cancellationToken);
        var messageFlowStart = MessageFlowStart.CreateStart(s1MessageGlobalSource.NameId);
        await s1MessageGlobalSource.SendControlAsync(messageFlowStart, cancellationToken);
        try {

            var messageFlowReport = MessageFlowReport.Create(s1MessageGlobalSource.NameId, default);
            /*
            await s1MessageGlobalSource.SendControlAsync(messageFlowReport, cancellationToken);
            var listCoordinatorNode = messageFlowReport.CoordinatorCollector.GetListCoordinatorNode();
            Assert.True(listCoordinatorNode.Count > 0);
            */
            var message1Start = MessageGroupStart.CreateStart(s1MessageGlobalSource.NameId);
            await s1MessageGlobalSource.SendControlAsync(message1Start, cancellationToken);
            try {
                await messageOutgoingSource.SendDataAsync(message1Start.CreateData<int>(1), cancellationToken);
            } finally {
                await messageOutgoingSource.SendControlAsync(message1Start.CreateEnd(), cancellationToken);
            }
        } finally {
            await s1MessageGlobalSource.SendControlAsync(messageFlowStart.CreateEnd(), cancellationToken);
        }
        Assert.NotNull(s4Processor.MessageData);
        var result = await s4Processor.MessageData.Task;
        Assert.Equal(3, result.Data);
        cts.Cancel();
        await task;

        /*
        var coordinatorCollector = new CoordinatorCollector();
        //coordinatorCollector.CollectCoordinatorNode(s1);
        coordinatorCollector.CollectCoordinatorNode(s2);
        coordinatorCollector.CollectCoordinatorNode(s3);
        //coordinatorCollector.CollectCoordinatorNode(s4);
        */
        s1MessageGlobalSource.Dispose();
        s2Processor.Dispose();
        s3Processor.Dispose();
        s4Processor.Dispose();
        logger.LogInformation("Done");
    }
}
