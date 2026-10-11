using FluentAssertions;
using Xunit;

namespace AzurePolicyDocs.Tests;

public class CliTests
{
    [Fact]
    public void CliAssembly_UsesAzurePolicyDocsCommandName()
    {
        typeof(AzurePolicyDocs.Cli.Program).Assembly.GetName().Name.Should().Be("azurepolicydocs");
    }

    [Fact]
    public async Task Generate_SingleInput_WritesMarkdownBesideInput()
    {
        using var temp = new TempDirectory();
        var input = temp.CopyFixture(
            "policies/storage.json",
            "Definitions",
            "comprehensive-definition.json"
        );

        var result = await CliRunner.RunAsync("generate", input);

        result.ExitCode.Should().Be(0, result.Stderr);
        result.Stdout.Should().BeEmpty();
        File.ReadAllText(temp.Combine("policies", "storage.md"))
            .Should()
            .Be(Fixtures.ReadExpected("comprehensive-definition.md"));
    }

    [Fact]
    public async Task Generate_Stdout_PrintsAndWritesNothing()
    {
        using var temp = new TempDirectory();
        var input = temp.CopyFixture("hard.json", "Definitions", "hard-coded-effect.json");

        var result = await CliRunner.RunAsync("generate", "--stdout", input);

        result.ExitCode.Should().Be(0, result.Stderr);
        result.Stdout.Should().Contain("# Public IP addresses are not allowed");
        Directory.GetFiles(temp.Path, "*.md").Should().BeEmpty();
    }

    [Fact]
    public async Task Generate_OutFile_WritesExactPath()
    {
        using var temp = new TempDirectory();
        var input = temp.CopyFixture("hard.json", "Definitions", "hard-coded-effect.json");
        var output = temp.Combine("docs", "nested", "custom-name.md");

        var result = await CliRunner.RunAsync("generate", input, "--outfile", output);

        result.ExitCode.Should().Be(0, result.Stderr);
        File.Exists(output).Should().BeTrue();
        File.Exists(temp.Combine("hard.md")).Should().BeFalse();
    }

    [Fact]
    public async Task Generate_OutDirWithSingleInput_UsesInputFileName()
    {
        using var temp = new TempDirectory();
        var input = temp.CopyFixture("hard.json", "Definitions", "hard-coded-effect.json");

        var result = await CliRunner.RunAsync("generate", input, "--outdir", temp.Combine("out"));

        result.ExitCode.Should().Be(0, result.Stderr);
        File.Exists(temp.Combine("out", "hard.md")).Should().BeTrue();
    }

    [Fact]
    public async Task Generate_PatternWithOutDir_PreservesRelativeDirectories()
    {
        using var temp = new TempDirectory();
        temp.CopyFixture(
            "Definitions/Storage/storage.json",
            "Definitions",
            "comprehensive-definition.json"
        );
        temp.CopyFixture(
            "Definitions/Tags/tags.jsonc",
            "Definitions",
            "modify-expression-effect.jsonc"
        );
        temp.CopyFixture("Definitions/sets/baseline.json", "PolicySets", "grouped-policy-set.json");
        temp.WriteFile("Definitions/README.md", "not a policy");

        var result = await CliRunner.RunAsync(
            "generate",
            "--pattern",
            temp.Combine("Definitions", "**", "*.json*"),
            "--outdir",
            temp.Combine("docs")
        );

        result.ExitCode.Should().Be(0, result.Stderr);
        File.Exists(temp.Combine("docs", "Storage", "storage.md")).Should().BeTrue();
        File.Exists(temp.Combine("docs", "Tags", "tags.md")).Should().BeTrue();
        File.Exists(temp.Combine("docs", "sets", "baseline.md")).Should().BeTrue();
        Directory
            .GetFiles(temp.Combine("Definitions"), "*.md", SearchOption.AllDirectories)
            .Should()
            .ContainSingle(path => path.EndsWith("README.md"));
    }

    [Fact]
    public async Task Generate_PatternWithoutOutDir_WritesBesideEachInput()
    {
        using var temp = new TempDirectory();
        temp.CopyFixture("a/one.json", "Definitions", "hard-coded-effect.json");
        temp.CopyFixture("b/two.json", "Definitions", "hard-coded-effect.json");

        var result = await CliRunner.RunAsync(
            "generate",
            "--pattern",
            temp.Combine("**", "*.json")
        );

        result.ExitCode.Should().Be(0, result.Stderr);
        File.Exists(temp.Combine("a", "one.md")).Should().BeTrue();
        File.Exists(temp.Combine("b", "two.md")).Should().BeTrue();
    }

