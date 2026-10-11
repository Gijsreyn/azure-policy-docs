using AzurePolicyDocs.Cli;
using FluentAssertions;
using Xunit;

namespace AzurePolicyDocs.Tests;

public class InputResolverTests
{
    [Theory]
    [InlineData("*.json", ".", "*.json")]
    [InlineData("**/*.json", ".", "**/*.json")]
    [InlineData("policy.json", ".", "policy.json")]
    [InlineData("definitions/**/*.json", "definitions", "**/*.json")]
    [InlineData("definitions/policy?.json", "definitions", "policy?.json")]
    [InlineData("definitions/nested/policy.json", "definitions/nested", "policy.json")]
    [InlineData("definitions/*/policy.json", "definitions", "*/policy.json")]
    [InlineData("definitions?/nested/*.json", ".", "definitions?/nested/*.json")]
    [InlineData("definitions with spaces/*.json", "definitions with spaces", "*.json")]
    public void SplitOnWildcard_UsesLongestLiteralDirectory(
        string pattern,
        string expectedRoot,
        string expectedPattern
    )
    {
        var (root, relativePattern) = InputResolver.SplitOnWildcard(pattern);

        root.Should().Be(Path.GetFullPath(expectedRoot));
        relativePattern.Should().Be(expectedPattern);
    }

    [Theory]
    [InlineData("*.json")]
    [InlineData("**/*.json")]
    [InlineData("policy?.json")]
    public void SplitOnWildcard_PreservesFileSystemRoot(string pattern)
    {
        var expectedRoot = Path.GetPathRoot(Directory.GetCurrentDirectory())!;

        var (root, relativePattern) = InputResolver.SplitOnWildcard(expectedRoot + pattern);

        root.Should().Be(expectedRoot);
        relativePattern.Should().Be(pattern);
    }

    [Fact]
    public void SplitOnWildcard_PreservesLeadingSeparator()
    {
        var (root, relativePattern) = InputResolver.SplitOnWildcard("/**/*.json");

        root.Should().Be(Path.GetFullPath("/"));
        relativePattern.Should().Be("**/*.json");
    }

    [Fact]
    public void SplitOnWildcard_HandlesNativeDirectorySeparators()
    {
        using var temp = new TempDirectory();
        var expectedRoot = temp.Combine("definitions", "nested");

        var (root, relativePattern) = InputResolver.SplitOnWildcard(
            Path.Combine(expectedRoot, "*.json")
        );

        root.Should().Be(expectedRoot);
        relativePattern.Should().Be("*.json");
    }

    [Fact]
    public void ResolvePattern_OrdersFilesOrdinally()
    {
        using var temp = new TempDirectory();
        var lowerCase = temp.WriteFile("b.json", "{}");
        var upperCase = temp.WriteFile("Z.json", "{}");

        var inputs = InputResolver.ResolvePattern(temp.Combine("*.json"));

        inputs.Select(input => input.FullPath).Should().Equal(upperCase, lowerCase);
    }
}
