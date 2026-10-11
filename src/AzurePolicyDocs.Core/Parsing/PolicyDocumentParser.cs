using System.Text.Json;

namespace AzurePolicyDocs.Parsing;

/// <summary>
/// Parses policy definition and policy set definition files. Accepts the full resource payload or only the
/// property bag, and tolerates comments and trailing commas so <c>.jsonc</c> files from Enterprise Policy as Code
/// work without preprocessing.
/// </summary>
public static class PolicyDocumentParser
{
    private const string PolicyDefinitionType = "Microsoft.Authorization/policyDefinitions";
    private const string PolicySetDefinitionType = "Microsoft.Authorization/policySetDefinitions";

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        MaxDepth = 256,
    };

    /// <summary>Reads and parses the file at <paramref name="path"/>.</summary>
    public static PolicyDocument ParseFile(string path)
    {
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception exception)
            when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            throw new PolicyDocumentationException(
                $"Unable to read '{path}': {exception.Message}",
                exception
            );
        }

        return Parse(json, path);
    }

    /// <summary>Parses <paramref name="json"/>. <paramref name="path"/> is used for messages and as the name fallback.</summary>
    public static PolicyDocument Parse(string json, string path)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new PolicyDocumentationException($"'{path}' is empty.");
        }

        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(json, DocumentOptions);
            root = document.RootElement.Clone();
        }
        catch (JsonException exception)
        {
            throw new PolicyDocumentationException(
                $"'{path}' is not valid JSON: {exception.Message}",
                exception
            );
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new PolicyDocumentationException($"'{path}' must contain a JSON object.");
        }

        var properties = root.GetPropertyOrNull("properties")
            is { ValueKind: JsonValueKind.Object } bag
            ? bag
            : root;
        var kind = DetectKind(root, properties, path);
        var name = root.GetTrimmedString("name");

        return new PolicyDocument(kind, name, root, properties, path);
    }

    private static PolicyDocumentKind DetectKind(
        JsonElement root,
        JsonElement properties,
        string path
    )
    {
        if (root.GetStringOrNull("type") is { } type)
        {
            if (string.Equals(type, PolicyDefinitionType, StringComparison.OrdinalIgnoreCase))
            {
                return PolicyDocumentKind.PolicyDefinition;
            }

            if (string.Equals(type, PolicySetDefinitionType, StringComparison.OrdinalIgnoreCase))
            {
                return PolicyDocumentKind.PolicySetDefinition;
            }
        }

        if (properties.GetPropertyOrNull("policyRule") is { ValueKind: JsonValueKind.Object })
        {
            return PolicyDocumentKind.PolicyDefinition;
        }

        if (properties.GetPropertyOrNull("policyDefinitions") is { ValueKind: JsonValueKind.Array })
        {
            return PolicyDocumentKind.PolicySetDefinition;
        }

        throw new PolicyDocumentationException(
            $"'{path}' is not a policy definition or a policy set definition: it has no 'type' of {PolicyDefinitionType} or {PolicySetDefinitionType}, and neither 'policyRule' nor 'policyDefinitions' was found."
        );
    }
}
