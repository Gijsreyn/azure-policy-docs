using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.Json;

namespace AzurePolicyDocs.Cli;

/// <summary>
/// Collects <c>--custom-template-value key=value</c> and <c>--custom-template-value-file-path file.json</c> in
/// command-line order, so the last occurrence of a key wins whichever form supplied it.
/// </summary>
internal static class CustomValuesParser
{
    public static IReadOnlyDictionary<string, string> Parse(
        ParseResult result,
        Option<string[]> valueOption,
        Option<string[]> fileOption
    )
    {
        var values = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var tokens = result.Tokens;

        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];
            if (token.Type != TokenType.Option)
            {
                continue;
            }

            if (Matches(token.Value, valueOption))
            {
                SetValue(values, NextArgument(tokens, ref index, OptionNames.CustomTemplateValue));
            }
            else if (Matches(token.Value, fileOption))
            {
                LoadFile(
                    values,
                    NextArgument(tokens, ref index, OptionNames.CustomTemplateValueFilePath)
                );
            }
        }

        return values;
    }

    private static bool Matches(string tokenValue, Option option) =>
        string.Equals(tokenValue, option.Name, StringComparison.Ordinal)
        || option.Aliases.Any(alias => string.Equals(tokenValue, alias, StringComparison.Ordinal));

    private static string NextArgument(
        IReadOnlyList<Token> tokens,
        ref int index,
        string optionName
    )
    {
        if (index + 1 >= tokens.Count || tokens[index + 1].Type != TokenType.Argument)
        {
            throw new CommandLineException($"The {optionName} parameter expects an argument.");
        }

        return tokens[++index].Value;
    }

    private static void SetValue(SortedDictionary<string, string> values, string pair)
    {
        var separator = pair.IndexOf('=');
        if (separator <= 0)
        {
            throw new CommandLineException(
                $"The {OptionNames.CustomTemplateValue} value \"{pair}\" must use the format key=value."
            );
        }

        values[pair[..separator]] = pair[(separator + 1)..];
    }

    private static void LoadFile(SortedDictionary<string, string> values, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new CommandLineException(
                $"The {OptionNames.CustomTemplateValueFilePath} parameter expects a nonempty path."
            );
        }

        var fullPath = PathHelper.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new CommandLineException(
                $"The custom template value file \"{fullPath}\" does not exist."
            );
        }

        try
        {
            using var document = JsonDocument.Parse(
                File.ReadAllText(fullPath),
                new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                }
            );

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new CommandLineException(
                    $"The custom template value file \"{fullPath}\" must contain a JSON object."
                );
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Name.Length == 0)
                {
                    throw new CommandLineException(
                        $"The custom template value file \"{fullPath}\" contains an empty key."
                    );
                }

                if (!seen.Add(property.Name))
                {
                    throw new CommandLineException(
                        $"The custom template value file \"{fullPath}\" contains the duplicate key \"{property.Name}\"."
                    );
                }

                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    throw new CommandLineException(
                        $"The custom template value file \"{fullPath}\" value for \"{property.Name}\" must be a string."
                    );
                }

                values[property.Name] = property.Value.GetString() ?? string.Empty;
            }
        }
        catch (JsonException exception)
        {
            throw new CommandLineException(
                $"The custom template value file \"{fullPath}\" is not valid JSON: {exception.Message}",
                exception
            );
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new CommandLineException(
                $"Unable to read custom template value file \"{fullPath}\": {exception.Message}",
                exception
            );
        }
    }
}
