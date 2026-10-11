using System.Collections.Immutable;
using System.Text.Json;
using AzurePolicyDocs.Model;
using AzurePolicyDocs.Parsing;
using Scriban.Runtime;

namespace AzurePolicyDocs.Rendering;

/// <summary>
/// Converts models into Scriban script objects with explicit camelCase keys. Building the objects by hand keeps
/// the template contract stable and documented in docs/template-model.md, independent of C# member names.
/// </summary>
internal static class PolicyDocumentationScriptModelFactory
{
    public static ScriptObject Create(PolicyDocumentationModel model)
    {
        var custom = CreateCustom(model.Custom);

        return new ScriptObject { { "custom", custom }, { "policy", CreatePolicy(model, custom) } };
    }

    public static ScriptObject CreateCatalog(PolicyCatalogModel model)
    {
        var custom = CreateCustom(model.Custom);

        return new ScriptObject
        {
            { "custom", custom },
            {
                "catalog",
                new ScriptObject
                {
                    { "custom", custom },
                    {
                        "policyDefinitions",
                        CreateArray(model.PolicyDefinitions, CreateCatalogEntry)
                    },
                    {
                        "policySetDefinitions",
                        CreateArray(model.PolicySetDefinitions, CreateCatalogEntry)
                    },
                }
            },
        };
    }

    private static ScriptObject CreatePolicy(PolicyDocumentationModel model, ScriptObject custom) =>
        new()
        {
            { "kind", KindName(model.Kind) },
            { "name", model.Name },
            { "displayName", model.DisplayName },
            { "description", model.Description },
            { "policyType", model.PolicyType },
            { "mode", model.Mode },
            { "version", model.Version },
            { "category", model.Category },
            { "preview", model.IsPreview },
            { "deprecated", model.IsDeprecated },
            { "path", model.Path },
            { "custom", custom },
            { "metadata", model.Metadata is { } metadata ? ConvertJson(metadata) : null },
            { "parameters", CreateArray(model.Parameters, CreateParameter) },
            { "effect", model.Effect is { } effect ? CreateEffect(effect) : null },
            {
                "effectDetails",
                model.EffectDetails is { } details ? CreateEffectDetails(details) : null
            },
            { "resourceTypes", CreateStrings(model.ResourceTypes) },
            { "fieldAliases", CreateStrings(model.FieldAliases) },
            { "policyRuleJson", model.PolicyRuleJson },
            {
                "policyRuleFence",
                model.PolicyRuleJson is { } rule ? JsonValueFormatter.GetCodeFence(rule) : null
            },
            { "policyDefinitions", CreateArray(model.PolicyDefinitions, CreateMember) },
            { "policyCount", model.PolicyDefinitions.Length },
            { "groups", CreateArray(model.Groups, CreateGroup) },
        };

    private static string KindName(PolicyDocumentKind kind) =>
        kind switch
        {
            PolicyDocumentKind.PolicySetDefinition => "policySetDefinition",
            _ => "policyDefinition",
        };

    private static ScriptObject CreateParameter(PolicyParameter parameter) =>
        new()
        {
            { "name", parameter.Name },
            { "type", parameter.Type },
            { "displayName", parameter.DisplayName },
            { "description", parameter.Description },
            { "required", parameter.IsRequired },
            { "defaultValue", parameter.DefaultValue },
            { "defaultValueJson", parameter.DefaultValueJson },
            {
                "defaultValueFence",
                parameter.DefaultValueJson is { } json
                    ? JsonValueFormatter.GetCodeFence(json)
                    : null
            },
            { "allowedValues", CreateStrings(parameter.AllowedValues) },
            { "strongType", parameter.StrongType },
            { "assignPermissions", parameter.AssignPermissions },
            { "deprecated", parameter.IsDeprecated },
            { "effect", parameter.IsEffect },
        };

