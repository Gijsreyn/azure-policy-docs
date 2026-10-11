using System.Text.Json;

namespace AzurePolicyDocs.Parsing;

/// <summary>
/// Case-insensitive, null-tolerant accessors. Azure Policy JSON is case-insensitive in practice:
/// exports, the portal, and hand-written files mix <c>displayName</c> and <c>DisplayName</c>.
/// </summary>
internal static class JsonElementExtensions
{
    public static bool TryGetPropertyIgnoreCase(
        this JsonElement element,
        string name,
        out JsonElement value
    )
    {
        value = default;
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (element.TryGetProperty(name, out value))
        {
            return true;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the property, or null when it is absent or JSON null.</summary>
    public static JsonElement? GetPropertyOrNull(this JsonElement element, string name) =>
        element.TryGetPropertyIgnoreCase(name, out var value)
        && value.ValueKind != JsonValueKind.Null
            ? value
            : null;

    public static string? GetStringOrNull(this JsonElement element, string name) =>
        element.GetPropertyOrNull(name) is { ValueKind: JsonValueKind.String } value
            ? value.GetString()
            : null;

    /// <summary>Reads a boolean that may be written as <c>true</c> or as the string <c>"true"</c>, which built-in metadata does.</summary>
    public static bool GetBooleanOrDefault(this JsonElement element, string name)
    {
        var value = element.GetPropertyOrNull(name);
        return value switch
        {
            { ValueKind: JsonValueKind.True } => true,
            { ValueKind: JsonValueKind.String } text => string.Equals(
                text.GetString(),
                "true",
                StringComparison.OrdinalIgnoreCase
            ),
            _ => false,
        };
    }

    public static IEnumerable<string> GetStringArray(this JsonElement element, string name)
    {
        if (element.GetPropertyOrNull(name) is not { ValueKind: JsonValueKind.Array } array)
        {
            yield break;
        }

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && item.GetString() is { } text)
            {
                yield return text;
            }
        }
    }

    public static string? GetTrimmedString(this JsonElement element, string name)
    {
        var value = element.GetStringOrNull(name)?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
