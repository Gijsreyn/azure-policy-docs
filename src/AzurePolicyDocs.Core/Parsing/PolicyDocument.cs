using System.Text.Json;

namespace AzurePolicyDocs.Parsing;

/// <summary>
/// The kind of Azure Policy resource a file describes.
/// </summary>
public enum PolicyDocumentKind
{
    /// <summary><c>Microsoft.Authorization/policyDefinitions</c>: a single policy with a <c>policyRule</c>.</summary>
    PolicyDefinition,

    /// <summary><c>Microsoft.Authorization/policySetDefinitions</c>: an initiative that groups policy definitions.</summary>
    PolicySetDefinition,
}

/// <summary>
/// A parsed policy file. <see cref="Properties"/> is the property bag regardless of whether the file was a full
/// resource payload (<c>name</c>, <c>type</c>, <c>properties</c>) or just the bag itself.
/// </summary>
public sealed record PolicyDocument(
    PolicyDocumentKind Kind,
    string? Name,
    JsonElement Root,
    JsonElement Properties,
    string Path
);
