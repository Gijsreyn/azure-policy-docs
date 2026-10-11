using System.CommandLine;

namespace AzurePolicyDocs.Cli.Commands;

/// <summary>Options that generate and catalog share, created once per command so each command owns its instances.</summary>
internal sealed class SharedOptions
{
    public Option<string?> Template { get; } =
        new(OptionNames.Template)
        {
            Description =
                "Path to a custom Scriban template. Defaults to the built-in Markdown template.",
        };

    public Option<string?> IncludeRoot { get; } =
        new(OptionNames.IncludeRoot)
        {
            Description =
                "Directory that Scriban include paths resolve against. Defaults to the directory of the input file (generate) or of the output file (catalog).",
        };

    public Option<string[]> CustomValue { get; } =
        new(OptionNames.CustomTemplateValue)
        {
            Description =
                "Supplies a custom template value as key=value, available to templates as custom.<key>. May be repeated.",
            Arity = ArgumentArity.ZeroOrMore,
            AllowMultipleArgumentsPerToken = false,
        };

    public Option<string[]> CustomValueFile { get; } =
        new(OptionNames.CustomTemplateValueFilePath)
        {
            Description =
                "Loads custom template values from a JSON object whose values are strings. May be repeated.",
            Arity = ArgumentArity.ZeroOrMore,
            AllowMultipleArgumentsPerToken = false,
        };

    public void AddTo(Command command)
    {
        command.Add(Template);
        command.Add(IncludeRoot);
        command.Add(CustomValue);
        command.Add(CustomValueFile);
    }

    public string? ResolveTemplate(ParseResult result)
    {
        if (result.GetValue(Template) is not { } template)
        {
            return null;
        }

        var fullPath = PathHelper.GetFullPath(template);
        if (!File.Exists(fullPath))
        {
            throw new CommandLineException($"The template file \"{fullPath}\" does not exist.");
        }

        return fullPath;
    }

    public string? ResolveIncludeRoot(ParseResult result)
    {
        if (result.GetValue(IncludeRoot) is not { } includeRoot)
        {
            return null;
        }

        var fullPath = PathHelper.GetFullPath(includeRoot);
        if (!Directory.Exists(fullPath))
        {
            throw new CommandLineException(
                $"The template include root directory \"{fullPath}\" does not exist."
            );
        }

        return fullPath;
    }

    public IReadOnlyDictionary<string, string> ResolveCustomValues(ParseResult result) =>
        CustomValuesParser.Parse(result, CustomValue, CustomValueFile);
}
