using System.Globalization;

namespace BlackjackCasino.Core;

public static class ProfileIdentity
{
    public const int MaximumNameLength = 24;
    public static string Validate(string? name)
    {
        string clean = string.Join(" ",(name ?? "").Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries));
        if (clean.Length == 0 || clean.Any(char.IsControl)) throw new ArgumentException("Enter a profile name.");
        if (new StringInfo(clean).LengthInTextElements > MaximumNameLength) throw new ArgumentException("Use at most 24 characters.");
        return clean;
    }
    public static string Restore(string? name)
    {
        try { return Validate(name); }
        catch (ArgumentException) { return "Player"; }
    }
}