    [Fact]
    public async Task Generate_Pattern_ContinuesAfterInvalidFileAndReturnsOne()
    {
        using var temp = new TempDirectory();
        temp.CopyFixture("policies/good.json", "Definitions", "hard-coded-effect.json");
        temp.CopyFixture("policies/bad.json", "Invalid", "not-a-policy.json");

        var result = await CliRunner.RunAsync(
            "generate",
            "--pattern",
            temp.Combine("policies", "*.json")
        );

        result.ExitCode.Should().Be(1);
        result.Stderr.Should().Contain("bad.json").And.Contain("is not a policy definition");
        File.Exists(temp.Combine("policies", "good.md")).Should().BeTrue();
        File.Exists(temp.Combine("policies", "bad.md")).Should().BeFalse();
    }

    [Fact]
    public async Task Generate_UnmatchedPattern_ReturnsZeroAndWarns()
    {
        using var temp = new TempDirectory();

        var result = await CliRunner.RunAsync(
            "generate",
            "--pattern",
            temp.Combine("**", "*.json")
        );

        result.ExitCode.Should().Be(0);
        result.Stdout.Should().BeEmpty();
        result.Stderr.Should().Contain("No files matched");
    }

    [Fact]
    public async Task Generate_MissingInputAndPattern_ReturnsOne()
    {
        var result = await CliRunner.RunAsync("generate");

        result.ExitCode.Should().Be(1);
        result
            .Stderr.Should()
            .Contain("Either the input file path or the --pattern parameter must be specified");
    }

    [Fact]
    public async Task Generate_DirectoryInput_IsRejected()
    {
        using var temp = new TempDirectory();

        var result = await CliRunner.RunAsync("generate", temp.Path);

        result.ExitCode.Should().Be(1);
        result.Stderr.Should().Contain("is a directory");
    }

    [Theory]
    [InlineData("generate", "x.json", "--pattern", "**/*.json")]
    [InlineData("generate", "--stdout", "--pattern", "**/*.json")]
    [InlineData("generate", "x.json", "--stdout", "--outdir", "docs")]
    [InlineData("generate", "x.json", "--stdout", "--outfile", "out.md")]
    [InlineData("generate", "x.json", "--outdir", "docs", "--outfile", "out.md")]
    [InlineData("generate", "--pattern", "**/*.json", "--outfile", "out.md")]
    [InlineData("generate", "x.json", "--custom-template-value", "novalue")]
    [InlineData("generate", "x.json", "--not-an-option")]
    public async Task Generate_InvalidArgumentCombinations_ReturnOne(params string[] args)
    {
        var result = await CliRunner.RunAsync(args);

        result.ExitCode.Should().Be(1);
        result.Stderr.Should().NotBeEmpty();
        result.Stderr.Should().NotContain("Unhandled exception");
    }

    [Fact]
    public async Task Generate_RejectsOutputThatOverwritesInput()
    {
        using var temp = new TempDirectory();
        var input = temp.CopyFixture("policy.json", "Definitions", "hard-coded-effect.json");

        var result = await CliRunner.RunAsync("generate", input, "--outfile", input);

        result.ExitCode.Should().Be(1);
        result.Stderr.Should().Contain("cannot overwrite the input");
        File.ReadAllText(input).Should().Contain("policyRule");
    }

    [Fact]
    public async Task Generate_RejectsPolicySourceExtensionForOutput()
    {
        using var temp = new TempDirectory();
        var input = temp.CopyFixture("policy.json", "Definitions", "hard-coded-effect.json");

        var result = await CliRunner.RunAsync(
            "generate",
            input,
            "--outfile",
            temp.Combine("other.jsonc")
        );

        result.ExitCode.Should().Be(1);
        result.Stderr.Should().Contain("policy source file extension");
        File.Exists(temp.Combine("other.jsonc")).Should().BeFalse();
    }

    [Fact]
    public async Task Generate_PatternWithOutDir_KeepsSameNamedFilesApart()
    {
        using var temp = new TempDirectory();
        temp.CopyFixture("a/policy.json", "Definitions", "hard-coded-effect.json");
        temp.CopyFixture("b/policy.json", "Definitions", "hard-coded-effect.json");

        var result = await CliRunner.RunAsync(
            "generate",
            "--pattern",
            temp.Combine("*", "policy.json"),
            "--outdir",
            temp.Combine("docs")
        );

        result.ExitCode.Should().Be(0, result.Stderr);
        File.Exists(temp.Combine("docs", "a", "policy.md")).Should().BeTrue();
        File.Exists(temp.Combine("docs", "b", "policy.md")).Should().BeTrue();
    }

