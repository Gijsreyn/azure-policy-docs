using System.CommandLine;
using AzurePolicyDocs.Rendering;

namespace AzurePolicyDocs.Cli.Commands;

internal sealed record GenerateArguments(
    string? InputFile,
    string? Pattern,
    string? OutDir,
    string? OutFile,
    bool ToStdout,
    string? TemplateFile,
    string? IncludeRoot,
    IReadOnlyDictionary<string, string> CustomValues
);

/// <summary>
/// <c>azurepolicydocs generate</c>: one Markdown file per policy definition or policy set definition.
/// </summary>
internal static class GenerateCommand
{
    private const string MarkdownExtension = ".md";

    public static Command Create(IOContext io)
    {
        var command = new Command(
            "generate",
            "Generates a Markdown document for each policy definition or policy set definition."
        )
        {
            TreatUnmatchedTokensAsErrors = true,
        };

        var inputArgument = new Argument<string?>("inputFile")
        {
            Description =
                "Path to a policy definition or policy set definition file (.json or .jsonc).",
            Arity = ArgumentArity.ZeroOrOne,
        };
        var patternOption = new Option<string?>(OptionNames.Pattern)
        {
            Description =
                "Generates documentation for every file matching the glob, for example \"Definitions/**/*.json\". Cannot be combined with an input file.",
        };
        var outDirOption = new Option<string?>(OptionNames.OutDir)
        {
            Description =
                "Writes generated files beneath this directory. With --pattern, the directory structure under the pattern root is preserved.",
        };
        var outFileOption = new Option<string?>(OptionNames.OutFile)
        {
            Description = "Writes the generated document to this exact path. Single input only.",
        };
        var stdoutOption = new Option<bool>(OptionNames.Stdout)
        {
            Description =
                "Prints the generated document to stdout and writes no file. Single input only.",
        };
        var shared = new SharedOptions();

        command.Add(inputArgument);
        command.Add(patternOption);
        command.Add(outDirOption);
        command.Add(outFileOption);
        command.Add(stdoutOption);
        shared.AddTo(command);

        command.Validators.Add(result =>
        {
            var input = result.GetValue(inputArgument);
            if (input is not null && input.StartsWith("--", StringComparison.Ordinal))
            {
                result.AddError($"Unrecognized parameter \"{input}\"");
            }

            if (input is not null && result.GetValue(patternOption) is not null)
            {
                result.AddError(
                    $"The input file and the {OptionNames.Pattern} parameter cannot both be specified."
                );
            }
        });

        command.SetAction(
            async (ParseResult result, CancellationToken cancellationToken) =>
            {
                try
                {
                    var arguments = new GenerateArguments(
                        InputFile: result.GetValue(inputArgument),
                        Pattern: result.GetValue(patternOption),
                        OutDir: result.GetValue(outDirOption),
                        OutFile: result.GetValue(outFileOption),
                        ToStdout: result.GetValue(stdoutOption),
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
        GenerateArguments arguments,
        IOContext io,
        CancellationToken cancellationToken
    )
    {
        ValidateOutputOptions(arguments);
        var inputs = ResolveInputs(arguments);
        if (inputs.Count == 0)
        {
            await io.Error.WriteLineAsync($"No files matched the pattern \"{arguments.Pattern}\".");
            return 0;
        }

        if (arguments.TemplateFile is { } templateFile)
        {
            // Fail once, up front, instead of once per input.
            PolicyDocumentationRenderer.ValidateTemplate(templateFile);
        }

        var options = new PolicyDocumentationGenerationOptions(
            arguments.TemplateFile,
            arguments.IncludeRoot,
            arguments.CustomValues
        );
        var generator = new PolicyDocumentationGenerator();
        var rendered = new List<(ResolvedInput Input, string Contents)>();
        var hasErrors = false;

        foreach (var input in inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                rendered.Add((input, generator.Generate(input.FullPath, options)));
            }
            catch (PolicyDocumentationException exception)
            {
                hasErrors = true;
                await io.Error.WriteLineAsync(exception.Message);
            }
        }

        if (arguments.ToStdout)
        {
            if (rendered.Count == 1)
            {
                await io.Output.WriteAsync(rendered[0].Contents);
            }
        }
        else if (rendered.Count > 0)
        {
            // Resolve and validate every destination before the first write, so a bad combination never
            // leaves a half-written set of files behind.
            var outputs = ResolveOutputs(arguments, rendered.Select(entry => entry.Input).ToList());
            ValidateOutputPaths(outputs);

            foreach (var (input, contents) in rendered)
            {
                PathHelper.WriteFile(outputs[input.FullPath], contents);
            }
        }

        return hasErrors ? 1 : 0;
    }

    private static void ValidateOutputOptions(GenerateArguments arguments)
    {
        if (arguments.Pattern is not null && arguments.ToStdout)
        {
            throw new CommandLineException(
                $"The {OptionNames.Stdout} parameter cannot be used with the {OptionNames.Pattern} parameter."
            );
        }

        if (arguments.Pattern is not null && arguments.OutFile is not null)
        {
            throw new CommandLineException(
                $"The {OptionNames.OutFile} parameter cannot be used with the {OptionNames.Pattern} parameter."
            );
        }

        if (arguments.ToStdout && arguments.OutDir is not null)
        {
            throw new CommandLineException(
                $"The {OptionNames.OutDir} and {OptionNames.Stdout} parameters cannot both be used."
            );
        }

        if (arguments.ToStdout && arguments.OutFile is not null)
        {
            throw new CommandLineException(
                $"The {OptionNames.OutFile} and {OptionNames.Stdout} parameters cannot both be used."
            );
        }

        if (arguments.OutDir is not null && arguments.OutFile is not null)
        {
            throw new CommandLineException(
                $"The {OptionNames.OutDir} and {OptionNames.OutFile} parameters cannot both be used."
            );
        }
    }

    private static IReadOnlyList<ResolvedInput> ResolveInputs(GenerateArguments arguments)
    {
        if (arguments.InputFile is { } inputFile)
        {
            return [InputResolver.ResolveSingle(inputFile)];
        }

        if (arguments.Pattern is { } pattern)
        {
            return InputResolver.ResolvePattern(pattern);
        }

        throw new CommandLineException(
            $"Either the input file path or the {OptionNames.Pattern} parameter must be specified."
        );
    }

    private static Dictionary<string, string> ResolveOutputs(
        GenerateArguments arguments,
        IReadOnlyList<ResolvedInput> inputs
    )
    {
        var outputs = new Dictionary<string, string>(PathHelper.PathComparer);
        var outDir = arguments.OutDir is { } directory ? PathHelper.GetFullPath(directory) : null;

        foreach (var input in inputs)
        {
            var fileName = Path.GetFileNameWithoutExtension(input.FullPath) + MarkdownExtension;
            string output;
            if (arguments.OutFile is { } outFile)
            {
                output = PathHelper.GetFullPath(outFile);
            }
            else if (outDir is not null)
            {
                output = arguments.Pattern is not null
                    ? Path.Combine(outDir, input.RelativeDirectory, fileName)
                    : Path.Combine(outDir, fileName);
            }
            else
            {
                output = Path.Combine(
                    Path.GetDirectoryName(input.FullPath) ?? input.RootDirectory,
                    fileName
                );
            }

            outputs[input.FullPath] = Path.GetFullPath(output);
        }

        return outputs;
    }

    private static void ValidateOutputPaths(Dictionary<string, string> outputs)
    {
        var seen = new HashSet<string>(PathHelper.PathComparer);
        foreach (var (input, output) in outputs)
        {
            if (PathHelper.PathEquals(input, output))
            {
                throw new CommandLineException(
                    $"The documentation output path cannot overwrite the input file \"{input}\"."
                );
            }

            if (PathHelper.HasExtension(output, ".json", ".jsonc"))
            {
                throw new CommandLineException(
                    $"Documentation output cannot use a policy source file extension: \"{output}\"."
                );
            }

            if (!seen.Add(output))
            {
                throw new CommandLineException(
                    $"Multiple input files resolve to the output file \"{output}\". Use {OptionNames.OutDir} with a pattern that keeps their directories apart."
                );
            }
        }
    }
}
