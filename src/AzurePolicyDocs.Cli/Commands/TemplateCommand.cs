using System.CommandLine;
using AzurePolicyDocs.Parsing;
using AzurePolicyDocs.Rendering;

namespace AzurePolicyDocs.Cli.Commands;

/// <summary>
/// <c>azurepolicydocs template export</c>: prints a built-in template, the natural starting point for a custom one.
/// </summary>
internal static class TemplateCommand
{
    private static readonly string[] Kinds = ["policyDefinition", "policySetDefinition", "catalog"];

    public static Command Create(IOContext io)
    {
        var command = new Command("template", "Works with the built-in Scriban templates.");
        command.Add(CreateExport(io));
        return command;
    }

    private static Command CreateExport(IOContext io)
    {
        var command = new Command(
            "export",
            "Prints a built-in template so you can copy and adapt it."
        )
        {
            TreatUnmatchedTokensAsErrors = true,
        };

        var kindOption = new Option<string>("--kind")
        {
            Description = $"Which template to export: {string.Join(", ", Kinds)}.",
            Required = true,
        };
        kindOption.AcceptOnlyFromAmong(Kinds);

        var outFileOption = new Option<string?>(OptionNames.OutFile)
        {
            Description = "Writes the template to this path instead of stdout.",
        };

        command.Add(kindOption);
        command.Add(outFileOption);

        command.SetAction(
            async (ParseResult result, CancellationToken cancellationToken) =>
            {
                try
                {
                    var kind = result.GetValue(kindOption)!;
                    var source = kind switch
                    {
                        "policySetDefinition" =>
                            PolicyDocumentationRenderer.GetBuiltInTemplateSource(
                                PolicyDocumentKind.PolicySetDefinition
                            ),
                        "catalog" => PolicyDocumentationRenderer.GetBuiltInCatalogTemplateSource(),
                        _ => PolicyDocumentationRenderer.GetBuiltInTemplateSource(
                            PolicyDocumentKind.PolicyDefinition
                        ),
                    };

                    if (result.GetValue(outFileOption) is { } outFile)
                    {
                        PathHelper.WriteFile(PathHelper.GetFullPath(outFile), source);
                    }
                    else
                    {
                        await io.Output.WriteAsync(source);
                    }

                    return 0;
                }
                catch (CommandLineException exception)
                {
                    await io.Error.WriteLineAsync(exception.Message);
                    return 1;
                }
            }
        );

        return command;
    }
}
