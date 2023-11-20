using Brimborium.MessageFlow.Disposable;

namespace Brimborium.MessageFlow.Test;

public class MessageSinkChannelTest {
    class MessageProcessorForTest1 : MessageProcessor<MessageData<int>, MessageData<int>> {
        public MessageProcessorForTest1(string name) : base(
            name: name,
            nameIncomingSink: "sink",
            nameOutgoingSource: "source",
            traceDataService: null,
            logger: DummyILogger.Instance) {
        }
        protected override async Task HandleMessageAsync(MessageData<int> message, CancellationToken cancellationToken) {
            await base.HandleMessageAsync(message, cancellationToken);
            var outgoingSource = this.OutgoingSource;
            if (outgoingSource is not null) {
                await outgoingSource.SendDataAsync(message with { Data = message.Data + 1 }, cancellationToken);
            } else {
                throw new Exception();
            }
        }

    }
    class MessageSinkChannelForTest1 : MessageSinkChannel<MessageData<int>> {
        public TaskCompletionSource<MessageData<int>>? MessageData;
        public TaskCompletionSource<RootMessage>? RootMessage;

        public MessageSinkChannelForTest1(
                NodeIdentifier sinkId,
                ChannelOptions? channelOptions,
                ILogger logger
            ) : base(
                sinkId,
                channelOptions,
                logger
            ) {
            this.MessageData = new TaskCompletionSource<MessageData<int>>();
            this.RootMessage = new TaskCompletionSource<RootMessage>();
        }
        public override async ValueTask HandleDataMessageAsync(MessageData<int> message, CancellationToken cancellationToken) {
            await base.HandleDataMessageAsync(message, cancellationToken);
            this.MessageData?.TrySetResult(message);
        }
        public override async ValueTask HandleControlMessageAsync(RootMessage message, CancellationToken cancellationToken) {
            await base.HandleControlMessageAsync(message, cancellationToken);
            this.RootMessage?.TrySetResult( message);
        }
    }
    [Fact]
    public async Task MessageSinkChannelTest1() {
        var cts = new CancellationTokenSource();
        
        var logger = DummyILogger.Instance;
        var s1 = new MessageSourceSingleTarget<MessageData<int>>(NodeIdentifier.Create("s1"), logger);
        var s2 = new MessageProcessorForTest1("s2");
        var s3 = new MessageProcessorForTest1("s3");
        var s4 = new MessageSinkChannelForTest1(NodeIdentifier.Create("s4"), default, logger);
        var mse = new MessageSinkExecutor();
        await mse.ConnectAsync(s1, s2.IncomingSinkD, cts.Token);
        await mse.ConnectAsync(s2.OutgoingSourceD, s3.IncomingSinkD, cts.Token);
        await mse.ConnectAsync(s3.OutgoingSourceD, s4, cts.Token);
        var task= mse.ExecuteAsync(cts.Token);
        await s1.SendDataAsync(new MessageData<int>(MessageIdentifier.CreateMessageIdentifier(), NodeIdentifier.Empty, DateTimeOffset.UtcNow,1),cts.Token);
        Assert.NotNull(s4.MessageData);
        var result =await s4.MessageData.Task;
        Assert.Equal(3, result.Data);
        cts.Cancel();
        await task;
        var coordinatorCollector = new CoordinatorCollector();
        s1.
        coordinatorCollector.CollectCoordinatorNode(s2);
        s1.Dispose();
        s2.Dispose();
        s3.Dispose();
        s4.Dispose();
    }
}
