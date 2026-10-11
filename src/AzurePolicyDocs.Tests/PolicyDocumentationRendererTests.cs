using AzurePolicyDocs.Model;
using AzurePolicyDocs.Parsing;
using AzurePolicyDocs.Rendering;
using FluentAssertions;
using Xunit;

namespace AzurePolicyDocs.Tests;

public class PolicyDocumentationRendererTests
{
    private readonly PolicyDocumentationGenerator generator = new();

    [Fact]
    public void Render_NullModel_ThrowsArgumentNullException()
    {
        var act = () => generator.Render(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("model");
    }

    [Fact]
    public void RenderCatalog_NullModel_ThrowsArgumentNullException()
    {
        var act = () => generator.RenderCatalog(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("model");
    }

    [Theory]
    [InlineData("Definitions", "comprehensive-definition.json", "comprehensive-definition.md")]
    [InlineData("Definitions", "hard-coded-effect.json", "hard-coded-effect.md")]
    [InlineData("Definitions", "modify-expression-effect.jsonc", "modify-expression-effect.md")]
    [InlineData(
        "PolicySets",
        "allowed-locations-initiative.jsonc",
        "allowed-locations-initiative.md"
    )]
    [InlineData("PolicySets", "grouped-policy-set.json", "grouped-policy-set.md")]
    public void Render_BuiltInTemplate_MatchesGoldenFile(
        string folder,
        string fixture,
        string expected
    )
    {
        var rendered = generator.Generate(Fixtures.Path(folder, fixture));

        rendered.Should().Be(Fixtures.ReadExpected(expected));
    }

    [Fact]
    public void Render_OutputUsesLfAndExactlyOneTrailingNewline()
    {
        var rendered = generator.Generate(Fixtures.Path("Definitions", "hard-coded-effect.json"));

        rendered.Should().NotContain("\r");
        rendered.Should().EndWith("\n");
        rendered.Should().NotEndWith("\n\n");
    }

    [Fact]
    public void Render_CustomTemplate_SupportsIncludesAndCustomValues()
    {
        var options = new PolicyDocumentationGenerationOptions(
            TemplateFile: Fixtures.Path("Templates", "custom.scriban"),
            IncludeRoot: Fixtures.Path("Templates"),
            CustomValues: new Dictionary<string, string> { ["owner"] = "Platform Team" }
        );

        var rendered = generator.Generate(
            Fixtures.Path("Definitions", "comprehensive-definition.json"),
            options
        );

        rendered
            .Should()
            .Be(
                "> Generated policy documentation.\n\n# deploy-storage-diagnostics\n\nOwner: Platform Team\nKind: policyDefinition\nParameters: 7\nDocumentation footer.\n"
            );
    }

    [Fact]
    public void Render_CustomValuesOnOptions_OverrideValuesOnModel()
    {
        var model = generator.BuildModel(
            Fixtures.Path("Definitions", "hard-coded-effect.json"),
            new Dictionary<string, string> { ["owner"] = "model" }
        );
        using var temp = new TempDirectory();
        var template = temp.WriteFile(
            "owner.scriban",
            "{{ custom.owner }}|{{ policy.custom.owner }}"
        );

        var rendered = generator.Render(
            model,
            new PolicyDocumentationGenerationOptions(
                template,
                CustomValues: new Dictionary<string, string> { ["owner"] = "options" }
            )
        );

        rendered.Should().Be("options|options\n");
    }

    [Fact]
    public void Render_IncludeRootDefaultsToInputDirectory()
    {
        using var temp = new TempDirectory();
        var input = temp.CopyFixture(
            "policies/policy.json",
            "Definitions",
            "hard-coded-effect.json"
        );
        temp.WriteFile("policies/_part.md", "included");
        var template = temp.WriteFile(
            "templates/with-include.scriban",
            "{{ include \"_part.md\" }}|{{ policy.name }}"
        );

        var rendered = generator.Generate(
            input,
            new PolicyDocumentationGenerationOptions(template)
        );

        rendered.Should().Be("included|policy\n");
    }

    [Fact]
    public void Render_MetadataIsExposedAsFreeFormObject()
    {
        using var temp = new TempDirectory();
        var template = temp.WriteFile(
            "meta.scriban",
            "{{ policy.metadata.owner }}/{{ policy.metadata.preview }}"
        );

        var rendered = generator.Generate(
            Fixtures.Path("Definitions", "comprehensive-definition.json"),
            new PolicyDocumentationGenerationOptions(template)
        );

        rendered.Should().Be("platform-team/true\n");
    }

    [Fact]
    public void Render_InvalidTemplate_ThrowsParseError()
    {
        using var temp = new TempDirectory();
        var template = temp.WriteFile("broken.scriban", "{{ if policy.name }}");

        var act = () =>
            generator.Generate(
                Fixtures.Path("Definitions", "hard-coded-effect.json"),
                new PolicyDocumentationGenerationOptions(template)
            );

        act.Should()
            .Throw<PolicyDocumentationException>()
            .WithMessage("Failed to parse the documentation template*");
    }

    [Fact]
    public void Render_MissingInclude_ThrowsRenderError()
    {
        using var temp = new TempDirectory();
        var template = temp.WriteFile("missing.scriban", "{{ include \"nope.md\" }}");

        var act = () =>
            generator.Generate(
                Fixtures.Path("Definitions", "hard-coded-effect.json"),
                new PolicyDocumentationGenerationOptions(template, temp.Path)
            );

        act.Should()
            .Throw<PolicyDocumentationException>()
            .WithMessage("Failed to render*does not exist*");
    }

    [Fact]
    public void Render_MissingTemplateFile_Throws()
    {
        var act = () =>
            generator.Generate(
                Fixtures.Path("Definitions", "hard-coded-effect.json"),
                new PolicyDocumentationGenerationOptions("missing.scriban")
            );

        act.Should().Throw<PolicyDocumentationException>().WithMessage("*does not exist*");
    }

    [Fact]
    public void ValidateTemplate_RejectsBrokenTemplate()
    {
        using var temp = new TempDirectory();
        var template = temp.WriteFile("broken.scriban", "{{ for x in }}");

        var act = () => PolicyDocumentationRenderer.ValidateTemplate(template);

        act.Should().Throw<PolicyDocumentationException>();
    }

    [Fact]
    public void RenderCatalog_BuiltInTemplate_MatchesGoldenFile()
    {
        var catalog = PolicyCatalogModel.Create([
            (
                generator.BuildModel(Fixtures.Path("Definitions", "comprehensive-definition.json")),
                "Definitions/comprehensive-definition.json",
                "Definitions/comprehensive-definition.md"
            ),
            (
                generator.BuildModel(Fixtures.Path("Definitions", "hard-coded-effect.json")),
                "Definitions/hard-coded-effect.json",
                "Definitions/hard-coded-effect.md"
            ),
            (
                generator.BuildModel(
                    Fixtures.Path("Definitions", "modify-expression-effect.jsonc")
                ),
                "Definitions/modify-expression-effect.jsonc",
                "Definitions/modify-expression-effect.md"
            ),
            (
                generator.BuildModel(
                    Fixtures.Path("PolicySets", "allowed-locations-initiative.jsonc")
                ),
                "PolicySets/allowed-locations-initiative.jsonc",
                "PolicySets/allowed-locations-initiative.md"
            ),
            (
                generator.BuildModel(Fixtures.Path("PolicySets", "grouped-policy-set.json")),
                "PolicySets/grouped-policy-set.json",
                "PolicySets/grouped-policy-set.md"
            ),
        ]);

        var rendered = generator.RenderCatalog(
            catalog,
            new PolicyDocumentationGenerationOptions(
                CustomValues: new Dictionary<string, string> { ["title"] = "Contoso policies" }
            )
        );

        rendered.Should().Be(Fixtures.ReadExpected("catalog.md"));
    }

    [Fact]
    public void BuiltInTemplateSources_AreAvailable()
    {
        PolicyDocumentationRenderer
            .GetBuiltInTemplateSource(PolicyDocumentKind.PolicyDefinition)
            .Should()
            .Contain("## Policy rule");
        PolicyDocumentationRenderer
            .GetBuiltInTemplateSource(PolicyDocumentKind.PolicySetDefinition)
            .Should()
            .Contain("## Policy definitions");
        PolicyDocumentationRenderer
            .GetBuiltInCatalogTemplateSource()
            .Should()
            .Contain("catalog.policyDefinitions");
    }
}
