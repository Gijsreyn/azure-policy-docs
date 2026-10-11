using System.CommandLine;
using AzurePolicyDocs.Model;
using AzurePolicyDocs.Rendering;

namespace AzurePolicyDocs.Cli.Commands;

internal sealed record CatalogArguments(
    string Pattern,
    string? OutFile,
    bool ToStdout,
    string? DocsDir,
    string? TemplateFile,
    string? IncludeRoot,
    IReadOnlyDictionary<string, string> CustomValues
);

/// <summary>
/// <c>policydocs catalog</c>: one index page that lists every matched policy and links to its documentation.
/// </summary>
internal static class CatalogCommand
{
    private const string DefaultFileName = "README.md";

    public static Command Create(IOContext io)
    {
        var command = new Command(
            "catalog",
            "Generates one Markdown index of all policies matching a glob pattern."
        )
        {
            TreatUnmatchedTokensAsErrors = true,
        };

        var patternOption = new Option<string>(OptionNames.Pattern)
        {
            Description =
                "Glob that selects the policy files to list, for example \"Definitions/**/*.json\".",
            Required = true,
        };
        var outFileOption = new Option<string?>(OptionNames.OutFile)
        {
            Description =
                $"Where to write the catalog. Defaults to {DefaultFileName} in the pattern root.",
        };
        var stdoutOption = new Option<bool>(OptionNames.Stdout)
        {
            Description = "Prints the catalog to stdout and writes no file.",
        };
        var docsDirOption = new Option<string?>(OptionNames.DocsDir)
        {
            Description =
                $"Directory where 'generate {OptionNames.OutDir}' wrote the per-policy documents; catalog links point there. Defaults to linking a .md file beside each source file.",
        };
        var shared = new SharedOptions();

        command.Add(patternOption);
        command.Add(outFileOption);
        command.Add(stdoutOption);
        command.Add(docsDirOption);
        shared.AddTo(command);

        command.SetAction(
            async (ParseResult result, CancellationToken cancellationToken) =>
            {
                try
                {
                    var arguments = new CatalogArguments(
                        Pattern: result.GetValue(patternOption)!,
                        OutFile: result.GetValue(outFileOption),
                        ToStdout: result.GetValue(stdoutOption),
                        DocsDir: result.GetValue(docsDirOption),
                        TemplateFile: shared.ResolveTemplate(result),
                        IncludeRoot: shared.ResolveIncludeRoot(result),
                        CustomValues: shared.ResolveCustomValues(result)
                    );

                    return await RunAsync(arguments, io, cancellationToken);
                }
                catch (Exception exception)
                    when (exception is CommandLineException or PolicyDocumentationException)
                {
                    await io.Error.WriteLineAsync(exception.Message);
                    return 1;
                }
            }
        );

        return command;
    }

    internal static async Task<int> RunAsync(
        CatalogArguments arguments,
        IOContext io,
        CancellationToken cancellationToken
    )
    {
        if (arguments.ToStdout && arguments.OutFile is not null)
        {
            throw new CommandLineException(
                $"The {OptionNames.OutFile} and {OptionNames.Stdout} parameters cannot both be used."
            );
        }

        var inputs = InputResolver.ResolvePattern(arguments.Pattern);
        var patternRoot =
            inputs.Count > 0
                ? inputs[0].RootDirectory
                : InputResolver.SplitOnWildcard(arguments.Pattern).Root;
        var outputFile = arguments.ToStdout
            ? null
            : PathHelper.GetFullPath(
                arguments.OutFile ?? Path.Combine(patternRoot, DefaultFileName)
            );

        if (outputFile is not null && PathHelper.HasExtension(outputFile, ".json", ".jsonc"))
        {
            throw new CommandLineException(
                $"Documentation output cannot use a policy source file extension: \"{outputFile}\"."
            );
        }

        if (arguments.TemplateFile is { } templateFile)
        {
            PolicyDocumentationRenderer.ValidateTemplate(templateFile);
        }

        var catalogDirectory = outputFile is not null
            ? Path.GetDirectoryName(outputFile) ?? patternRoot
            : Directory.GetCurrentDirectory();
        var docsDirectory = arguments.DocsDir is { } docsDir
            ? PathHelper.GetFullPath(docsDir)
            : null;

        var generator = new PolicyDocumentationGenerator();
        var entries =
            new List<(PolicyDocumentationModel Model, string Path, string? DocumentationPath)>();
        var hasErrors = false;

        foreach (var input in inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var model = generator.BuildModel(input.FullPath);
                var documentFile = docsDirectory is not null
                    ? Path.Combine(
                        docsDirectory,
                        input.RelativeDirectory,
                        Path.GetFileNameWithoutExtension(input.FullPath) + ".md"
                    )
                    : Path.ChangeExtension(input.FullPath, ".md");

                entries.Add(
                    (
                        model,
                        PathHelper.GetRelativeLink(catalogDirectory, input.FullPath),
                        PathHelper.GetRelativeLink(catalogDirectory, documentFile)
                    )
                );
            }
            catch (PolicyDocumentationException exception)
            {
                hasErrors = true;
                await io.Error.WriteLineAsync(exception.Message);
            }
        }

        var catalog = PolicyCatalogModel.Create(entries, arguments.CustomValues);
        var options = new PolicyDocumentationGenerationOptions(
            arguments.TemplateFile,
            arguments.IncludeRoot ?? catalogDirectory,
            arguments.CustomValues
        );
        var contents = generator.RenderCatalog(catalog, options);

        if (outputFile is null)
        {
            await io.Output.WriteAsync(contents);
        }
        else
        {
            PathHelper.WriteFile(outputFile, contents);
        }

        return hasErrors ? 1 : 0;
    }
}
