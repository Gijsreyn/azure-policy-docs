using System.Text.Encodings.Web;
using System.Text.Json;

namespace AzurePolicyDocs.Model;

/// <summary>
/// Turns JSON values into strings for templates. Uses relaxed escaping so <c>'</c> and <c>+</c> survive,
/// which matters for policy expressions such as <c>[parameters('effect')]</c>.
/// </summary>
internal static class JsonValueFormatter
{
    private static readonly JsonSerializerOptions CompactOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>A single-line form for tables: strings are shown bare, everything else as compact JSON.</summary>
    public static string ToDisplay(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            var text = element.GetString() ?? string.Empty;
            return text.Length == 0 ? "\"\"" : text;
        }

        return ToCompactJson(element);
    }

    public static string ToCompactJson(JsonElement element) =>
        JsonSerializer.Serialize(element, CompactOptions);

    /// <summary>Pretty-printed with two-space indentation and <c>\n</c> line endings on every platform.</summary>
    public static string ToIndentedJson(JsonElement element) =>
        JsonSerializer.Serialize(element, IndentedOptions).ReplaceLineEndings("\n");

    /// <summary>A Markdown code fence longer than any backtick run inside <paramref name="contents"/>.</summary>
    public static string GetCodeFence(string contents)
    {
        var longestRun = 0;
        var currentRun = 0;
        foreach (var character in contents)
        {
            currentRun = character == '`' ? currentRun + 1 : 0;
            longestRun = Math.Max(longestRun, currentRun);
        }

        return new string('`', Math.Max(3, longestRun + 1));
    }
}
