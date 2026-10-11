namespace AzurePolicyDocs.Cli;

/// <summary>Option names, in one place so messages and definitions never drift apart.</summary>
internal static class OptionNames
{
    public const string Pattern = "--pattern";
    public const string OutDir = "--outdir";
    public const string OutFile = "--outfile";
    public const string Stdout = "--stdout";
    public const string Template = "--template";
    public const string IncludeRoot = "--include-root";
    public const string CustomTemplateValue = "--custom-template-value";
    public const string CustomTemplateValueFilePath = "--custom-template-value-file-path";
    public const string DocsDir = "--docs-dir";
}
