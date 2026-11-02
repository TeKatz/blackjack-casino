namespace BlackjackCasino.Desktop;

internal static class AdventureCaseBoardCollectionExtensions
{
    internal static int IndexOf<T>(this IReadOnlyList<T> items, T value)
    {
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        for (int index = 0; index < items.Count; index++)
        {
            if (comparer.Equals(items[index], value)) return index;
        }
        return -1;
    }
}
