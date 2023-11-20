using Newtonsoft.Json;

namespace Brimborium.MessageFlow.Tracing;

public class TraceDataServiceNewtonsoftJson : ITraceDataServiceSerializer
{
    private static Encoding UTF8NoBOM => _UTF8NoBOM ??= new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static Encoding? _UTF8NoBOM;

    private readonly Newtonsoft.Json.JsonSerializer _Serializer;

    public TraceDataServiceNewtonsoftJson()
    {
        _Serializer = new Newtonsoft.Json.JsonSerializer();
    }

    public void Serialize<T>(MemoryStream stream, T traceData)
    {
        using var streamWriter = new StreamWriter(stream, UTF8NoBOM, -1, true);
        using var jsonWriter = new JsonTextWriter(streamWriter);
        _Serializer.Serialize(jsonWriter, traceData);
        jsonWriter.Flush();
    }
}
