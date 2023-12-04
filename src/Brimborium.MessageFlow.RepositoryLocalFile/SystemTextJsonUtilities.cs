
namespace Brimborium.MessageFlow.RepositoryLocalFile;

public class SystemTextJsonUtilities : JsonUtilities {
    private System.Text.Json.JsonSerializerOptions _Options;
    private System.Text.Json.JsonSerializerOptions _UsedOptions;
    private bool _IsChanged = true;

    public SystemTextJsonUtilities() {
        this._UsedOptions = System.Text.Json.JsonSerializerOptions.Default;
        this._Options = new System.Text.Json.JsonSerializerOptions(this._UsedOptions);
        this._Options.AllowTrailingCommas = true;
        this._Options.DictionaryKeyPolicy = null;
        this._Options.WriteIndented = false;
    }

    public void AddJsonConverter(JsonConverter jsonConverter) {
        this._Options.Converters.Add(jsonConverter);
        this._IsChanged = true;
    }

    public System.Text.Json.JsonSerializerOptions GetOptions() {
        if (this._IsChanged) {
            var nextOptions = new JsonSerializerOptions(this._Options);
            nextOptions.MakeReadOnly();
            this._UsedOptions = nextOptions;
            this._IsChanged = false;
            return nextOptions;
        } else {
            return this._UsedOptions;
        }
    }

    public override async Task SerializeAsync<T>(Stream stream, T value, CancellationToken cancellationToken) {
        await System.Text.Json.JsonSerializer.SerializeAsync<T>(
            stream,
            value,
            this.GetOptions(),
            cancellationToken);
    }

    public override async Task<Optional<T>> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken) {
        var result = await System.Text.Json.JsonSerializer.DeserializeAsync<T>(
            stream,
            this.GetOptions(),
            cancellationToken);
        if (result is null) {
            return new Optional<T>();
        } else {
            return result.AsOptional();
        }
    }

    public override async Task SerializeLinesAsync<T>(Stream stream, List<T> listValue, CancellationToken cancellationToken) {
        var jsonSerializerOptions = this.GetOptions();

        foreach (var item in listValue) {
            await System.Text.Json.JsonSerializer.SerializeAsync<T>(stream, item, jsonSerializerOptions);
            stream.WriteByte(13);
            stream.WriteByte(10);
        }
        await stream.FlushAsync();
    }

    public override async Task<List<T>> DeserializeLines<T>(Stream stream, CancellationToken cancellationToken) {
        var jsonSerializerOptions = this.GetOptions();

        List<T> result = new List<T>();
        int state = 0;
        var currentLineStream = Manager.GetStream();
        byte[] buffer = new byte[4096];
        while (true) {
            var bytesRead = await stream.ReadAsync(buffer, 0, 4096, cancellationToken);
            if (bytesRead == 0) {
                break;
            }

            int idxStart = 0;
            for (int pos = idxStart; pos < bytesRead; pos++) {
                byte b = buffer[pos];
                if (b == 10 || b == 13) {
                    if (state == 2) {
                        //skip the second
                        idxStart = pos;
                        continue;
                    } else {
                        state = 2;
                        if (idxStart < pos) {
                            currentLineStream.Write(buffer, idxStart, pos - idxStart);
                            idxStart = pos;
                        }
                        // Deserialize
                        DeserializeItem(currentLineStream, result, jsonSerializerOptions);
                        currentLineStream = Manager.GetStream();
                    }
                } else {
                    state = 1;
                }
            }
        }
        if (currentLineStream.Position > 0) {
            DeserializeItem(currentLineStream, result, jsonSerializerOptions);
        }

        return result;
    }

    private void DeserializeItem<T>(MemoryStream currentLineStream, List<T> result, JsonSerializerOptions jsonSerializerOptions) {
        currentLineStream.Position = 0;
        var item = System.Text.Json.JsonSerializer.Deserialize<T>(currentLineStream, jsonSerializerOptions);
        if (item is not null) {
            result.Add(item);
        }
        currentLineStream.Dispose();
    }
}