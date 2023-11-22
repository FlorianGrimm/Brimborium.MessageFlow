using Microsoft.IO;

namespace Brimborium.MessageFlow.Tracing;

public sealed class TraceDataService : ITraceDataService
{
    private readonly TraceDataOptions _Options;
    private readonly RecyclableMemoryStreamManager _RecyclableMemoryStreamManager;
    private readonly string? _FileName;
    private readonly System.Collections.Concurrent.ConcurrentQueue<MemoryStream> _TraceDataQueue;
    private readonly ITraceDataServiceSerializer _TraceDataServiceSerializer;
    private readonly ILogger<TraceDataService> _Logger;

    public TraceDataService(
        IServiceProvider? serviceProvider,
        ITraceDataServiceSerializer? traceDataServiceSerializer,
        IOptions<TraceDataOptions> options,
        ILogger<TraceDataService> logger
        )
    {
        _Options = options.Value;
        _TraceDataQueue = new System.Collections.Concurrent.ConcurrentQueue<MemoryStream>();
        _RecyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        if (traceDataServiceSerializer is null)
        {
            if (serviceProvider is not null)
            {
                traceDataServiceSerializer = serviceProvider.GetService<ITraceDataServiceSerializer>();
            }
            if (traceDataServiceSerializer is null)
            {
                traceDataServiceSerializer = new TraceDataServiceNewtonsoftJson();
            }
        }
        _TraceDataServiceSerializer = traceDataServiceSerializer;
        _Logger = logger;

        if (_Options.Enabled)
        {
            var fileName = _Options.FileName;
            string? fullFileName;
            if (!string.IsNullOrEmpty(fileName))
            {
                fullFileName = Path.GetFullPath(fileName);
            }
            else
            {
                fullFileName = default;
            }
            if (!string.IsNullOrEmpty(fullFileName))
            {
                _FileName = fullFileName;
                logger.LogInformation("TraceDataLog: {fullFileName}", fullFileName);
            }
            else
            {
                _FileName = default;
            }
        }
        else
        {
            _FileName = default;
        }

        var hostApplicationLifetime = serviceProvider?.GetService<IHostApplicationLifetime>();
        hostApplicationLifetime?.ApplicationStopped.Register(() => Dispose());
    }

    public bool IsEnabled => _FileName is not null;

    public void TraceData<T>(string message, T data)
    {
        if (_FileName is not null)
        {
            TraceData(new TraceData<T>(message, data));
        }
    }

    public void TraceData<T>(TraceData<T> traceData)
    {
        if (_FileName is not null)
        {
            var stream = _RecyclableMemoryStreamManager.GetStream();
            _TraceDataServiceSerializer.Serialize(stream, traceData);
            stream.WriteByte(13);
            stream.WriteByte(10);
            _TraceDataQueue.Enqueue(stream);
            Flush();
        }
    }

    private Task _FlushTask = Task.CompletedTask;
    private bool _IsDisposed = false;
    private bool _AppendWrite = false;
    private int _CountLinesInFile = 0;
    private DateTimeOffset _LastFlushTime = DateTimeOffset.MinValue;

    public void Flush()
    {
        if (_FileName is not null)
        {
            if (_FlushTask.IsCompleted)
            {
                lock (this)
                {
                    if (_FlushTask.IsCompleted)
                    {
                        _FlushTask = FlushInternal();
                    }
                }
            }
        }
    }

    public Task FlushAsync()
    {
        lock (this)
        {
            return _FlushTask;
        }
    }

    private async Task FlushInternal()
    {
        FileStream? fileStream = default;
        if (_FileName is not null)
        {
            try
            {
                while (!_IsDisposed)
                {
                    while (!_IsDisposed && _TraceDataQueue.TryDequeue(out var memoryStream))
                    {
                        if (fileStream is null)
                        {
                            if (_CountLinesInFile > 1000)
                            {
                                _AppendWrite = false;
                            }
                            else if (DateTimeOffset.UtcNow.Subtract(_LastFlushTime) > TimeSpan.FromMinutes(15))
                            {
                                _AppendWrite = false;
                            }
                            FileMode fileMode;
                            if (_AppendWrite)
                            {
                                fileMode = FileMode.Append;
                            }
                            else
                            {
                                _AppendWrite = true;
                                fileMode = FileMode.Create;
                                _CountLinesInFile = 0;
                            }
                            fileStream = new FileStream(_FileName, fileMode, FileAccess.Write, FileShare.Read);
                        }
                        memoryStream.Position = 0;
                        await memoryStream.CopyToAsync(fileStream, 4096, CancellationToken.None).ConfigureAwait(false);
                        memoryStream.Dispose();
                        _CountLinesInFile++;
                    }
                    if (fileStream is not null)
                    {
                        await fileStream.FlushAsync();
                        fileStream.Dispose();
                        fileStream = null;
                        _LastFlushTime = DateTimeOffset.UtcNow;
                    }
                    else
                    {
                        lock (this)
                        {
                            _FlushTask = Task.CompletedTask;
                        }
                        return;
                    }
                }
            }
            catch (Exception error)
            {
                _Logger.LogError(error, "While writing TraceData to {FileName}", _FileName);
            }
            finally
            {
                if (fileStream is not null)
                {
                    fileStream.Flush();
                    fileStream.Dispose();
                }
            }
        }
    }

    private void Dispose(bool disposing)
    {
        if (!_IsDisposed)
        {
            _IsDisposed = true;
            if (_FlushTask.IsCompleted)
            {
                // done
            }
            else
            {
                _FlushTask.GetAwaiter().GetResult();
            }
        }
    }

    ~TraceDataService()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public readonly struct TraceData<T>
{
    public string Message { get; }
    public T Data { get; }

    [JsonConstructor]
    public TraceData(string message, T data)
    {
        Message = message;
        Data = data;
    }
}