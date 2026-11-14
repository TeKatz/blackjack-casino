namespace BlackjackCasino.Desktop;

/// <summary>Presentation preferences owned by one casino destination, independent of game state.</summary>
internal sealed record AreaPresentationSettings
{
    public bool SoundEnabled { get; init; } = true;
    public bool AnimationsEnabled { get; init; } = true;
    public bool MusicEnabled { get; init; } = true;
    public double MusicVolume { get; init; } = .10;

    internal AreaPresentationSettings Sanitize() => this with
    {
        MusicVolume = double.IsFinite(MusicVolume) ? Math.Clamp(MusicVolume, 0, .32) : .10,
    };
}

internal static class AreaPresentationPreferences
{
    internal static readonly string[] Areas = ["midnight-reels", "neon-vault", "nine-lives", "midnight-manor", "kniffel", "chill-lounge", "adventure", "casino-empire", "midnight-merge", "album", "progression", "lobby"];

    internal static AreaPresentationSettings Presentation(this AppPreferences preferences, string area) =>
        preferences.AreaPresentation is { } settings && settings.TryGetValue(area, out AreaPresentationSettings? value) && value is not null
            ? value.Sanitize()
            : new AreaPresentationSettings
            {
                SoundEnabled = preferences.SoundEnabled, AnimationsEnabled = preferences.AnimationsEnabled,
                MusicEnabled = preferences.MusicEnabled, MusicVolume = preferences.MusicVolume,
            }.Sanitize();

    internal static AppPreferences WithPresentation(this AppPreferences preferences, string area, AreaPresentationSettings value)
    {
        var next = new Dictionary<string, AreaPresentationSettings>(preferences.AreaPresentation ?? new(), StringComparer.Ordinal);
        next[area] = value.Sanitize();
        return preferences with { AreaPresentation = next };
    }

    // Snapshot the legacy global values once, before any local toggle can change them.
    // Build the dictionary once. The former WithPresentation loop cloned the growing map once
    // per area, which made every PlayerStateStore.Sanitize call allocate twelve dictionaries.
    internal static AppPreferences InitializePresentationAreas(this AppPreferences preferences)
    {
        var settings = new Dictionary<string, AreaPresentationSettings>(preferences.AreaPresentation ?? new(), StringComparer.Ordinal);
        foreach (string area in Areas) settings[area] = preferences.Presentation(area);
        return preferences with { AreaPresentation = settings };
    }
}
