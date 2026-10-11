using System.CommandLine;
using AzurePolicyDocs.Cli.Commands;

namespace AzurePolicyDocs.Cli;

/// <summary>Entry point of the <c>azurepolicydocs</c> tool.</summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = PathHelper.Utf8WithoutBom;
        }
        catch (IOException)
        {
            // No console attached; the default encoding is fine.
        }

        return await RunAsync(args, Console.Out, Console.Error, CancellationToken.None);
    }

    /// <summary>Runs the tool with explicit writers. Tests use this to capture output in-process.</summary>
    public static async Task<int> RunAsync(
        string[] args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken = default
    )
    {
        var io = new IOContext(output, error);
        var rootCommand = new RootCommand(
            "Generates Markdown documentation for Azure Policy definitions and policy set definitions from their JSON."
        );
        rootCommand.Add(GenerateCommand.Create(io));
        rootCommand.Add(CatalogCommand.Create(io));
        rootCommand.Add(TemplateCommand.Create(io));

        var configuration = new InvocationConfiguration { Output = output, Error = error };

        return await rootCommand.Parse(args).InvokeAsync(configuration, cancellationToken);
    }
}
