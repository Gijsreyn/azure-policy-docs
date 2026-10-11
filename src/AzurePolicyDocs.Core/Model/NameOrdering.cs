using System.Collections.Immutable;

namespace AzurePolicyDocs.Model;

/// <summary>
/// Case-insensitive ordering with an ordinal tie-break, so output is deterministic across platforms and cultures.
/// </summary>
internal static class NameOrdering
{
    public static readonly IComparer<string> Comparer = Comparer<string>.Create(Compare);

    public static ImmutableArray<T> SortByName<T>(
        IEnumerable<T> items,
        Func<T, string> nameSelector
    ) => [.. items.OrderBy(nameSelector, Comparer)];

    private static int Compare(string left, string right)
    {
        var caseInsensitive = string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        return caseInsensitive != 0 ? caseInsensitive : string.CompareOrdinal(left, right);
    }
}
