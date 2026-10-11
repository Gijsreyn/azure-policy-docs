using System.Collections.Immutable;
using System.Text.Json;
using AzurePolicyDocs.Parsing;

namespace AzurePolicyDocs.Model;

/// <summary>
/// Everything a template can render for one policy definition or policy set definition.
/// Definition-only members are empty or null for policy sets and the other way around.
/// </summary>
public sealed record PolicyDocumentationModel(
    PolicyDocumentKind Kind,
    string Name,
    string? DisplayName,
    string? Description,
    string? PolicyType,
    string? Mode,
    string? Version,
    string? Category,
    bool IsPreview,
    bool IsDeprecated,
    string Path,
    ImmutableSortedDictionary<string, string> Custom,
    JsonElement? Metadata,
    ImmutableArray<PolicyParameter> Parameters,
    PolicyEffect? Effect,
    PolicyEffectDetails? EffectDetails,
    ImmutableArray<string> ResourceTypes,
    ImmutableArray<string> FieldAliases,
    string? PolicyRuleJson,
    ImmutableArray<PolicySetMember> PolicyDefinitions,
    ImmutableArray<PolicyDefinitionGroup> Groups
);

/// <summary>A parameter declared by a policy or policy set definition, in declaration order.</summary>
public sealed record PolicyParameter(
    string Name,
    string Type,
    string? DisplayName,
    string? Description,
    bool IsRequired,
    string? DefaultValue,
    string? DefaultValueJson,
    ImmutableArray<string> AllowedValues,
    string? StrongType,
    bool AssignPermissions,
    bool IsDeprecated,
    bool IsEffect
);

/// <summary>How the <c>then.effect</c> of a policy rule is determined.</summary>
public enum PolicyEffectKind
{
    /// <summary>A literal such as <c>Deny</c>.</summary>
    Literal,

    /// <summary>A reference to one parameter, such as <c>[parameters('effect')]</c>.</summary>
    Parameter,

    /// <summary>Any other expression, such as <c>[if(...)]</c>.</summary>
    Expression,
}

/// <summary>The effect of a policy definition and the values it can take.</summary>
public sealed record PolicyEffect(
    string Raw,
    PolicyEffectKind Kind,
    string? ParameterName,
    string? DefaultEffect,
    ImmutableArray<string> AllowedEffects
);

/// <summary>The <c>then.details</c> block used by deployIfNotExists, auditIfNotExists, modify, and append.</summary>
public sealed record PolicyEffectDetails(
    string? ResourceType,
    ImmutableArray<RoleDefinition> RoleDefinitions,
    bool HasExistenceCondition,
    bool HasDeployment,
    ImmutableArray<ModifyOperation> ModifyOperations
);

/// <summary>A role the remediation identity needs. <see cref="Name"/> is known for common built-in roles only.</summary>
public sealed record RoleDefinition(string Id, string Guid, string? Name);

/// <summary>One entry of <c>then.details.operations</c> in a modify policy.</summary>
public sealed record ModifyOperation(string Operation, string? Field);

/// <summary>A policy definition referenced by a policy set definition.</summary>
public sealed record PolicySetMember(
    string ReferenceId,
    string? PolicyDefinitionId,
    string? PolicyDefinitionName,
    string Name,
    bool IsBuiltIn,
    string? DefinitionVersion,
    ImmutableArray<string> GroupNames,
    ImmutableArray<PolicySetMemberParameter> Parameters
);

/// <summary>A parameter value passed to a referenced policy definition.</summary>
public sealed record PolicySetMemberParameter(string Name, string Value);

/// <summary>A <c>policyDefinitionGroups</c> entry, with the number of members that reference it.</summary>
public sealed record PolicyDefinitionGroup(
    string Name,
    string? DisplayName,
    string? Category,
    string? Description,
    string? AdditionalMetadataId,
    int MemberCount
);
