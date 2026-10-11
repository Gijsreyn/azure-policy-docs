namespace AzurePolicyDocs.Cli;

/// <summary>
/// A user-facing argument or input/output problem. The message is printed to stderr and the command exits with 1.
/// </summary>
internal sealed class CommandLineException : Exception
{
    public CommandLineException(string message)
        : base(message) { }

    public CommandLineException(string message, Exception innerException)
        : base(message, innerException) { }
}
