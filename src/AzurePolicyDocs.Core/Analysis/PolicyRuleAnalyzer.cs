using System.Collections.Immutable;
using System.Text.Json;
using AzurePolicyDocs.Model;
using AzurePolicyDocs.Parsing;

namespace AzurePolicyDocs.Analysis;

/// <summary>
/// Walks a policy rule and collects the resource types it targets through explicit <c>type</c> conditions and
/// the field aliases it references. Both are heuristics for readers: the rule itself stays the source of truth
/// and is rendered in full.
/// </summary>
internal static class PolicyRuleAnalyzer
{
    private static readonly string[] PositiveTypeOperators =
    [
        "equals",
        "in",
        "like",
        "match",
        "matchInsensitively",
        "contains",
    ];

    public static (
        ImmutableArray<string> ResourceTypes,
        ImmutableArray<string> FieldAliases
    ) Analyze(JsonElement? condition, JsonElement? existenceCondition)
    {
        var resourceTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fieldAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (condition is { } ifCondition)
        {
            Walk(ifCondition, resourceTypes, fieldAliases, collectTypes: true);
        }

        if (existenceCondition is { } existence)
        {
            // The existence condition targets the related resource, so its type conditions do not describe the
            // evaluated resource. Its aliases are still worth listing.
            Walk(existence, resourceTypes: null, fieldAliases, collectTypes: false);
        }

        return (
            NameOrdering.SortByName(resourceTypes, type => type),
            NameOrdering.SortByName(fieldAliases, alias => alias)
        );
    }

    private static void Walk(
        JsonElement node,
        HashSet<string>? resourceTypes,
        HashSet<string> fieldAliases,
        bool collectTypes
    )
    {
        switch (node.ValueKind)
        {
            case JsonValueKind.Object:
                if (node.GetStringOrNull("field") is { } field)
                {
                    if (string.Equals(field, "type", StringComparison.OrdinalIgnoreCase))
                    {
                        if (collectTypes && resourceTypes is not null)
                        {
                            CollectTypeValues(node, resourceTypes);
                        }
                    }
                    else if (IsAlias(field))
                    {
                        fieldAliases.Add(field);
                    }
                }

                foreach (var property in node.EnumerateObject())
                {
                    Walk(property.Value, resourceTypes, fieldAliases, collectTypes);
                }

                break;

            case JsonValueKind.Array:
                foreach (var item in node.EnumerateArray())
                {
                    Walk(item, resourceTypes, fieldAliases, collectTypes);
                }

                break;
        }
    }

    private static void CollectTypeValues(JsonElement condition, HashSet<string> resourceTypes)
    {
        foreach (var operatorName in PositiveTypeOperators)
        {
            if (condition.GetPropertyOrNull(operatorName) is not { } value)
            {
                continue;
            }

            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    AddLiteral(value.GetString(), resourceTypes);
                    break;

                case JsonValueKind.Array:
                    foreach (var item in value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            AddLiteral(item.GetString(), resourceTypes);
                        }
                    }

                    break;
            }
        }
    }

    private static void AddLiteral(string? value, HashSet<string> resourceTypes)
    {
        // Expressions such as [parameters('resourceTypes')] are resolved at assignment time; nothing to list.
        if (!string.IsNullOrWhiteSpace(value) && !value.TrimStart().StartsWith('['))
        {
            resourceTypes.Add(value.Trim());
        }
    }

    /// <summary>
    /// Aliases look like <c>Microsoft.Storage/storageAccounts/supportsHttpsTrafficOnly</c>. Plain fields
    /// (<c>type</c>, <c>location</c>, <c>tags['env']</c>) and expressions are not aliases.
    /// </summary>
    private static bool IsAlias(string field)
    {
        var trimmed = field.Trim();
        return trimmed.Contains('/')
            && !trimmed.StartsWith('[')
            && !trimmed.StartsWith("tags", StringComparison.OrdinalIgnoreCase);
    }
}
