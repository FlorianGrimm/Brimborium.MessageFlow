namespace Brimborium.MessageFlow;

public class MessageSinkChannel<T>(
        NodeIdentifier sinkId,
        ChannelOptions? channelOptions,
        IMessageProcessorInternal? processorOwner,
        ILogger logger
    ) : MessageSink<T>(sinkId, logger)
    where T : RootMessage {
    protected Task _ExecuteTask = Task.CompletedTask;
    protected IMessageProcessorInternal? _ProcessorOwner = processorOwner;
    private Channel<RootMessage>? _Channel = CreateChannel(channelOptions);

    protected static Channel<RootMessage> CreateChannel(
        ChannelOptions? channelOptions
        ) {
        if (channelOptions is null) {
            var boundedChannelOptions = new BoundedChannelOptions(10000) {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
            };
            return Channel.CreateBounded<RootMessage>(boundedChannelOptions);
        } else if (channelOptions is BoundedChannelOptions boundedChannelOptions) {
            return Channel.CreateBounded<RootMessage>(boundedChannelOptions);
        } else if (channelOptions is UnboundedChannelOptions unboundedChannelOptions) {
            return Channel.CreateUnbounded<RootMessage>(unboundedChannelOptions);
        } else {
            throw new ArgumentException("unknown type", nameof(channelOptions));
        }
    }

    protected override bool Dispose(bool disposing) {
        if (base.Dispose(disposing)) {
            //if (this._ExecuteTask.IsCompleted) {
            //} else {
            //    try {
            //        this._ExecuteTask.GetAwaiter().GetResult();
            //    } catch (Exception error) {
            //        this.Logger.LogWarning(error, "While Dispose");
            //    }
            //}
            if (disposing) {
                this._ProcessorOwner = null;
                this._Channel = default;
            } else {
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
        ObjectDisposedException.ThrowIf(this.GetIsDisposed() || (this._Channel is null), this);
        var writer = this._Channel.Writer;
        var connection = new MessageConnectionChannel<T>(
            senderId, this, writer, this.Logger);
        lock (this._ListSource) {
            this._ListSource.Add(new(senderId, new(connection)));
        }
        await this.StartAsync(cancellationToken);
        this._ProcessorOwner?.OnConnected(connection);
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
            this._ExecuteTask = this.ExecuteLoopAsync(cancellationToken);
        }
        return Task.CompletedTask;
    }

    protected virtual async Task ExecuteLoopAsync(CancellationToken cancellationToken) {
        try {
            ObjectDisposedException.ThrowIf(this.GetIsDisposed() || (this._Channel is null), this);
            var reader = this._Channel.Reader;

            this.Logger.LogDebug("Execute SinkId: {SinkId}", this.SinkId);
            while (!cancellationToken.IsCancellationRequested) {
                if (this.GetIsDisposed() || (this._Channel is null)) {
                    break;
                } else if (reader.TryRead(out var message)) {
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
            this._ExecuteTask = this.ExecuteLoopAsync(cancellationToken);
        }
        return this._ExecuteTask;
    }

    public override CoordinatorNodeSink GetCoordinatorNodeSink() {
        lock (this._ListSource) {
            List<NodeIdentifier> listSourceId = [];
            foreach (var item in this._ListSource) {
                if (item.Value.TryGetTarget(out var connection)) {
                    listSourceId.Add(connection.SourceId);
                }
            }
            return new CoordinatorNodeSink(this._SinkId, listSourceId);
        }
    }

    public Channel<RootMessage>? GetChannel() => this._Channel;
}
