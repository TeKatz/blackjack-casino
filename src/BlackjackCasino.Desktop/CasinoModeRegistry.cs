namespace BlackjackCasino.Desktop;

internal sealed record CasinoMode(string Id,string Title,string Subtitle,string Description,string Accent,string Emblem,string Category,Func<System.Windows.FrameworkElement>? Artwork=null);

/// <summary>Composition root registers modes; lobby rendering stays generic for primary and optional secondary actions.</summary>
internal sealed class CasinoModeRegistry
{
    private sealed record Entry(
        CasinoMode Mode,
        Action Open,
        Func<bool>? CanOpen,
        Action? SecondaryOpen,
        string? SecondaryLabel,
        Func<bool>? CanSecondaryOpen);

    private readonly Dictionary<string,Entry> entries = new(StringComparer.Ordinal);
    internal event Action? Changed;
    internal IReadOnlyList<CasinoMode> Modes => entries.Values.Select(x=>x.Mode).ToArray();

    internal void Register(
        CasinoMode mode,
        Action open,
        Func<bool>? canOpen = null,
        Action? secondaryOpen = null,
        string? secondaryLabel = null,
        Func<bool>? canSecondaryOpen = null)
    {
        ArgumentNullException.ThrowIfNull(open);
        if (secondaryOpen is null != string.IsNullOrWhiteSpace(secondaryLabel))
            throw new ArgumentException("A secondary lobby action requires both an action and a label.");
        if(string.IsNullOrWhiteSpace(mode.Id) || mode.Id.Any(c=>!char.IsAsciiLetterOrDigit(c) && c!='-' && c!='_')
            || !entries.TryAdd(mode.Id,new Entry(mode,open,canOpen,secondaryOpen,secondaryLabel,canSecondaryOpen)))
            throw new ArgumentException("Mode IDs must be nonempty and unique.");
        Changed?.Invoke();
    }

    internal bool CanOpen(string id) => entries.TryGetValue(id, out Entry? entry) && (entry.CanOpen?.Invoke() ?? true);
    internal void Open(string id)
    {
        if (CanOpen(id)) entries[id].Open();
    }

    internal bool HasSecondary(string id) => entries.TryGetValue(id, out Entry? entry) && entry.SecondaryOpen is not null;
    internal string? SecondaryLabel(string id) => entries.TryGetValue(id, out Entry? entry) ? entry.SecondaryLabel : null;
    internal bool CanOpenSecondary(string id)
        => entries.TryGetValue(id, out Entry? entry) && entry.SecondaryOpen is not null && (entry.CanSecondaryOpen?.Invoke() ?? true);
    internal void OpenSecondary(string id)
    {
        if (CanOpenSecondary(id)) entries[id].SecondaryOpen!.Invoke();
    }

    internal void NotifyStateChanged() => Changed?.Invoke();
}
