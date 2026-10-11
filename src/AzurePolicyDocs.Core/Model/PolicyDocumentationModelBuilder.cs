using System.Collections.Immutable;
using System.Text.Json;
using AzurePolicyDocs.Analysis;
using AzurePolicyDocs.Parsing;

namespace AzurePolicyDocs.Model;

/// <summary>
/// Turns a parsed <see cref="PolicyDocument"/> into the <see cref="PolicyDocumentationModel"/> that templates render.
/// </summary>
public static class PolicyDocumentationModelBuilder
{
    private const string BuiltInPolicyDefinitionPrefix =
        "/providers/Microsoft.Authorization/policyDefinitions/";

    public static PolicyDocumentationModel Build(
        PolicyDocument document,
        IReadOnlyDictionary<string, string>? customValues = null
    )
    {
        var properties = document.Properties;
        var metadata = properties.GetPropertyOrNull("metadata")
            is { ValueKind: JsonValueKind.Object } metadataElement
            ? metadataElement
            : (JsonElement?)null;

        var name = document.Name ?? System.IO.Path.GetFileNameWithoutExtension(document.Path);
        var version =
            properties.GetTrimmedString("version") ?? metadata?.GetTrimmedString("version");
        var custom = customValues is null
            ? ImmutableSortedDictionary.Create<string, string>(StringComparer.Ordinal)
            : customValues.ToImmutableSortedDictionary(StringComparer.Ordinal);

        var declaredParameters = properties.GetPropertyOrNull("parameters")
            is { ValueKind: JsonValueKind.Object } parametersElement
            ? parametersElement
            : (JsonElement?)null;

        PolicyEffect? effect = null;
        PolicyEffectDetails? effectDetails = null;
        var resourceTypes = ImmutableArray<string>.Empty;
        var fieldAliases = ImmutableArray<string>.Empty;
        string? policyRuleJson = null;
        var members = ImmutableArray<PolicySetMember>.Empty;
        var groups = ImmutableArray<PolicyDefinitionGroup>.Empty;

        if (document.Kind == PolicyDocumentKind.PolicyDefinition)
        {
            var policyRule = properties.GetPropertyOrNull("policyRule");
            var then = policyRule?.GetPropertyOrNull("then");
            var details = then?.GetPropertyOrNull("details");

            effect = PolicyEffectAnalyzer.Analyze(
                then?.GetStringOrNull("effect"),
                declaredParameters
            );
            effectDetails = details is { ValueKind: JsonValueKind.Object } detailsElement
                ? BuildEffectDetails(detailsElement)
                : null;
            (resourceTypes, fieldAliases) = PolicyRuleAnalyzer.Analyze(
                policyRule?.GetPropertyOrNull("if"),
                details?.GetPropertyOrNull("existenceCondition")
            );
            policyRuleJson = policyRule is { } rule
                ? JsonValueFormatter.ToIndentedJson(rule)
                : null;
        }
        else
        {
            members = BuildMembers(properties);
            groups = BuildGroups(properties, members);
        }

        var parameters = BuildParameters(declaredParameters, effect?.ParameterName);

        return new PolicyDocumentationModel(
            Kind: document.Kind,
            Name: name,
            DisplayName: properties.GetTrimmedString("displayName"),
            Description: properties.GetTrimmedString("description"),
            PolicyType: properties.GetTrimmedString("policyType"),
            Mode: document.Kind == PolicyDocumentKind.PolicyDefinition
                ? properties.GetTrimmedString("mode")
                : null,
            Version: version,
            Category: metadata?.GetTrimmedString("category"),
            IsPreview: metadata?.GetBooleanOrDefault("preview") ?? false,
            IsDeprecated: metadata?.GetBooleanOrDefault("deprecated") ?? false,
            Path: document.Path,
            Custom: custom,
            Metadata: metadata,
            Parameters: parameters,
            Effect: effect,
            EffectDetails: effectDetails,
            ResourceTypes: resourceTypes,
            FieldAliases: fieldAliases,
            PolicyRuleJson: policyRuleJson,
            PolicyDefinitions: members,
            Groups: groups
        );
    }

    private static ImmutableArray<PolicyParameter> BuildParameters(
        JsonElement? declared,
        string? effectParameterName
    )
    {
        if (declared is not { ValueKind: JsonValueKind.Object } parameters)
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<PolicyParameter>();
        foreach (var property in parameters.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var definition = property.Value;
            var parameterMetadata = definition.GetPropertyOrNull("metadata")
                is { ValueKind: JsonValueKind.Object } element
                ? element
                : (JsonElement?)null;
            var defaultValue = definition.GetPropertyOrNull("defaultValue");
            var allowedValues = definition.GetPropertyOrNull("allowedValues")
                is { ValueKind: JsonValueKind.Array } allowed
                ? allowed.EnumerateArray().Select(JsonValueFormatter.ToDisplay).ToImmutableArray()
                : [];
            var isEffect =
                string.Equals(
                    property.Name,
                    effectParameterName,
                    StringComparison.OrdinalIgnoreCase
                )
                || (
                    effectParameterName is null
                    && string.Equals(property.Name, "effect", StringComparison.OrdinalIgnoreCase)
                );

            builder.Add(
                new PolicyParameter(
                    Name: property.Name,
                    Type: definition.GetTrimmedString("type") ?? "string",
                    DisplayName: parameterMetadata?.GetTrimmedString("displayName"),
                    Description: parameterMetadata?.GetTrimmedString("description"),
                    IsRequired: defaultValue is null,
                    DefaultValue: defaultValue is { } value
                        ? JsonValueFormatter.ToDisplay(value)
                        : null,
                    DefaultValueJson: defaultValue is { } json
                        ? JsonValueFormatter.ToIndentedJson(json)
                        : null,
                    AllowedValues: allowedValues,
                    StrongType: parameterMetadata?.GetTrimmedString("strongType"),
                    AssignPermissions: parameterMetadata?.GetBooleanOrDefault("assignPermissions")
                        ?? false,
                    IsDeprecated: parameterMetadata?.GetBooleanOrDefault("deprecated") ?? false,
                    IsEffect: isEffect
                )
            );
        }

        return builder.ToImmutable();
    }

