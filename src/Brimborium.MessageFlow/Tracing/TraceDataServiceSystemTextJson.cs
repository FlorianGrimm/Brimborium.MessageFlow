namespace Brimborium.MessageFlow.Tracing;

public sealed class TraceDataServiceSystemTextJson : ITraceDataServiceSerializer
{
    //private static Encoding UTF8NoBOM => _UTF8NoBOM ??= new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    //private static Encoding? _UTF8NoBOM;

    private readonly JsonSerializerOptions _JsonSerializerOptions;

    public TraceDataServiceSystemTextJson(
        )
    {
        _JsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = default
        };
    }

    public void Serialize<T>(MemoryStream stream, T data)
    {
        JsonSerializer.Serialize(stream, data, _JsonSerializerOptions);
    }
}

/*
jsonString = JsonSerializer.Serialize(
    weatherForecast, typeof(WeatherForecast), SourceGenerationContext.Default);
*/