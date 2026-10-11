using AzurePolicyDocs.Model;
using AzurePolicyDocs.Parsing;
using Scriban;
using Scriban.Syntax;

namespace AzurePolicyDocs.Rendering;

/// <summary>
/// Renders models with Scriban. Output always uses <c>\n</c> line endings and ends with exactly one newline,
/// so generated files diff cleanly regardless of the platform that produced them.
/// </summary>
public sealed class PolicyDocumentationRenderer
{
    private const string PolicyDefinitionTemplateResource = "Templates/PolicyDefinition.scriban";
    private const string PolicySetDefinitionTemplateResource =
        "Templates/PolicySetDefinition.scriban";
    private const string CatalogTemplateResource = "Templates/Catalog.scriban";

    private static readonly Lazy<Template> PolicyDefinitionTemplate = new(() =>
        LoadBuiltInTemplate(PolicyDefinitionTemplateResource)
    );
    private static readonly Lazy<Template> PolicySetDefinitionTemplate = new(() =>
        LoadBuiltInTemplate(PolicySetDefinitionTemplateResource)
    );
    private static readonly Lazy<Template> CatalogTemplate = new(() =>
        LoadBuiltInTemplate(CatalogTemplateResource)
    );

    /// <summary>Renders one policy or policy set definition.</summary>
    public string Render(
        PolicyDocumentationModel model,
        PolicyDocumentationGenerationOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(model);

        options ??= PolicyDocumentationGenerationOptions.Default;
        var effectiveModel = ApplyCustomValues(model, options.CustomValues);
        var (template, templatePath) =
            options.TemplateFile is { } customTemplate
                ? (ParseTemplate(ReadTemplate(customTemplate), customTemplate), customTemplate)
            : model.Kind == PolicyDocumentKind.PolicySetDefinition
                ? (PolicySetDefinitionTemplate.Value, PolicySetDefinitionTemplateResource)
            : (PolicyDefinitionTemplate.Value, PolicyDefinitionTemplateResource);

        var includeRoot =
            options.IncludeRoot
            ?? Path.GetDirectoryName(Path.GetFullPath(model.Path))
            ?? Directory.GetCurrentDirectory();
        var scriptObject = PolicyDocumentationScriptModelFactory.Create(effectiveModel);

        return RenderTemplate(template, templatePath, scriptObject, includeRoot);
    }

    /// <summary>Renders a catalog of many policies.</summary>
    public string RenderCatalog(
        PolicyCatalogModel model,
        PolicyDocumentationGenerationOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(model);

        options ??= PolicyDocumentationGenerationOptions.Default;
        var effectiveModel = options.CustomValues is { } customValues
            ? model with
            {
                Custom = MergeCustom(model.Custom, customValues),
            }
            : model;
        var (template, templatePath) = options.TemplateFile is { } customTemplate
            ? (ParseTemplate(ReadTemplate(customTemplate), customTemplate), customTemplate)
            : (CatalogTemplate.Value, CatalogTemplateResource);

        var includeRoot = options.IncludeRoot ?? Directory.GetCurrentDirectory();
        var scriptObject = PolicyDocumentationScriptModelFactory.CreateCatalog(effectiveModel);

        return RenderTemplate(template, templatePath, scriptObject, includeRoot);
    }

    /// <summary>
    /// Parses a custom template without rendering it, so a CLI can report a broken template once instead of
    /// once per input file. Throws <see cref="PolicyDocumentationException"/> when the file is missing or invalid.
    /// </summary>
    public static void ValidateTemplate(string templateFile) =>
        ParseTemplate(ReadTemplate(templateFile), templateFile);

    /// <summary>The source of a built-in template, for people who want to start a custom template from it.</summary>
    public static string GetBuiltInTemplateSource(PolicyDocumentKind kind) =>
        LoadResource(
            kind == PolicyDocumentKind.PolicySetDefinition
                ? PolicySetDefinitionTemplateResource
                : PolicyDefinitionTemplateResource
        );

    /// <summary>The source of the built-in catalog template.</summary>
    public static string GetBuiltInCatalogTemplateSource() => LoadResource(CatalogTemplateResource);

    private static string RenderTemplate(
        Template template,
        string templatePath,
        Scriban.Runtime.ScriptObject scriptObject,
        string includeRoot
    )
    {
        var context = new TemplateContext
        {
            TemplateLoader = new FileTemplateLoader(includeRoot),
            LimitToString = 0,
            LoopLimit = 100_000,
            MemberRenamer = member => member.Name,
        };
        context.PushGlobal(scriptObject);

        string rendered;
        try
        {
            rendered = template.Render(context);
        }
        catch (ScriptRuntimeException exception)
        {
            throw new PolicyDocumentationException(
                $"Failed to render the documentation template '{templatePath}': {exception.Message}",
                exception
            );
        }

        return NormalizeOutput(rendered);
    }

    private static PolicyDocumentationModel ApplyCustomValues(
        PolicyDocumentationModel model,
        IReadOnlyDictionary<string, string>? customValues
    ) =>
        customValues is null
            ? model
            : model with
            {
                Custom = MergeCustom(model.Custom, customValues),
            };

    private static System.Collections.Immutable.ImmutableSortedDictionary<
        string,
        string
    > MergeCustom(
        System.Collections.Immutable.ImmutableSortedDictionary<string, string> existing,
        IReadOnlyDictionary<string, string> overrides
    )
    {
        var builder = existing.ToBuilder();
        foreach (var (key, value) in overrides)
        {
            builder[key] = value;
        }

        return builder.ToImmutable();
    }

    private static Template LoadBuiltInTemplate(string resourceName) =>
        ParseTemplate(LoadResource(resourceName), resourceName);

    private static string LoadResource(string resourceName)
    {
        using var stream =
            typeof(PolicyDocumentationRenderer).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"The built-in template '{resourceName}' is missing from the assembly."
            );
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    private static string ReadTemplate(string templatePath)
    {
        if (!File.Exists(templatePath))
        {
            throw new PolicyDocumentationException(
                $"The template file '{templatePath}' does not exist."
            );
        }

        try
        {
            return File.ReadAllText(templatePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new PolicyDocumentationException(
                $"Unable to read template file '{templatePath}': {exception.Message}",
                exception
            );
        }
    }

    private static Template ParseTemplate(string source, string sourcePath)
    {
        var template = Template.Parse(source, sourcePath);
        if (template.HasErrors)
        {
            throw new PolicyDocumentationException(
                $"Failed to parse the documentation template '{sourcePath}':{Environment.NewLine}{string.Join(Environment.NewLine, template.Messages)}"
            );
        }

        return template;
    }

    private static string NormalizeOutput(string rendered)
    {
        var normalized = rendered.ReplaceLineEndings("\n").TrimEnd('\n');
        return normalized + "\n";
    }
}
