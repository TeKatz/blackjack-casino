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
