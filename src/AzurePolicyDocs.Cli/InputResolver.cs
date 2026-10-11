using Microsoft.Extensions.FileSystemGlobbing;

namespace AzurePolicyDocs.Cli;

/// <summary>An input file together with the root its relative directory is measured from.</summary>
internal sealed record ResolvedInput(string FullPath, string RootDirectory)
{
    /// <summary>Directory of the input relative to the root, or empty when the input sits in the root.</summary>
    public string RelativeDirectory
    {
        get
        {
            var directory = Path.GetDirectoryName(FullPath) ?? RootDirectory;
            var relative = Path.GetRelativePath(RootDirectory, directory);
            return relative == "." ? string.Empty : relative;
        }
    }
}

/// <summary>
/// Resolves the positional input or a glob pattern to concrete files. The pattern root is the longest literal
/// directory before the first wildcard, which is also the root used to preserve relative directories under
/// an output directory.
/// </summary>
internal static class InputResolver
{
    public static ResolvedInput ResolveSingle(string inputPath)
    {
        var fullPath = PathHelper.GetFullPath(inputPath);
        if (Directory.Exists(fullPath))
        {
            throw new CommandLineException(
                $"\"{fullPath}\" is a directory. Pass a policy file, or use {OptionNames.Pattern} to select many files."
            );
        }

        if (!File.Exists(fullPath))
        {
            throw new CommandLineException($"The input file \"{fullPath}\" does not exist.");
        }

        var root = Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory();
        return new ResolvedInput(fullPath, root);
    }

    public static IReadOnlyList<ResolvedInput> ResolvePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new CommandLineException(
                $"The {OptionNames.Pattern} parameter expects a glob pattern."
            );
        }

        var (root, relativePattern) = SplitOnWildcard(pattern);
        if (!Directory.Exists(root))
        {
            throw new CommandLineException(
                $"The pattern root directory \"{root}\" does not exist."
            );
        }

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude(relativePattern);

        return matcher
            .GetResultsInFullPath(root)
            .Select(Path.GetFullPath)
            .Order(StringComparer.Ordinal)
            .Select(path => new ResolvedInput(path, root))
            .ToArray();
    }

    internal static (string Root, string RelativePattern) SplitOnWildcard(string pattern)
    {
        var patternSpan = pattern.AsSpan();
        var wildcardIndex = patternSpan.IndexOfAny('*', '?');
        if (wildcardIndex == -1)
        {
            wildcardIndex = pattern.Length;
        }

        var lastSeparator = patternSpan[..wildcardIndex].LastIndexOfAny('/', '\\');
        // Retain a leading separator so a filesystem-root glob stays rooted.
        var root = lastSeparator switch
        {
            -1 => string.Empty,
            0 => pattern[..1],
            _ => pattern[..lastSeparator],
        };
        var relativePattern = lastSeparator != -1 ? pattern[(lastSeparator + 1)..] : pattern;

        if (root.Length == 0)
        {
            root = Directory.GetCurrentDirectory();
        }
        else if (root.EndsWith(':'))
        {
            // "C:" alone would resolve against the drive's current directory; the user meant the drive root.
            root += Path.DirectorySeparatorChar;
        }

        return (PathHelper.GetFullPath(root), relativePattern);
    }
}
