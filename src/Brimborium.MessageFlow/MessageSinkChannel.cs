namespace Brimborium.MessageFlow;

public class MessageSinkChannel<T>
    : MessageSink<T>
    where T : RootMessage {
    private readonly Channel<RootMessage> _Channel;
    //private readonly ChannelOptions _ChannelOptions;

    public MessageSinkChannel(
        NodeIdentifier sinkId,
        ChannelOptions? channelOptions,
        ILogger logger
        ) : base(
            sinkId,
            logger) {
        if (channelOptions is null) {
            var boundedChannelOptions = new BoundedChannelOptions(10000) {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
            };
            //this._ChannelOptions = boundedChannelOptions;
            this._Channel = Channel.CreateBounded<RootMessage>(boundedChannelOptions);
        } else if (channelOptions is BoundedChannelOptions boundedChannelOptions) {
            //this._ChannelOptions = boundedChannelOptions;
            this._Channel = Channel.CreateBounded<RootMessage>(boundedChannelOptions);
        } else if (channelOptions is UnboundedChannelOptions unboundedChannelOptions) {
            //this._ChannelOptions = unboundedChannelOptions;
            this._Channel = Channel.CreateUnbounded<RootMessage>(unboundedChannelOptions);
        } else {
            throw new ArgumentException("unknown type", nameof(channelOptions));
        }
    }

    protected override bool Dispose(bool disposing) {
        if (base.Dispose(disposing)) {
            if (this._ExecuteTask.IsCompleted) {
            } else {
                try {
                    this._ExecuteTask.GetAwaiter().GetResult();
                } catch (Exception error) {
                    this.Logger.LogWarning(error, "While Dispose");
                }
            }
            return true;
        } else {
            return false;
        }
    }

    public override async ValueTask<MessageConnectResult<T>> ConnectAsync(
        NodeIdentifier senderId,
        CancellationToken cancellationToken
        ) {
        var writer = this._Channel.Writer;
        var connection = new MessageConnectionChannel<T>(
            senderId, this, writer, this.Logger);
        lock (this._ListSource) {
            this._ListSource.Add(new(senderId, new(connection)));
        }
        await this.StartAsync(cancellationToken);
        return new MessageConnectResult<T>(connection, this);
    }

    public override bool Disconnect(IMessageConnection<T> messageConnection) {
        lock (this._ListSource) {
            for (var idx = 0; idx < this._ListSource.Count; idx++) {
                var item = this._ListSource[idx];
                if (item.Key.Id == messageConnection.SourceId.Id) {
                    if (item.Value.TryGetTarget(out var target)) {
                        if (ReferenceEquals(target, messageConnection)) {
                            this._ListSource.RemoveAt(idx);
                            messageConnection.Dispose();
                            return true;
                        }
                    } else {
                        this._ListSource.RemoveAt(idx);
                        idx--;
                    }
                    messageConnection.Dispose();
                    return true;
                }
            }
        }
        return false;
    }

    public override Task StartAsync(CancellationToken cancellationToken) {
        if (this._ExecuteTask.IsCompleted) {
            this._ExecuteTask = this.ExecuteAsyncInternal(cancellationToken);
        }
        return Task.CompletedTask;
    }

#if false
    private async Task ExecuteAsyncInternal(CancellationToken cancellationToken) {
        CancellationTokenSource executeTokenSource;
        var singleReader = this._ChannelOptions.SingleReader;
        if (singleReader) {
            lock (this) {
                if (this._ExecuteTokenSource is null) {
                    var disposeToken = this.GetDisposeToken();
                    executeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    this._ExecuteTokenSource = executeTokenSource;
                    disposeToken.Register((that) => {
                        if (this._ExecuteTokenSource is CancellationTokenSource tokenSource) {
                            try {
                                if (tokenSource.IsCancellationRequested) {
                                    //
                                } else {
                                    tokenSource.Cancel();
                                }
                            } catch (ObjectDisposedException) {
                            }
                        } else {
                        }
                    }, this);
                } else {
                    return;
                }
            }
        } else {
            var disposeToken = this.GetDisposeToken();
            executeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            disposeToken.Register((state) => {
                if (state is CancellationTokenSource tokenSource) {
                    try {
                        if (tokenSource.IsCancellationRequested) {
                            //
                        } else {
                            tokenSource.Cancel();
                        }
                    } catch (ObjectDisposedException) {
                    }
                } else {
                }
            }, executeTokenSource);
        }

        try {
            var executeToken = executeTokenSource.Token;
            var reader = this._Channel.Reader;
            this.Logger.LogDebug("Execute SinkId: {SinkId}", this.SinkId);
            while (!executeToken.IsCancellationRequested) {
                if (reader.TryRead(out var message)) {
                    try {
                        if (message is T tMessage) {
                            await this.HandleMessage(tMessage, executeToken);
                        } else {
                            await this.HandleControlMessage(message, executeToken);
                        }
                    } catch (OperationCanceledException) {
                        this.Logger.LogDebug("Execute canceled SinkId: {SinkId}", this.SinkId);
                        return;
                    } catch (Exception error) {
                        this.Logger.LogError(error, "error");
                    }
                } else {
                    try {
                        await reader.WaitToReadAsync(executeToken).ConfigureAwait(false);
                    } catch (OperationCanceledException) {
                        this.Logger.LogDebug("Execute canceled SinkId: {SinkId}", this.SinkId);
                        return;
                    }
                }
            }
            this.Logger.LogDebug("Execute finished SinkId: {SinkId}", this.SinkId);
        } catch (System.Exception error) {
            this.Logger.LogError(error, "Fatal error");
            throw;
        } finally {
            using (var cts = this._ExecuteTokenSource) {
                this._ExecuteTokenSource = null;
            }
        }
    }
#endif

    protected virtual async Task ExecuteAsyncInternal(CancellationToken cancellationToken) {
        try {
            var reader = this._Channel.Reader;
            this.Logger.LogDebug("Execute SinkId: {SinkId}", this.SinkId);
            while (!cancellationToken.IsCancellationRequested) {
                if (reader.TryRead(out var message)) {
                    try {
                        if (message is T tMessage) {
                            await this.HandleDataMessageAsync(tMessage, cancellationToken);
                        } else {
                            await this.HandleControlMessageAsync(message, cancellationToken);
                        }
                    } catch (OperationCanceledException) {
                        this.Logger.LogDebug("Execute canceled SinkId: {SinkId}", this.SinkId);
                        return;
                    } catch (Exception error) {
                        this.Logger.LogError(error, "error");
                    }
                } else {
                    try {
                        await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false);
                    } catch (OperationCanceledException) {
                        this.Logger.LogDebug("Execute canceled SinkId: {SinkId}", this.SinkId);
                        return;
                    }
                }
            }
            this.Logger.LogDebug("Execute finished SinkId: {SinkId}", this.SinkId);
        } catch (System.Exception error) {
            this.Logger.LogError(error, "Fatal error");
            throw;
        }
    }

    public override Task ExecuteAsync(CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested) {
            return this._ExecuteTask;
        }
        if (this._ExecuteTask.IsCompleted) {
            this._ExecuteTask = this.ExecuteAsyncInternal(cancellationToken);
        }
        return this._ExecuteTask;
    }

    public override CoordinatorNodeSink GetCoordinatorNodeSink() {
        List<NodeIdentifier> listSourceId = [];
        lock (this._ListSource) {
            foreach (var item in this._ListSource) {
                if (item.Value.TryGetTarget(out var connection)) {
                    listSourceId.Add(connection.SourceId);
                }
            }
        }
        return new CoordinatorNodeSink(
            this._SinkId,
            listSourceId
        );
    }


    public Channel<RootMessage> GetChannel() => this._Channel;

}
