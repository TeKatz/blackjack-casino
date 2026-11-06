using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Shared initialization and security policy for the offline React/WebView2 pages.
/// Each caller still supplies its own exact-page trust predicate and retains its
/// process/navigation failure handlers, message parser, session IDs and receipts.
/// </summary>
internal static class ModernWebViewBootstrap
{
    internal static async Task<CoreWebView2?> InitializeAsync(
        WebView2 browser,
        Func<bool> isClosed,
        string frontendFolder,
        Func<string?, bool> trustedPage)
    {
        ArgumentNullException.ThrowIfNull(browser);
        ArgumentNullException.ThrowIfNull(isClosed);
        ArgumentException.ThrowIfNullOrWhiteSpace(frontendFolder);
        ArgumentNullException.ThrowIfNull(trustedPage);

        await browser.EnsureCoreWebView2Async();
        // Closing a game while WebView2 initializes must not resurrect a
        // browser, attach handlers or navigate away from the casino lobby.
        if (isClosed()) return null;

        CoreWebView2 core = browser.CoreWebView2
            ?? throw new InvalidOperationException("WebView2 has no initialized browser core.");

        // No browser tooling, host-object exposure, remote navigation,
        // new windows or permissions for packaged, offline game documents.
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.AreHostObjectsAllowed = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = false;
        core.Settings.IsWebMessageEnabled = true;

        core.SetVirtualHostNameToFolderMapping(
            ManorPreviewProtocol.VirtualHost,
            frontendFolder,
            CoreWebView2HostResourceAccessKind.DenyCors);

        // The page-specific predicate rejects other pages on the *same*
        // virtual host as well as remote URLs, query strings and redirects.
        core.NavigationStarting += (_, args) =>
        {
            if (!trustedPage(args.Uri)) args.Cancel = true;
        };
        core.NewWindowRequested += (_, args) => args.Handled = true;
        core.PermissionRequested += (_, args) => args.State = CoreWebView2PermissionState.Deny;
        return core;
    }
}
