namespace BlackjackCasino.Desktop;

internal sealed record CasinoMode(string Id,string Title,string Subtitle,string Description,string Accent,string Emblem,string Category,Func<System.Windows.FrameworkElement>? Artwork=null);

/// <summary>Composition root registers modes; lobby rendering contains no mode-ID switch.</summary>
internal sealed class CasinoModeRegistry
{
    private readonly Dictionary<string,(CasinoMode Mode,Action Open)> entries = new(StringComparer.Ordinal);
    internal event Action? Changed;
    internal IReadOnlyList<CasinoMode> Modes => entries.Values.Select(x=>x.Mode).ToArray();
    internal void Register(CasinoMode mode,Action open)
    {
        ArgumentNullException.ThrowIfNull(open);
        if(string.IsNullOrWhiteSpace(mode.Id) || mode.Id.Any(c=>!char.IsAsciiLetterOrDigit(c) && c!='-' && c!='_') || !entries.TryAdd(mode.Id,(mode,open))) throw new ArgumentException("Mode IDs must be nonempty and unique.");
        Changed?.Invoke();
    }
    internal void Open(string id) => entries[id].Open();
}
