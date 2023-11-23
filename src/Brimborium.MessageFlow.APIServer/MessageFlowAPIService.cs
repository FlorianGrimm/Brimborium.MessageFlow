namespace Brimborium.MessageFlow.APIServer;

public interface IMessageFlowAPIService {
    List<string> GetEngineNames();
    MessageFlowGraph? GetEngineGraph(string name);
    bool Register(string name, IMessageEngine messageEngine);
}

public class MessageFlowAPIService : IMessageFlowAPIService {
    private readonly MessageFlowAPIServiceOption _Options;
    private ImmutableDictionary<string, IMessageEngine> _EngineByName = ImmutableDictionary<string, IMessageEngine>.Empty;

    public MessageFlowAPIService(
        IOptions<MessageFlowAPIServiceOption> options
        ) {
        this._Options = options.Value;
    }

    public bool Register(string name, IMessageEngine messageEngine) {
        lock (this) {
            if (this._EngineByName.ContainsKey(name)) {
                return false;
            } else {
                this._EngineByName = this._EngineByName.Add(name, messageEngine);
                return true;
            }
        }
    }


    public List<string> GetEngineNames() {
        return this._EngineByName.Keys.ToList();
    }

    public MessageFlowGraph? GetEngineGraph(string name) {
        if (!this._EngineByName.TryGetValue(name, out var messageEngine)) {
            return null;
        } else {
            return messageEngine.ToMessageFlowGraph();
        }
    }
}


public class MessageFlowAPIServiceOption {
}
