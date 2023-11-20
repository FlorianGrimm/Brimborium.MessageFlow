namespace Brimborium.MessageFlow.Tracing;


public class TraceDataOptions
{
    public bool Enabled { get; set; }
    public string? FileName { get; set; }
}

public interface ITraceDataService : IDisposable
{
    void TraceData<T>(string message, T data);
    void TraceData<T>(TraceData<T> traceData);

    bool IsEnabled { get; }

    void Flush();
    Task FlushAsync();
}

public interface ITraceDataServiceSerializer
{
    void Serialize<T>(MemoryStream stream, T traceData);
}