    [Fact]
    public async Task Generate_CustomTemplateWithValues_MergesInCommandLineOrder()
    {
        using var temp = new TempDirectory();
        var input = temp.CopyFixture("policy.json", "Definitions", "hard-coded-effect.json");
        var template = temp.WriteFile(
            "t.scriban",
            "{{ custom.owner }}|{{ custom.fromFile }}|{{ custom.inline }}"
        );
        var values = Fixtures.Path("Templates", "values.json");

        var fileLast = await CliRunner.RunAsync(
            "generate",
            "--stdout",
            input,
            "--template",
            template,
            "--custom-template-value",
            "owner=inline",
            "--custom-template-value-file-path",
            values,
            "--custom-template-value",
            "inline=yes"
        );
        var inlineLast = await CliRunner.RunAsync(
            "generate",
            "--stdout",
            input,
            "--template",
            template,
            "--custom-template-value-file-path",
            values,
            "--custom-template-value",
            "owner=inline"
        );

        fileLast.ExitCode.Should().Be(0, fileLast.Stderr);
        fileLast.Stdout.Should().Be("Platform Team|yes|yes\n");
        inlineLast.ExitCode.Should().Be(0, inlineLast.Stderr);
        inlineLast.Stdout.Should().Be("inline|yes|\n");
    }

    [Fact]
    public async Task Generate_CustomTemplateWithIncludeRoot_ResolvesIncludes()
    {
        using var temp = new TempDirectory();
        var input = temp.CopyFixture("policy.json", "Definitions", "comprehensive-definition.json");

        var result = await CliRunner.RunAsync(
            "generate",
            "--stdout",
            input,
            "--template",
            Fixtures.Path("Templates", "custom.scriban"),
            "--include-root",
            Fixtures.Path("Templates"),
            "--custom-template-value",
            "owner=Platform Team"
        );

        result.ExitCode.Should().Be(0, result.Stderr);
        result
            .Stdout.Should()
            .Be(
                "> Generated policy documentation.\n\n# deploy-storage-diagnostics\n\nOwner: Platform Team\nKind: policyDefinition\nParameters: 7\nDocumentation footer.\n"
            );
    }

    [Theory]
    [InlineData("[]", "must contain a JSON object")]
    [InlineData("""{ "count": 1 }""", "must be a string")]
    [InlineData("""{ "": "value" }""", "contains an empty key")]
    [InlineData("""{ "a": "1", "a": "2" }""", "duplicate key")]
    [InlineData("{ invalid", "is not valid JSON")]
    public async Task Generate_CustomValueFile_RejectsInvalidContent(
        string contents,
        string expectedError
    )
    {
        using var temp = new TempDirectory();
        var input = temp.CopyFixture("policy.json", "Definitions", "hard-coded-effect.json");
        var values = temp.WriteFile("values.json", contents);

        var result = await CliRunner.RunAsync(
            "generate",
            "--stdout",
            input,
            "--custom-template-value-file-path",
            values
        );

        result.ExitCode.Should().Be(1);
        result.Stderr.Should().Contain(expectedError);
    }

    [Fact]
    public async Task Generate_InvalidTemplate_FailsOnceAndPreservesExistingOutput()
    {
        using var temp = new TempDirectory();
        temp.CopyFixture("a/one.json", "Definitions", "hard-coded-effect.json");
        temp.CopyFixture("b/two.json", "Definitions", "hard-coded-effect.json");
        var existing = temp.WriteFile("a/one.md", "preserve me");
        var template = temp.WriteFile("broken.scriban", "{{ if policy.name }}");

        var result = await CliRunner.RunAsync(
            "generate",
            "--pattern",
            temp.Combine("*", "*.json"),
            "--template",
            template
        );

        result.ExitCode.Should().Be(1);
        result.Stderr.Should().Contain("Failed to parse");
        result
            .Stderr.Split("Failed to parse")
            .Should()
            .HaveCount(2, "the template error is reported once, not per input");
        File.ReadAllText(existing).Should().Be("preserve me");
    }

    [Fact]
    public async Task Generate_MissingTemplate_ReturnsOne()
    {
        using var temp = new TempDirectory();
        var input = temp.CopyFixture("policy.json", "Definitions", "hard-coded-effect.json");

        var result = await CliRunner.RunAsync(
            "generate",
            "--stdout",
            input,
            "--template",
            temp.Combine("missing.scriban")
        );

        result.ExitCode.Should().Be(1);
        result.Stderr.Should().Contain("does not exist");
    }

