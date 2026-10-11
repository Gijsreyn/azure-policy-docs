namespace AzurePolicyDocs.Rendering;

/// <summary>
/// Controls rendering. Without a <see cref="TemplateFile"/> the built-in Markdown template for the document kind is used.
/// </summary>
/// <param name="TemplateFile">Full path of a custom Scriban template, or null for the built-in template.</param>
/// <param name="IncludeRoot">Directory that Scriban <c>include</c> paths resolve against. Defaults to the input file's directory.</param>
/// <param name="CustomValues">Values exposed to templates as <c>custom.&lt;key&gt;</c>. Merged over values already on the model.</param>
public sealed record PolicyDocumentationGenerationOptions(
    string? TemplateFile = null,
    string? IncludeRoot = null,
    IReadOnlyDictionary<string, string>? CustomValues = null
)
{
    public static PolicyDocumentationGenerationOptions Default { get; } = new();
}