    private static PolicyEffectDetails BuildEffectDetails(JsonElement details)
    {
        var roles = details
            .GetStringArray("roleDefinitionIds")
            .Select(RoleDefinitionCatalog.Resolve)
            .ToImmutableArray();

        var operations = details.GetPropertyOrNull("operations")
            is { ValueKind: JsonValueKind.Array } operationsElement
            ? operationsElement
                .EnumerateArray()
                .Where(operation => operation.ValueKind == JsonValueKind.Object)
                .Select(operation => new ModifyOperation(
                    operation.GetTrimmedString("operation") ?? "unknown",
                    operation.GetTrimmedString("field")
                ))
                .ToImmutableArray()
            : [];

        return new PolicyEffectDetails(
            ResourceType: details.GetTrimmedString("type"),
            RoleDefinitions: roles,
            HasExistenceCondition: details.GetPropertyOrNull("existenceCondition") is not null,
            HasDeployment: details.GetPropertyOrNull("deployment") is not null,
            ModifyOperations: operations
        );
    }

    private static ImmutableArray<PolicySetMember> BuildMembers(JsonElement properties)
    {
        if (
            properties.GetPropertyOrNull("policyDefinitions")
            is not { ValueKind: JsonValueKind.Array } definitions
        )
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<PolicySetMember>();
        var index = 0;
        foreach (var member in definitions.EnumerateArray())
        {
            index++;
            if (member.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var policyDefinitionId = member.GetTrimmedString("policyDefinitionId");
            var policyDefinitionName = member.GetTrimmedString("policyDefinitionName");
            var idName = policyDefinitionId
                ?.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault();
            var referenceId =
                member.GetTrimmedString("policyDefinitionReferenceId")
                ?? policyDefinitionName
                ?? idName
                ?? $"policy-{index}";

            var parameters = member.GetPropertyOrNull("parameters")
                is { ValueKind: JsonValueKind.Object } parameterValues
                ? parameterValues
                    .EnumerateObject()
                    .Select(parameter => new PolicySetMemberParameter(
                        parameter.Name,
                        FormatMemberParameterValue(parameter.Value)
                    ))
                    .ToImmutableArray()
                : [];

            builder.Add(
                new PolicySetMember(
                    ReferenceId: referenceId,
                    PolicyDefinitionId: policyDefinitionId,
                    PolicyDefinitionName: policyDefinitionName,
                    Name: policyDefinitionName ?? idName ?? referenceId,
                    IsBuiltIn: policyDefinitionId?.StartsWith(
                        BuiltInPolicyDefinitionPrefix,
                        StringComparison.OrdinalIgnoreCase
                    ) ?? false,
                    DefinitionVersion: member.GetTrimmedString("definitionVersion"),
                    GroupNames: member.GetStringArray("groupNames").ToImmutableArray(),
                    Parameters: parameters
                )
            );
        }

        return builder.ToImmutable();
    }

    /// <summary>Member parameters are written as <c>{ "value": ... }</c>; EPAC also accepts the bare value.</summary>
    private static string FormatMemberParameterValue(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
        && element.GetPropertyOrNull("value") is { } wrapped
            ? JsonValueFormatter.ToDisplay(wrapped)
            : JsonValueFormatter.ToDisplay(element);

    private static ImmutableArray<PolicyDefinitionGroup> BuildGroups(
        JsonElement properties,
        ImmutableArray<PolicySetMember> members
    )
    {
        if (
            properties.GetPropertyOrNull("policyDefinitionGroups")
            is not { ValueKind: JsonValueKind.Array } groups
        )
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<PolicyDefinitionGroup>();
        foreach (var group in groups.EnumerateArray())
        {
            if (
                group.ValueKind != JsonValueKind.Object
                || group.GetTrimmedString("name") is not { } groupName
            )
            {
                continue;
            }

            var memberCount = members.Count(member =>
                member.GroupNames.Any(candidate =>
                    string.Equals(candidate, groupName, StringComparison.OrdinalIgnoreCase)
                )
            );

            builder.Add(
                new PolicyDefinitionGroup(
                    Name: groupName,
                    DisplayName: group.GetTrimmedString("displayName"),
                    Category: group.GetTrimmedString("category"),
                    Description: group.GetTrimmedString("description"),
                    AdditionalMetadataId: group.GetTrimmedString("additionalMetadataId"),
                    MemberCount: memberCount
                )
            );
        }

        return builder.ToImmutable();
    }
}
