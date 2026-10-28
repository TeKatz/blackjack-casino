using System.Reflection;
using BlackjackCasino.Update;

namespace BlackjackCasino.Desktop;

internal static class AppVersion
{
    public static SemanticVersion Current
    {
        get
        {
            Assembly assembly = typeof(AppVersion).Assembly;
            string? informational = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            if (SemanticVersion.TryParse(informational, out SemanticVersion version))
            {
                return version;
            }

            Version fallback = assembly.GetName().Version ?? new Version(0, 0, 0);
            return new SemanticVersion(fallback.Major, fallback.Minor, Math.Max(0, fallback.Build));
        }
    }
}
