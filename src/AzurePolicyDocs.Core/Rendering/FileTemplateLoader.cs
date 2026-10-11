using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;

namespace AzurePolicyDocs.Rendering;

/// <summary>
/// Resolves Scriban <c>include</c> names against one root directory, so templates can share fragments such as
/// a header or footer.
/// </summary>
internal sealed class FileTemplateLoader(string root) : ITemplateLoader
{
    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
    {
        try
        {
            return Path.GetFullPath(Path.Combine(root, templateName));
        }
        catch (Exception exception)
            when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new ScriptRuntimeException(
                callerSpan,
                $"Unable to resolve include path '{templateName}': {exception.Message}",
                exception
            );
        }
    }

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        if (!File.Exists(templatePath))
        {
            throw new ScriptRuntimeException(
                callerSpan,
                $"The include file '{templatePath}' does not exist."
            );
        }

        try
        {
            return File.ReadAllText(templatePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new ScriptRuntimeException(
                callerSpan,
                $"Unable to read include file '{templatePath}': {exception.Message}",
                exception
            );
        }
    }

    public ValueTask<string?> LoadAsync(
        TemplateContext context,
        SourceSpan callerSpan,
        string templatePath
    ) => new(Load(context, callerSpan, templatePath));
}
