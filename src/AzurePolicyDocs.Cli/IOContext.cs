namespace AzurePolicyDocs.Cli;

/// <summary>Where a command writes. Tests pass string writers; <c>Main</c> passes the console.</summary>
internal sealed record IOContext(TextWriter Output, TextWriter Error);
