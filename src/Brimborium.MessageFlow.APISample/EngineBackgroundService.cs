namespace Brimborium.MessageFlow.APISample;

class EngineBackgroundService : IHostedService {
    private readonly IMessageFlowAPIService _MessageFlowAPIService;
    private readonly IHostApplicationLifetime _HostApplicationLifetime;
    private readonly ILogger<EngineBackgroundService> _Logger;
    private readonly MessageEngine _Engine;

    public EngineBackgroundService(
        IMessageFlowAPIService messageFlowAPIService,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<EngineBackgroundService> logger
        ) {
        this._MessageFlowAPIService = messageFlowAPIService;
        this._HostApplicationLifetime = hostApplicationLifetime;
        this._Logger = logger;
        var engine = CreateEngine(logger);
        this._Engine = engine;
        messageFlowAPIService.Register(engine.NameId.Name, engine);
        CancellationTokenSource? ctsTearDown = default;
        hostApplicationLifetime.ApplicationStopping.Register(async void () => {
            if (this._Engine is not null) {
                ctsTearDown = new();
                await this._Engine.TearDownAsync(ctsTearDown.Token);
                ctsTearDown = default;
            }
        });
        hostApplicationLifetime.ApplicationStopped.Register(() => {
            ctsTearDown?.Cancel();
        });
    }


    private static MessageEngine CreateEngine(ILogger logger) {
        var engine = new MessageEngine("hack", logger);
        var producer = new Producer("Producer", engine.MessageFlowLogging);
        var doOne = new DoOne("DoOne", engine.MessageFlowLogging);
        var doTwo = new DoTwo("DoTwo", engine.MessageFlowLogging);
        engine.ConnectMessage(engine.GlobalOutgoingSource, producer.IncomingSink);
        engine.ConnectData(producer.OutgoingSource, doOne.IncomingSink);
        engine.ConnectMessage(doOne.OutgoingSource, engine.GlobalIncomingSink);
        engine.ConnectData(producer.OutgoingSource, doTwo.IncomingSink);
        engine.ConnectMessage(doTwo.OutgoingSource, engine.GlobalIncomingSink);
        return engine;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        await this._Engine.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken) {
        await this._Engine.SendFlowEnd(null, cancellationToken);
        await this._Engine.ExecuteAsync(cancellationToken);
        await this._Engine.TaskExecute;
    }

    private class Producer : MessageProcessorTransform<FlowMessage, MessageData<int>> {
        private Task _TaskTimer = Task.CompletedTask;
        private CancellationTokenSource? _TimerCTS;

        public Producer(NodeIdentifier nameId, IMessageFlowLogging messageFlowLogging) : base(nameId, messageFlowLogging) {
        }

        protected override ValueTask HandleMessageAsync(FlowMessage message, CancellationToken cancellationToken) {
            if (message is MessageFlowStart) {
                if (this._TaskTimer.IsCompleted) {
                    this._TaskTimer = this.StartTimerAsync(cancellationToken);
                }
            }
            if (message is MessageFlowEnd) {
                this._TimerCTS?.Cancel();
            }
            return ValueTask.CompletedTask;
        }

        private async Task StartTimerAsync(CancellationToken cancellationToken) {
            this._TimerCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var stoppingToken = this._TimerCTS.Token;
            int loop = 1;
            while (!stoppingToken.IsCancellationRequested) {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                var message = MessageData<int>.Create(loop++);
                await this.OutgoingSource.SendDataAsync(message, stoppingToken);
            }
            Interlocked.Exchange(ref this._TimerCTS, null)?.Dispose();
        }

        protected override ValueTask HandleDataAsync(FlowMessage message, CancellationToken cancellationToken)
            => this.HandleMessageAsync(message, cancellationToken);

        protected override bool Dispose(bool disposing) {
            if (base.Dispose(disposing)) {
                var timerCTS = Interlocked.Exchange(ref this._TimerCTS, null);
                if (this._TaskTimer.IsCompleted) {
                    if (timerCTS is not null) {
                        timerCTS.Dispose();
                    }
                } else {
                    if (timerCTS is not null) {
                        timerCTS.Cancel();
                    }
                }
                return true;
            } else {
                return false;
            }
        }
    }

    private class DoOne : MessageProcessorTransform<MessageData<int>, MessageData<int>> {
        public DoOne(NodeIdentifier nameId, IMessageFlowLogging messageFlowLogging) : base(nameId, messageFlowLogging) {
        }

        protected override async ValueTask HandleDataAsync(MessageData<int> message, CancellationToken cancellationToken) {
            var messageNext = MessageData<int>.Create(message.Data + 1);
            await this.OutgoingSource.SendDataAsync(messageNext, cancellationToken);
        }

        protected override async ValueTask HandleMessageAsync(FlowMessage message, CancellationToken cancellationToken) {
            await this.OutgoingSource.SendMessageAsync(message, cancellationToken);
        }
    }

    private class DoTwo : MessageProcessorTransform<MessageData<int>, MessageData<int>> {
        public DoTwo(NodeIdentifier nameId, IMessageFlowLogging messageFlowLogging) : base(nameId, messageFlowLogging) {
        }

        protected override async ValueTask HandleDataAsync(MessageData<int> message, CancellationToken cancellationToken) {
            var messageNext = MessageData<int>.Create(message.Data + 1);
            await this.OutgoingSource.SendDataAsync(messageNext, cancellationToken);
        }

        protected override async ValueTask HandleMessageAsync(FlowMessage message, CancellationToken cancellationToken) {
            await this.OutgoingSource.SendMessageAsync(message, cancellationToken);
        }
    }
}