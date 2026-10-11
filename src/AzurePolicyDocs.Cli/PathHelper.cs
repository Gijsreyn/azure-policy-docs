using System.Text;

namespace AzurePolicyDocs.Cli;

internal static class PathHelper
{
    public static readonly Encoding Utf8WithoutBom = new UTF8Encoding(
        encoderShouldEmitUTF8Identifier: false
    );

    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    public static StringComparer PathComparer =>
        PathComparison == StringComparison.OrdinalIgnoreCase
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    public static string GetFullPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new CommandLineException("A path cannot be empty.");
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception exception)
            when (exception
                    is ArgumentException
                        or NotSupportedException
                        or PathTooLongException
                        or IOException
            )
        {
            throw new CommandLineException(
                $"The path \"{path}\" is invalid: {exception.Message}",
                exception
            );
        }
    }

    public static bool PathEquals(string left, string right) =>
        string.Equals(TrimTrailingSeparators(left), TrimTrailingSeparators(right), PathComparison);

    public static bool HasExtension(string path, params string[] extensions)
    {
        var extension = Path.GetExtension(path);
        return extensions.Any(candidate =>
            string.Equals(candidate, extension, StringComparison.OrdinalIgnoreCase)
        );
    }

    /// <summary>A relative path with forward slashes, suitable for Markdown links.</summary>
    public static string GetRelativeLink(string fromDirectory, string toPath)
    {
        var relative = Path.GetRelativePath(fromDirectory, toPath);
        return relative.Replace('\\', '/');
    }

    public static void WriteFile(string path, string contents)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, contents, Utf8WithoutBom);
        }
        catch (Exception exception)
            when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            throw new CommandLineException(
                $"Unable to write \"{path}\": {exception.Message}",
                exception
            );
        }
    }

    private static string TrimTrailingSeparators(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
