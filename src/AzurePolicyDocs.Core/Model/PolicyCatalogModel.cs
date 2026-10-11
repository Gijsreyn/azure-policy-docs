using System.Collections.Immutable;
using AzurePolicyDocs.Parsing;

namespace AzurePolicyDocs.Model;

/// <summary>
/// An index of many policies, for a repository landing page. Entries are sorted by category, then display name.
/// </summary>
public sealed record PolicyCatalogModel(
    ImmutableArray<PolicyCatalogEntry> PolicyDefinitions,
    ImmutableArray<PolicyCatalogEntry> PolicySetDefinitions,
    ImmutableSortedDictionary<string, string> Custom
)
{
    /// <summary>
    /// Builds a catalog. <paramref name="entries"/> pairs each model with the paths the catalog should link to,
    /// already made relative to the catalog's own location by the caller.
    /// </summary>
    public static PolicyCatalogModel Create(
        IEnumerable<(
            PolicyDocumentationModel Model,
            string Path,
            string? DocumentationPath
        )> entries,
        IReadOnlyDictionary<string, string>? customValues = null
    )
    {
        var all = entries
            .Select(entry => new PolicyCatalogEntry(
                Kind: entry.Model.Kind,
                Name: entry.Model.Name,
                DisplayName: entry.Model.DisplayName,
                Description: entry.Model.Description,
                Category: entry.Model.Category,
                Mode: entry.Model.Mode,
                Version: entry.Model.Version,
                DefaultEffect: entry.Model.Effect?.DefaultEffect,
                AllowedEffects: entry.Model.Effect?.AllowedEffects ?? [],
                PolicyCount: entry.Model.PolicyDefinitions.Length,
                Path: entry.Path,
                DocumentationPath: entry.DocumentationPath
            ))
            .OrderBy(entry => entry.Category ?? string.Empty, NameOrdering.Comparer)
            .ThenBy(entry => entry.DisplayName ?? entry.Name, NameOrdering.Comparer)
            .ThenBy(entry => entry.Path, NameOrdering.Comparer)
            .ToImmutableArray();

        return new PolicyCatalogModel(
            PolicyDefinitions: all.Where(entry => entry.Kind == PolicyDocumentKind.PolicyDefinition)
                .ToImmutableArray(),
            PolicySetDefinitions: all.Where(entry =>
                    entry.Kind == PolicyDocumentKind.PolicySetDefinition
                )
                .ToImmutableArray(),
            Custom: customValues is null
                ? ImmutableSortedDictionary.Create<string, string>(StringComparer.Ordinal)
                : customValues.ToImmutableSortedDictionary(StringComparer.Ordinal)
        );
    }
}

/// <summary>One row of the catalog.</summary>
public sealed record PolicyCatalogEntry(
    PolicyDocumentKind Kind,
    string Name,
    string? DisplayName,
    string? Description,
    string? Category,
    string? Mode,
    string? Version,
    string? DefaultEffect,
    ImmutableArray<string> AllowedEffects,
    int PolicyCount,
    string Path,
    string? DocumentationPath
);
