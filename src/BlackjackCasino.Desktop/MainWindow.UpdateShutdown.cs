using System.Windows;
using System.Windows.Media;
using Microsoft.Web.WebView2.Wpf;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    // WebView2 is an HwndHost: closing WPF windows does not guarantee that all
    // browser controllers are explicitly disposed before the updater observes
    // the parent process exit. This path is used only after successfully
    // starting the external updater, immediately before shutdown.
    private static void ReleaseBrowserHostsForUpdate()
    {
        var browsers = new List<WebView2>();
        foreach (Window window in Application.Current.Windows.Cast<Window>().ToArray())
        {
            try
            {
                CollectBrowserHosts(window, browsers);
            }
            catch (Exception)
            {
                // A detached or already closing window may have a disappearing
                // visual tree. Nothing here must block application shutdown.
            }
        }

        foreach (WebView2 browser in browsers.Distinct())
        {
            try { browser.Dispose(); }
            catch (Exception)
            {
                // The parent process is exiting regardless. The updater will
                // continue to wait for the actual parent PID to terminate.
            }
        }
    }

    private static void CollectBrowserHosts(DependencyObject node, List<WebView2> browsers)
    {
        if (node is WebView2 browser)
        {
            browsers.Add(browser);
            return;
        }

        int count = VisualTreeHelper.GetChildrenCount(node);
        for (int i = 0; i < count; i++)
            CollectBrowserHosts(VisualTreeHelper.GetChild(node, i), browsers);
    }
}
