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

    public void Add(JsonConverter jsonConverter) {
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

    public override void Serialize<T>(Stream stream, T value) {
        System.Text.Json.JsonSerializer.Serialize<T>(stream, value, this.GetOptions());
    }


    public override T? Deserialize<T>(Stream stream) where T : default {
        var result = System.Text.Json.JsonSerializer.Deserialize<T>(stream, this.GetOptions());
        return result;
    }


    /*
    private static byte[] _CommandAdd = ("add"u8).ToArray();
    private static byte[] _CommandRem = ("rem"u8).ToArray();
    private static byte[] _CommandUpd = ("upd"u8).ToArray();
    */

    // add del upd
    // add 01234567 
    // 0123456789123


    public override async Task SerializeLines<T>(Stream stream, List<T> listValue, CancellationToken cancellationToken) {
        var jsonSerializerOptions = this.GetOptions();

        foreach (var item in listValue) {
            System.Text.Json.JsonSerializer.Serialize<T>(stream, item, jsonSerializerOptions);
            stream.WriteByte(10);
            stream.WriteByte(13);
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
                        idxStart = pos;
                        continue;
                    } else {
                        state = 2;
                        if (idxStart < pos) {
                            currentLineStream.Write(buffer, idxStart, pos - idxStart);
                            idxStart = pos;
                        }
                        // Deserialize
                        {
                            currentLineStream.Position = 0;
                            var item = System.Text.Json.JsonSerializer.Deserialize<T>(currentLineStream, jsonSerializerOptions);
                            if (item is not null) {
                                result.Add(item);
                            }
                            currentLineStream.Dispose();
                        }
                        currentLineStream = Manager.GetStream();
                    }
                } else {
                    state = 1;
                }
            }
        }
        if (currentLineStream.Position > 0) {
            // Deserialize
            currentLineStream.Position = 0;
            var item = System.Text.Json.JsonSerializer.Deserialize<T>(currentLineStream, jsonSerializerOptions);
            if (item is not null) {
                result.Add(item);
            }
            currentLineStream.Dispose();
        }

        return result;
    }
}