namespace AzurePolicyDocs;

/// <summary>
/// Raised when a policy file cannot be parsed, a model cannot be built, or a template cannot be loaded or rendered.
/// The message is written for the person running the tool and is safe to print as-is.
/// </summary>
public sealed class PolicyDocumentationException : Exception
{
    public PolicyDocumentationException(string message)
        : base(message) { }

    public PolicyDocumentationException(string message, Exception innerException)
        : base(message, innerException) { }
}
