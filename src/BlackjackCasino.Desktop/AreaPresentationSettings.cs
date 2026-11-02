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
    internal static AppPreferences InitializePresentationAreas(this AppPreferences preferences)
    {
        var next = preferences;
        foreach (string area in Areas) next = next.WithPresentation(area, preferences.Presentation(area));
        return next;
    }
}
