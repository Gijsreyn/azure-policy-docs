using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;
using AzurePolicyDocs.Model;
using AzurePolicyDocs.Parsing;

namespace AzurePolicyDocs.Analysis;

/// <summary>
/// Works out which effects a policy rule can take: a literal, the allowed values of the referenced parameter,
/// or the effect literals that appear inside a more complex expression.
/// </summary>
internal static partial class PolicyEffectAnalyzer
{
    private static readonly string[] KnownEffects =
    [
        "Audit",
        "Deny",
        "Disabled",
        "AuditIfNotExists",
        "DeployIfNotExists",
        "Append",
        "Modify",
        "DenyAction",
        "Manual",
        "AddToNetworkGroup",
        "Mutate",
        "EnforceSetting",
        "EnforceRegoPolicy",
        "EnforceOPAConstraint",
    ];

    public static PolicyEffect? Analyze(string? rawEffect, JsonElement? parameters)
    {
        if (string.IsNullOrWhiteSpace(rawEffect))
        {
            return null;
        }

        var raw = rawEffect.Trim();
        var parameterMatch = ParameterReference().Match(raw);
        if (parameterMatch.Success)
        {
            var parameterName = parameterMatch.Groups["name"].Value;
            var parameter = FindParameter(parameters, parameterName);
            var allowed = parameter is { } declared
                ? declared.GetStringArray("allowedValues").ToImmutableArray()
                : [];
            var defaultEffect = parameter?.GetStringOrNull("defaultValue");

            return new PolicyEffect(
                raw,
                PolicyEffectKind.Parameter,
                parameterName,
                defaultEffect,
                allowed
            );
        }

        if (raw.StartsWith('['))
        {
            var literals = QuotedLiteral()
                .Matches(raw)
                .Select(match => match.Groups["value"].Value)
                .Select(CanonicalEffectName)
                .OfType<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToImmutableArray();

            return new PolicyEffect(raw, PolicyEffectKind.Expression, null, null, literals);
        }

        return new PolicyEffect(raw, PolicyEffectKind.Literal, null, raw, [raw]);
    }

    private static JsonElement? FindParameter(JsonElement? parameters, string name)
    {
        if (parameters is not { ValueKind: JsonValueKind.Object } declared)
        {
            return null;
        }

        return
            declared.TryGetPropertyIgnoreCase(name, out var parameter)
            && parameter.ValueKind == JsonValueKind.Object
            ? parameter
            : null;
    }

    private static string? CanonicalEffectName(string candidate) =>
        KnownEffects.FirstOrDefault(effect =>
            string.Equals(effect, candidate, StringComparison.OrdinalIgnoreCase)
        );

    [GeneratedRegex(
        @"^\[\s*parameters\(\s*'(?<name>[^']+)'\s*\)\s*\]$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    )]
    private static partial Regex ParameterReference();

    [GeneratedRegex(@"'(?<value>[^']*)'", RegexOptions.CultureInvariant)]
    private static partial Regex QuotedLiteral();
}
