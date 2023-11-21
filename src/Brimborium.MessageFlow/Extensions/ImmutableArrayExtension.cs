namespace Brimborium.MessageFlow.Extensions;

public static class ImmutableArrayExtension {
    public static ImmutableArray<T> AddValueIfNotNull<T>(this ImmutableArray<T> that, T? value)
        where T : class {
        if (value is not null) {
            that.Add(value);
        }
        return that;
    }

    public static ImmutableArray<T> AddRangeIfNotNull<T>(this ImmutableArray<T> that, IEnumerable<T?>? values)
        where T : class {
        if (values is not null) {
            ImmutableArray<T>.Builder? builder = default;
            foreach (var value in values) {
                if (value is not null) {
                    builder ??= that.ToBuilder();
                    builder.Add(value);
                }
            }
            if (builder is not null) {
                return builder.ToImmutable();
            }
        }
        return that;
    }
}