    [Fact]
    public async Task Catalog_Pattern_WritesReadmeInPatternRootWithLinks()
    {
        using var temp = new TempDirectory();
        temp.CopyFixture(
            "Definitions/Storage/storage.json",
            "Definitions",
            "comprehensive-definition.json"
        );
        temp.CopyFixture("Definitions/Sets/baseline.json", "PolicySets", "grouped-policy-set.json");

        var result = await CliRunner.RunAsync(
            "catalog",
            "--pattern",
            temp.Combine("Definitions", "**", "*.json")
        );

        result.ExitCode.Should().Be(0, result.Stderr);
        var catalog = File.ReadAllText(temp.Combine("Definitions", "README.md"));
        catalog.Should().Contain("# Azure Policy catalog");
        catalog
            .Should()
            .Contain(
                "[Deploy diagnostic settings for storage accounts \\| preview](Storage/storage.md)"
            );
        catalog.Should().Contain("[Security baseline](Sets/baseline.md)");
    }

    [Fact]
    public async Task Catalog_DocsDirAndOutFile_LinkIntoDocsDirectory()
    {
        using var temp = new TempDirectory();
        temp.CopyFixture(
            "Definitions/Storage/storage.json",
            "Definitions",
            "comprehensive-definition.json"
        );

        var result = await CliRunner.RunAsync(
            "catalog",
            "--pattern",
            temp.Combine("Definitions", "**", "*.json"),
            "--outfile",
            temp.Combine("docs", "README.md"),
            "--docs-dir",
            temp.Combine("docs"),
            "--custom-template-value",
            "title=Contoso policies"
        );

        result.ExitCode.Should().Be(0, result.Stderr);
        var catalog = File.ReadAllText(temp.Combine("docs", "README.md"));
        catalog.Should().StartWith("# Contoso policies");
        catalog.Should().Contain("](Storage/storage.md)");
    }

    [Fact]
    public async Task Catalog_Stdout_PrintsAndContinuesPastInvalidFiles()
    {
        using var temp = new TempDirectory();
        temp.CopyFixture("p/good.json", "Definitions", "hard-coded-effect.json");
        temp.CopyFixture("p/bad.json", "Invalid", "not-a-policy.json");

        var result = await CliRunner.RunAsync(
            "catalog",
            "--stdout",
            "--pattern",
            temp.Combine("p", "*.json")
        );

        result.ExitCode.Should().Be(1);
        result.Stdout.Should().Contain("Public IP addresses are not allowed");
        result.Stderr.Should().Contain("bad.json");
    }

    [Fact]
    public async Task Catalog_RequiresPattern()
    {
        var result = await CliRunner.RunAsync("catalog");

        result.ExitCode.Should().Be(1);
        result.Stderr.Should().Contain("--pattern");
    }

    [Theory]
    [InlineData("policyDefinition", "## Policy rule")]
    [InlineData("policySetDefinition", "## Policy definitions")]
    [InlineData("catalog", "catalog.policyDefinitions")]
    public async Task TemplateExport_PrintsBuiltInTemplate(string kind, string marker)
    {
        var result = await CliRunner.RunAsync("template", "export", "--kind", kind);

        result.ExitCode.Should().Be(0, result.Stderr);
        result.Stdout.Should().Contain(marker);
    }

    [Fact]
    public async Task TemplateExport_WritesToFile()
    {
        using var temp = new TempDirectory();
        var output = temp.Combine("templates", "definition.scriban");

        var result = await CliRunner.RunAsync(
            "template",
            "export",
            "--kind",
            "policyDefinition",
            "--outfile",
            output
        );

        result.ExitCode.Should().Be(0, result.Stderr);
        File.ReadAllText(output).Should().Contain("## Policy rule");
    }

    [Fact]
    public async Task TemplateExport_RejectsUnknownKind()
    {
        var result = await CliRunner.RunAsync("template", "export", "--kind", "nope");

        result.ExitCode.Should().Be(1);
    }

    [Fact]
    public async Task Help_ListsCommands()
    {
        var result = await CliRunner.RunAsync("--help");

        result.ExitCode.Should().Be(0);
        result.Stdout.Should().Contain("generate").And.Contain("catalog").And.Contain("template");
    }

    [Fact]
    public async Task Version_PrintsAssemblyVersion()
    {
        var result = await CliRunner.RunAsync("--version");

        result.ExitCode.Should().Be(0);
        result.Stdout.Trim().Should().MatchRegex(@"^\d+\.\d+\.\d+");
    }
}
