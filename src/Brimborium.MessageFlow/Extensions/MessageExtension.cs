namespace Brimborium.MessageFlow.Extensions;

public static class MessageExtension {
    public static List<NodeIdentifier> ToListNodeIdentifier(this IEnumerable<IWithName>? listThat) {
        List<NodeIdentifier> result = [];
        if (listThat is not null) {
            foreach (var that in listThat) {
                result.Add(that.NameId);
            }
        }
        return result;
    }
}