    private static ScriptObject CreateEffect(PolicyEffect effect) =>
        new()
        {
            { "raw", effect.Raw },
            {
                "kind",
                effect.Kind switch
                {
                    PolicyEffectKind.Parameter => "parameter",
                    PolicyEffectKind.Expression => "expression",
                    _ => "literal",
                }
            },
            { "parameterName", effect.ParameterName },
            { "defaultEffect", effect.DefaultEffect },
            { "allowedEffects", CreateStrings(effect.AllowedEffects) },
        };

    private static ScriptObject CreateEffectDetails(PolicyEffectDetails details) =>
        new()
        {
            { "resourceType", details.ResourceType },
            { "roleDefinitions", CreateArray(details.RoleDefinitions, CreateRole) },
            { "hasExistenceCondition", details.HasExistenceCondition },
            { "hasDeployment", details.HasDeployment },
            { "modifyOperations", CreateArray(details.ModifyOperations, CreateOperation) },
        };

    private static ScriptObject CreateRole(RoleDefinition role) =>
        new()
        {
            { "id", role.Id },
            { "guid", role.Guid },
            { "name", role.Name },
        };

    private static ScriptObject CreateOperation(ModifyOperation operation) =>
        new() { { "operation", operation.Operation }, { "field", operation.Field } };

    private static ScriptObject CreateMember(PolicySetMember member) =>
        new()
        {
            { "referenceId", member.ReferenceId },
            { "policyDefinitionId", member.PolicyDefinitionId },
            { "policyDefinitionName", member.PolicyDefinitionName },
            { "name", member.Name },
            { "builtIn", member.IsBuiltIn },
            { "definitionVersion", member.DefinitionVersion },
            { "groupNames", CreateStrings(member.GroupNames) },
            { "parameters", CreateArray(member.Parameters, CreateMemberParameter) },
        };

    private static ScriptObject CreateMemberParameter(PolicySetMemberParameter parameter) =>
        new() { { "name", parameter.Name }, { "value", parameter.Value } };

    private static ScriptObject CreateGroup(PolicyDefinitionGroup group) =>
        new()
        {
            { "name", group.Name },
            { "displayName", group.DisplayName },
            { "category", group.Category },
            { "description", group.Description },
            { "additionalMetadataId", group.AdditionalMetadataId },
            { "memberCount", group.MemberCount },
        };

    private static ScriptObject CreateCatalogEntry(PolicyCatalogEntry entry) =>
        new()
        {
            { "kind", KindName(entry.Kind) },
            { "name", entry.Name },
            { "displayName", entry.DisplayName },
            { "description", entry.Description },
            { "category", entry.Category },
            { "mode", entry.Mode },
            { "version", entry.Version },
            { "defaultEffect", entry.DefaultEffect },
            { "allowedEffects", CreateStrings(entry.AllowedEffects) },
            { "policyCount", entry.PolicyCount },
            { "path", entry.Path },
            { "documentationPath", entry.DocumentationPath },
        };

    private static ScriptObject CreateCustom(ImmutableSortedDictionary<string, string> custom)
    {
        var scriptObject = new ScriptObject();
        foreach (var (key, value) in custom)
        {
            scriptObject.Add(key, value);
        }

        return scriptObject;
    }

    private static ScriptArray CreateStrings(ImmutableArray<string> values)
    {
        var array = new ScriptArray();
        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }

    private static ScriptArray CreateArray<T>(ImmutableArray<T> items, Func<T, object?> project)
    {
        var array = new ScriptArray();
        foreach (var item in items)
        {
            array.Add(project(item));
        }

        return array;
    }

    /// <summary>Exposes free-form JSON such as <c>metadata</c> so templates can read any property an author added.</summary>
    internal static object? ConvertJson(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var scriptObject = new ScriptObject();
                foreach (var property in element.EnumerateObject())
                {
                    scriptObject[property.Name] = ConvertJson(property.Value);
                }

                return scriptObject;

            case JsonValueKind.Array:
                var array = new ScriptArray();
                foreach (var item in element.EnumerateArray())
                {
                    array.Add(ConvertJson(item));
                }

                return array;

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                return element.TryGetInt64(out var integer) ? integer : element.GetDouble();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            default:
                return null;
        }
    }
}
