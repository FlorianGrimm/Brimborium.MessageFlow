
namespace Brimborium.MessageFlow;

public interface IMessageSinkExecution {
    NodeIdentifier SinkId { get; }
    Task StartAsync(CancellationToken cancellationToken);
    Task ExecuteAsync(CancellationToken cancellationToken);
}

public class MessageSinkExecutor {
    private readonly Dictionary<NodeIdentifier, IMessageSinkExecution> _DictExecutor = [];
    private List<IMessageSinkExecution> _ListExecutor = [];

    public void Add(IMessageSinkExecution messageSinkExecution) {
        lock (this._DictExecutor) {
            this._DictExecutor[messageSinkExecution.SinkId] = messageSinkExecution;
            this._ListExecutor = this._DictExecutor.Values.ToList();
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        var listToAwait = this._ListExecutor;
        var listTask = new List<Task>();
        foreach (var item in listToAwait) {
            listTask.Add(item.StartAsync(cancellationToken));
        }
        await Task.WhenAll(listTask);
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken) {
        var listToAwait = this._ListExecutor;
        var listTask = new List<Task>();
        foreach (var item in listToAwait) {
            listTask.Add(item.ExecuteAsync(cancellationToken));
        }
        await Task.WhenAll(listTask);
    }

    public async Task StartExecuteAsync(CancellationToken cancellationToken) {
        await this.StartAsync(cancellationToken);
        await this.ExecuteAsync(cancellationToken);
    }

        public async Task<IMessageConnection<T>> ConnectAsync<T>(
        IMessageOutgoingSource<T> source,
        IMessageIncomingSink<T> sink,
        CancellationToken executionCancellationToken)
        where T : RootMessage {
        var messageSinkExecution = await source.ConnectAsync(sink, executionCancellationToken);
        this.Add(messageSinkExecution.MessageSinkExecution);
        return messageSinkExecution.Connection;
    }
}