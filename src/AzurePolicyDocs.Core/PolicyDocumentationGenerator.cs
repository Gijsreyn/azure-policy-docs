using AzurePolicyDocs.Model;
using AzurePolicyDocs.Parsing;
using AzurePolicyDocs.Rendering;

namespace AzurePolicyDocs;

/// <summary>
/// The one-stop entry point: parse a file, build its model, render it. Use the pieces directly when you need
/// the model without rendering, for example to feed another output format.
/// </summary>
public sealed class PolicyDocumentationGenerator
{
    private readonly PolicyDocumentationRenderer renderer = new();

    /// <summary>Parses the file and builds its model.</summary>
    public PolicyDocumentationModel BuildModel(
        string path,
        IReadOnlyDictionary<string, string>? customValues = null
    ) => PolicyDocumentationModelBuilder.Build(PolicyDocumentParser.ParseFile(path), customValues);

    /// <summary>Parses JSON text and builds its model. <paramref name="path"/> is used in messages and as the name fallback.</summary>
    public PolicyDocumentationModel BuildModel(
        string json,
        string path,
        IReadOnlyDictionary<string, string>? customValues = null
    ) =>
        PolicyDocumentationModelBuilder.Build(PolicyDocumentParser.Parse(json, path), customValues);

    /// <summary>Renders a model to Markdown (or whatever a custom template produces).</summary>
    public string Render(
        PolicyDocumentationModel model,
        PolicyDocumentationGenerationOptions? options = null
    ) => renderer.Render(model, options);

    /// <summary>Parses, builds, and renders the file at <paramref name="path"/>.</summary>
    public string Generate(string path, PolicyDocumentationGenerationOptions? options = null) =>
        Render(BuildModel(path, options?.CustomValues), options);

    /// <summary>Renders a catalog model.</summary>
    public string RenderCatalog(
        PolicyCatalogModel catalog,
        PolicyDocumentationGenerationOptions? options = null
    ) => renderer.RenderCatalog(catalog, options);
}
