using Brimborium.MessageFlow.Disposable;

namespace Brimborium.MessageFlow.Test;

public class MessageSinkChannelTest {
    class MessageProcessor1ForTest1 : MessageProcessorTransform<MessageData<int>, MessageData<int>> {
        public MessageProcessor1ForTest1(
            string name,
            ITraceDataService? traceDataService,
            ILogger logger) : base(
            name: name,
            traceDataService: traceDataService,
            logger: logger) {
        }
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
    class MessageProcessor2ForTest1 : MessageProcessorTransform<MessageData<int>, MessageData<int>> {
        public TaskCompletionSource<MessageData<int>>? MessageData;
        public TaskCompletionSource<RootMessage>? RootMessage;

        public MessageProcessor2ForTest1(
                string name,
                ITraceDataService? traceDataService,
                ILogger logger
            ) : base(
                name,
                traceDataService,
                logger
            ) {
            this.MessageData = new TaskCompletionSource<MessageData<int>>();
            this.RootMessage = new TaskCompletionSource<RootMessage>();
        }
        protected override async ValueTask HandleDataMessageAsync(MessageData<int> message, CancellationToken cancellationToken) {
            this.MessageData?.TrySetResult(message);
            await base.HandleDataMessageAsync(message, cancellationToken);
        }
        protected override async ValueTask HandleControlMessageAsync(RootMessage message, CancellationToken cancellationToken) {
            this.RootMessage?.TrySetResult(message);
            await base.HandleControlMessageAsync(message, cancellationToken);
        }
    }

    [Fact]
    public async Task MessageSinkChannelTest1() {
        var cts = new CancellationTokenSource();

        var logger = DummyILogger.Instance;
        var s1 = new MessageGobalSource(NodeIdentifier.Create("s1"), logger);
        //var coordinatorCollector = new CoordinatorCollector();

        //var s1 = new MessageOutgoingSourceSingleTarget<MessageData<int>>(NodeIdentifier.Create("s1"), logger);
        var s2 = new MessageProcessor1ForTest1("s2", default, logger);
        var s3 = new MessageProcessor1ForTest1("s3", default, logger);
        var s4 = new MessageProcessor2ForTest1("s4", default, logger);
        var s5 = new MessageGobalSink("s5", "sink", default, logger);
        var mse = new MessageSinkExecutor();
        var cancellationToken = cts.Token;
        var messageOutgoingSource = s1.GetOutgoingSourceD<MessageData<int>>(default);
        await mse.ConnectAsync(messageOutgoingSource, s2.IncomingSinkD, cancellationToken);
        await mse.ConnectAsync(s2.OutgoingSourceD, s3.IncomingSinkD, cancellationToken);
        await mse.ConnectAsync(s3.OutgoingSourceD, s4.IncomingSinkD, cancellationToken);
        await mse.ConnectAsync(s4.OutgoingSourceD, s5.GetIncomingSinkD<MessageData<int>>(null), cancellationToken);
        var task = mse.StartExecuteAsync(cancellationToken);
        var messageFlowStart = MessageFlowStart.CreateStart(s1.NameId);
        await s1.SendControlAsync(messageFlowStart, cancellationToken);
        try {

            var messageFlowReport = MessageFlowReport.Create(s1.NameId, default);
            await s1.SendControlAsync(messageFlowReport, cancellationToken);

            var message1Start = MessageGroupStart.CreateStart(s1.NameId);
            await s1.SendControlAsync(message1Start, cancellationToken);
            try {
                await messageOutgoingSource.SendDataAsync(message1Start.CreateData<int>(1), cancellationToken);
            } finally {
                await messageOutgoingSource.SendControlAsync(message1Start.CreateEnd(), cancellationToken);
            }
        } finally {
            await s1.SendControlAsync(messageFlowStart.CreateEnd(), cancellationToken);
        }
        Assert.NotNull(s4.MessageData);
        var result = await s4.MessageData.Task;
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
        s1.Dispose();
        s2.Dispose();
        s3.Dispose();
        s4.Dispose();
    }
}
