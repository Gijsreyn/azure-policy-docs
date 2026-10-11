using AzurePolicyDocs.Parsing;
using FluentAssertions;
using Xunit;

namespace AzurePolicyDocs.Tests;

public class PolicyDocumentParserTests
{
    [Fact]
    public void ParseFile_FullPayload_DetectsPolicyDefinitionAndName()
    {
        var document = PolicyDocumentParser.ParseFile(
            Fixtures.Path("Definitions", "comprehensive-definition.json")
        );

        document.Kind.Should().Be(PolicyDocumentKind.PolicyDefinition);
        document.Name.Should().Be("deploy-storage-diagnostics");
        document.Properties.TryGetProperty("policyRule", out _).Should().BeTrue();
    }

    [Fact]
    public void ParseFile_PropertyBagOnly_DetectsPolicyDefinitionWithoutName()
    {
        var document = PolicyDocumentParser.ParseFile(
            Fixtures.Path("Definitions", "hard-coded-effect.json")
        );

        document.Kind.Should().Be(PolicyDocumentKind.PolicyDefinition);
        document.Name.Should().BeNull();
        document.Properties.TryGetProperty("displayName", out _).Should().BeTrue();
    }

    [Fact]
    public void ParseFile_PolicySet_DetectsPolicySetDefinition()
    {
        var document = PolicyDocumentParser.ParseFile(
            Fixtures.Path("PolicySets", "grouped-policy-set.json")
        );

        document.Kind.Should().Be(PolicyDocumentKind.PolicySetDefinition);
        document.Name.Should().Be("security-baseline");
    }

    [Fact]
    public void ParseFile_Jsonc_AcceptsCommentsAndTrailingCommas()
    {
        var document = PolicyDocumentParser.ParseFile(
            Fixtures.Path("Definitions", "modify-expression-effect.jsonc")
        );

        document.Kind.Should().Be(PolicyDocumentKind.PolicyDefinition);
        document.Name.Should().Be("modify-costcenter-tag");
    }

    [Fact]
    public void Parse_PropertyNamesAreCaseInsensitive()
    {
        const string json = """
            {
              "Name": "mixed-case",
              "Type": "microsoft.authorization/policydefinitions",
              "Properties": { "DisplayName": "Mixed", "PolicyRule": { "if": {}, "then": { "effect": "Audit" } } }
            }
            """;

        var document = PolicyDocumentParser.Parse(json, "mixed.json");

        document.Kind.Should().Be(PolicyDocumentKind.PolicyDefinition);
        document.Name.Should().Be("mixed-case");
        document.Properties.TryGetProperty("DisplayName", out _).Should().BeTrue();
    }

    [Fact]
    public void Parse_TypePropertyWinsOverHeuristics()
    {
        const string json =
            """{ "type": "Microsoft.Authorization/policySetDefinitions", "properties": { "policyDefinitions": [] } }""";

        PolicyDocumentParser
            .Parse(json, "set.json")
            .Kind.Should()
            .Be(PolicyDocumentKind.PolicySetDefinition);
    }

    [Fact]
    public void ParseFile_NotAPolicy_ThrowsActionableMessage()
    {
        var act = () =>
            PolicyDocumentParser.ParseFile(Fixtures.Path("Invalid", "not-a-policy.json"));

        act.Should()
            .Throw<PolicyDocumentationException>()
            .WithMessage("*is not a policy definition or a policy set definition*");
    }

    [Fact]
    public void ParseFile_MalformedJson_ThrowsWithPath()
    {
        var path = Fixtures.Path("Invalid", "malformed.json");
        var act = () => PolicyDocumentParser.ParseFile(path);

        act.Should()
            .Throw<PolicyDocumentationException>()
            .WithMessage($"*{path}*is not valid JSON*");
    }

    [Fact]
    public void Parse_EmptyContent_Throws()
    {
        var act = () => PolicyDocumentParser.Parse("   ", "empty.json");

        act.Should().Throw<PolicyDocumentationException>().WithMessage("*is empty*");
    }

    [Fact]
    public void Parse_ArrayRoot_Throws()
    {
        var act = () => PolicyDocumentParser.Parse("[]", "array.json");

        act.Should()
            .Throw<PolicyDocumentationException>()
            .WithMessage("*must contain a JSON object*");
    }

    [Fact]
    public void ParseFile_MissingFile_Throws()
    {
        var act = () =>
            PolicyDocumentParser.ParseFile(Fixtures.Path("Invalid", "does-not-exist.json"));

        act.Should().Throw<PolicyDocumentationException>().WithMessage("*Unable to read*");
    }
}
