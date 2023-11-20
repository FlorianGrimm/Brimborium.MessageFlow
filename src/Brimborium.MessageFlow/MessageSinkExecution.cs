
namespace Brimborium.MessageFlow;

public interface IMessageSinkExecution {
    NodeIdentifier SinkId { get; }
    Task StartAsync(CancellationToken cancellationToken);
    Task ExecuteAsync(CancellationToken cancellationToken);
}

public class MessageSinkExecutor {
    private readonly Dictionary<NodeIdentifier, IMessageSinkExecution> _DictExecutor;

    public MessageSinkExecutor() {
        this._DictExecutor = new Dictionary<NodeIdentifier, IMessageSinkExecution>();
    }

    public void Add(IMessageSinkExecution messageSinkExecution) {
        lock (this._DictExecutor) {
            this._DictExecutor[messageSinkExecution.SinkId] = messageSinkExecution;
        }
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken) {
        List<IMessageSinkExecution> listToAwait;
        lock (this._DictExecutor) {
            listToAwait = this._DictExecutor.Values.ToList();
        }
        var listTask = new List<Task>();
        foreach (var item in listToAwait) {
            listTask.Add(item.ExecuteAsync(cancellationToken));
        }
        await Task.WhenAll(listTask);
    }

    public async Task<IMessageEdgeConnection<T>> ConnectAsync<T>(
        IMessageOutgoingSource<T> source,
        IMessageIncomingSink<T> sink,
        CancellationToken executionCancellationToken)
        where T : RootMessage {
        var messageSinkExecution = await source.ConnectAsync(sink, executionCancellationToken);
        this.Add(messageSinkExecution.MessageSinkExecution);
        return messageSinkExecution.Connection;
    }
}