namespace AzurePolicyDocs.Tests;

internal static class Fixtures
{
    public static string Root { get; } =
        System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures");

    public static string Path(params string[] segments) =>
        System.IO.Path.Combine([Root, .. segments]);

    public static string Read(params string[] segments) => File.ReadAllText(Path(segments));

    /// <summary>Expected files are stored with LF; normalize in case a checkout converted them.</summary>
    public static string ReadExpected(string name) =>
        Read("Expected", name).ReplaceLineEndings("\n");
}

/// <summary>A scratch directory that is removed when the test finishes.</summary>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "policydocs-tests",
            Guid.NewGuid().ToString("n")
        );
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string Combine(params string[] segments) => System.IO.Path.Combine([Path, .. segments]);

    public string CopyFixture(string relativeTarget, params string[] fixtureSegments)
    {
        var target = Combine(relativeTarget);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target)!);
        File.Copy(Fixtures.Path(fixtureSegments), target, overwrite: true);
        return target;
    }

    public string WriteFile(string relativeTarget, string contents)
    {
        var target = Combine(relativeTarget);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target)!);
        File.WriteAllText(target, contents);
        return target;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Best effort; the OS temp cleaner takes care of stragglers.
        }
    }
}

internal sealed record CliResult(int ExitCode, string Stdout, string Stderr);

internal static class CliRunner
{
    public static async Task<CliResult> RunAsync(params string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exitCode = await AzurePolicyDocs.Cli.Program.RunAsync(args, stdout, stderr);

        return new CliResult(exitCode, stdout.ToString(), stderr.ToString());
    }
}
