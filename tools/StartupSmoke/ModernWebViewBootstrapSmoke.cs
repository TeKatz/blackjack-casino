using System.IO;
using Microsoft.Web.WebView2.Core;

/// <summary>
/// Inspect real initialized browser instances, not mocked policy flags.
/// Called from packaged lobby and six-activity Lounge startup smokes.
/// </summary>
internal static class ModernWebViewBootstrapSmoke
{
    internal static void RequireHardened(CoreWebView2 core, string page)
    {
        ArgumentNullException.ThrowIfNull(core);
        string expectedFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlackjackCasino", "WebView2");
        string actualFolder = Path.GetFullPath(core.Environment.UserDataFolder);
        if (!string.Equals(
            Path.TrimEndingDirectorySeparator(actualFolder),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(expectedFolder)),
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                page + " uses a WebView2 profile inside the replaceable application folder: " + actualFolder);
        }

        var settings = core.Settings;
        if (settings.AreDefaultContextMenusEnabled
            || settings.AreDevToolsEnabled
            || settings.AreHostObjectsAllowed
            || settings.IsStatusBarEnabled
            || settings.IsZoomControlEnabled
            || !settings.IsWebMessageEnabled)
        {
            throw new InvalidOperationException(
                page + " did not receive the shared offline WebView2 security policy.");
        }
    }
}
