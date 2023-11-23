

using System.Globalization;

namespace Brimborium.MessageFlow;

internal static class NodeIdentifierInternal {

    private static long _Id = 1;
    internal static long GetNextId() {
        return Interlocked.Increment(ref _Id);
    }
}

//[TypeConverter(typeof(NodeIdentifierConverter))]
[JsonConverter(typeof(NodeIdentifierJsonConverter))]
public record class NodeIdentifier(
    long Id,
    string Name,
    NodeIdentifier? Parent
    ) {
    public static NodeIdentifier Unknown => new(-1, "?", default);
    public static NodeIdentifier Empty => new(0, string.Empty, default);

    public static NodeIdentifier Create(string name) {
        return new NodeIdentifier(
            NodeIdentifierInternal.GetNextId(),
            name,
            default);
    }

    public static NodeIdentifier CreateChild(NodeIdentifier parent, string name) {
        return new NodeIdentifier(
            NodeIdentifierInternal.GetNextId(),
            name,
            parent);
    }

    public override string ToString()
        => this.ToString(new StringBuilder()).ToString();

    public StringBuilder ToString(StringBuilder sb) {
        if (this.Parent is not null) {
            this.Parent.ToString(sb);
            sb.Append('/');
        }
        sb.Append(this.Name).Append('#').Append(this.Id);
        return sb;
    }

    public static implicit operator NodeIdentifier(string name)
        => new(NodeIdentifierInternal.GetNextId(), name, default);

    public static NodeIdentifier operator +(NodeIdentifier parent, NodeIdentifier right)
        => new(NodeIdentifierInternal.GetNextId(), right.Name, parent);

    public static NodeIdentifier? Parse(string? text) {
        NodeIdentifier? result = default;
        if (text is null) return result;
        ReadOnlySpan<char> value = text;
        while (value.Length > 0) {
            var posHash = value.IndexOf('#');
            if (posHash >= 0) {
                var name = value[0..posHash].ToString();
                value = value[(posHash+1)..];
                var posSlash = value.IndexOf('/');
                long id;
                if (posSlash < 0) {
                    long.TryParse(value, out id);
                    value = value[0..0];
                } else {
                    long.TryParse(value[0..posSlash], out id);
                    value = value[(posSlash+1)..];
                }
                result = new NodeIdentifier(id, name, result);
            }
        }
        return result;
    }
}
/*
public class NodeIdentifierConverter : TypeConverter {
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) {
        // return base.CanConvertFrom(context, sourceType);
        return typeof(string) == sourceType;
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) {
        return base.ConvertFrom(context, culture, value);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? destinationType) {
        //return base.CanConvertTo(context, destinationType);
        return typeof(string) == destinationType;
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) {
        if (typeof(NodeIdentifier) == destinationType) {
            if (value is null) { return null; }
            if (value is NodeIdentifier nodeIdentifier) { return nodeIdentifier; }
            if (value is string text) { return NodeIdentifier.Parse(text); }
        }
        return null;
    }
}
*/
public class NodeIdentifierJsonConverter : JsonConverter<NodeIdentifier> {
    public override void Write(Utf8JsonWriter writer, NodeIdentifier value, JsonSerializerOptions options) {
        writer.WriteStringValue(value.ToString());
    }

    public override NodeIdentifier? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var text = reader.GetString();
        return NodeIdentifier.Parse(text) ?? NodeIdentifier.Empty;
    }
}