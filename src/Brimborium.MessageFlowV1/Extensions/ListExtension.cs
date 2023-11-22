namespace Brimborium.MessageFlow.Extensions;

public static class ListExtension {
    public static List<T> AddValueIfNotNull<T>(this List<T> that, T? value)
        where T : class {
        if (value is not null) {
            that.Add(value);
        }
        return that;
    }

    public static List<T> AddRangeIfNotNull<T>(this List<T> that, IEnumerable<T?>? values)
        where T : class {
        if (values is not null) {
            foreach (var value in values) {
                if (value is not null) {
                    that.Add(value);
                }
            }
        }
        return that;
    }
}
