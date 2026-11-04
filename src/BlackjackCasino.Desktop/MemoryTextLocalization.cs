using BlackjackCasino.Core.Memory;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Display-only localization. Every deck and symbol retains its stable game/save ID.
/// </summary>
internal static class MemoryTextLocalization
{
    private static string Lookup(string key, string original)
    {
        if (Localization.Language != UiLanguagePreference.German) return original;
        string result = Localization.T(key);
        return result.StartsWith("⟦", StringComparison.Ordinal) ? original : result;
    }

    internal static MemoryDeckDefinition Deck(MemoryDeckDefinition original) =>
        Localization.Language == UiLanguagePreference.German
            ? original with
            {
                Name = Lookup($"Memory.Deck.{original.Id}.Name", original.Name),
                Description = Lookup($"Memory.Deck.{original.Id}.Description", original.Description),
                Symbols = original.Symbols.Select(symbol => symbol with
                {
                    Name = Lookup($"Memory.Symbol.{symbol.Id}.Name", symbol.Name),
                }).ToArray(),
            }
            : original;

    internal static MemorySymbol Symbol(MemorySymbol original) =>
        original with { Name = Lookup($"Memory.Symbol.{original.Id}.Name", original.Name) };

    internal static MemoryDeckDefinition[] Decks() =>
        MemoryCatalog.Decks.Select(Deck).ToArray();
}
