using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Desktop;
using Microsoft.Web.WebView2.Core;

internal static class VaultSmoke
{
    internal static async Task Run(Window window, string output)
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            // Reproduce the longer currency strings from the user's German desktop. This is the
            // layout that previously clipped the leading V in the full-vault bonus line.
            CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentCulture = german;
            CultureInfo.CurrentUICulture = german;

            int visibleWindowsBefore = Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible);
            ModernLobbyView? modern = Find<ModernLobbyView>(window)
                .FirstOrDefault(view => view.Ready && view.IsVisible);
            if (modern is not null)
            {
                // The Vault is a native-authoritative React page. The old WPF modal
                // remains available only for fallback and table-mode navigation.
                CoreWebView2 web = modern.Browser.CoreWebView2!;
                // The WebView2 message handler may run in a different async
                // ExecutionContext than this test. Switch the application locale
                // itself so the UI and C# payout formatter are both German.
                bool startedGerman = await web.ExecuteScriptAsync(
                    "document.documentElement.lang==='de'") == "true";
                if (!startedGerman)
                {
                    await web.ExecuteScriptAsync("document.querySelector('.lobby-language')?.click()");
                    bool germanReady = false;
                    for (int i=0;i<100 && !germanReady;i++)
                    {
                        germanReady = await web.ExecuteScriptAsync(
                            "document.documentElement.lang==='de'") == "true";
                        if (!germanReady) await Task.Delay(50);
                    }
                    Require(germanReady,"React Vault smoke did not activate the persisted German casino locale.");
                }
                string link = await web.ExecuteScriptAsync(
                    "Boolean([...document.querySelectorAll('.lobby-nav-item')]"
                    + ".find(x=>/Vault|Tresor/.test(x.textContent||'')))");
                Require(link == "true", "React lobby is missing the Vault navigation entry.");
                await web.ExecuteScriptAsync(
                    "[...document.querySelectorAll('.lobby-nav-item')]"
                    + ".find(x=>/Vault|Tresor/.test(x.textContent||''))?.click()");
                for (int i = 0; i < 100 && modern.RecordsPage != "vault"; i++) await Task.Delay(60);
                Require(modern.RecordsPage == "vault", "Native Vault route did not activate React.");
                for (int i = 0; i < 100; i++)
                {
                    if (await web.ExecuteScriptAsync(
                        "Boolean(document.querySelector('.vault-page .vault-break:not(:disabled)'))") == "true") break;
                    await Task.Delay(60);
                }
                Require(await web.ExecuteScriptAsync(
                    "Boolean(document.querySelector('.vault-page .vault-break:not(:disabled)'))") == "true",
                    "A full Gold Vault is not ready to open in React.");
                Require(!Find<Grid>(window).Any(g => g.Name == "CasinoVaultOverlay" && g.IsVisible),
                    "The old WPF Vault was opened over the modern presentation.");
                await using (FileStream beforeShot = File.Create(Path.Combine(output, "casino-vault-in-app.png")))
                    await web.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, beforeShot);
                await web.ExecuteScriptAsync("document.querySelector('.vault-break')?.click()");
                bool rewarded = false;
                for (int i = 0; i < 100; i++)
                {
                    rewarded = await web.ExecuteScriptAsync(
                        "Boolean(document.querySelector('.vault-reward .vault-total strong')"
                        + "?.textContent?.includes('60.000,00'))") == "true";
                    if (rewarded) break;
                    await Task.Delay(75);
                }
                if (!rewarded)
                {
                    string diagnostics = await web.ExecuteScriptAsync(
                        "JSON.stringify({notice:document.querySelector('.vault-notice')?.textContent,"
                        + "reward:document.querySelector('.vault-reward')?.textContent,"
                        + "button:document.querySelector('.vault-break')?.textContent})");
                    throw new InvalidOperationException(
                        "React Vault did not display the German-formatted Gold $60,000 payout. DOM: " + diagnostics);
                }
                Require(await web.ExecuteScriptAsync(
                    "Boolean(document.querySelector('.vault-break:disabled'))") == "true",
                    "A paid Vault can still be opened a second time.");
                Require(Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible) == visibleWindowsBefore,
                    "React Vault unexpectedly opened a second native window.");
                await using (FileStream rewardShot = File.Create(Path.Combine(output, "casino-vault-opened.png")))
                    await web.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, rewardShot);
                await web.ExecuteScriptAsync("document.querySelector('.vault-page .shop-back')?.click()");
                for (int i = 0; i < 100 && modern.RecordsPage is not null; i++) await Task.Delay(60);
                Require(modern.RecordsPage is null, "React Vault did not return to the lobby.");
                if (!startedGerman)
                {
                    await web.ExecuteScriptAsync("document.querySelector('.lobby-language')?.click()");
                    for (int i=0;i<100;i++)
                    {
                        if (await web.ExecuteScriptAsync(
                            "document.documentElement.lang==='en'") == "true") break;
                        await Task.Delay(50);
                    }
                    Require(await web.ExecuteScriptAsync("document.documentElement.lang==='en'") == "true",
                        "React Vault smoke did not restore the English casino locale.");
                }
                File.WriteAllText(Path.Combine(output, "vault-result.txt"),
                    "PASS: React Vault opened through its native C# transaction, paid the Gold reserve with the full bonus, and returned to lobby without a WPF overlay.");
                return;
            }
            Button vault = Find<Button>(window).Single(button => button.Name == "CasinoVaultButton" && button.IsVisible);
            vault.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            for (int i = 0; i < 30 && !Find<Grid>(window)
                .Any(grid => grid.Name == "CasinoVaultOverlay" && grid.IsVisible); i++)
                await Task.Delay(50);
            window.UpdateLayout();

            Grid overlay = Find<Grid>(window).Single(grid => grid.Name == "CasinoVaultOverlay" && grid.IsVisible);
            Border modal = Find<Border>(overlay).Single(border => border.Name == "CasinoVaultModalFrame" && border.IsVisible);
            Require(modal.ActualWidth > 900 && modal.ActualHeight > 600,
                "Casino Vault in-app modal did not render at its authored size.");
            Require(Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible) == visibleWindowsBefore,
                "Casino Vault regressed to a second visible native Window.");
            Require(!Application.Current.Windows.Cast<Window>().Any(w => w.IsVisible && w != window && w.Title.Contains("Vault", StringComparison.OrdinalIgnoreCase)),
                "Casino Vault exposed native Windows chrome instead of the in-app modal.");

            Capture(window, Path.Combine(output, "casino-vault-in-app.png"));

            Button open = Find<Button>(overlay).Single(button => button.Name == "CasinoVaultOpenButton" && button.IsVisible);
            Require(open.IsEnabled && open.Content?.ToString()?.StartsWith("BREAK OPEN", StringComparison.Ordinal) == true,
                "Full Gold Vault is not ready to open in the in-app modal.");
            open.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(100);
            window.UpdateLayout();

            TextBlock bonus = Find<TextBlock>(overlay).Single(text => text.Name == "VaultRewardBonus" && text.IsVisible);
            TextBlock total = Find<TextBlock>(overlay).Single(text => text.Name == "VaultRewardTotal" && text.IsVisible);
            Require(bonus.Text.StartsWith("FULL VAULT BONUS", StringComparison.Ordinal),
                "Vault reward bonus label lost its leading text.");
            Require(bonus.Text.Contains("10.000,00", StringComparison.Ordinal),
                "Vault reward smoke did not exercise the longer German-formatted bonus amount.");
            Require(bonus.TextWrapping == TextWrapping.Wrap && bonus.TextAlignment == TextAlignment.Center &&
                    bonus.HorizontalAlignment == HorizontalAlignment.Stretch,
                "Vault reward bonus line is not protected against localized text clipping.");
            Require(bonus.ActualWidth >= 250 && bonus.ActualHeight >= 20,
                $"Vault reward bonus layout is too small: {bonus.ActualWidth:0.#}x{bonus.ActualHeight:0.#}.");
            Require(total.Text.Contains("60.000,00", StringComparison.Ordinal),
                "Gold Vault did not present the expected $60,000 total payout in German formatting.");

            Capture(window, Path.Combine(output, "casino-vault-opened.png"));

            Button close = Find<Button>(overlay).Single(button => button.Name == "CasinoVaultCloseButton" && button.IsVisible);
            close.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(80);
            window.UpdateLayout();
            Require(!Find<Grid>(window).Any(grid => grid.Name == "CasinoVaultOverlay" && grid.IsVisible),
                "Casino Vault overlay remained visible after closing it.");
            Require(Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible) == visibleWindowsBefore,
                "Closing the in-app Vault changed the native window count.");

            File.WriteAllText(Path.Combine(output, "vault-result.txt"),
                "PASS: Casino Vault stayed inside MainWindow, rendered custom Midnight Club chrome, opened a full Gold Vault, and displayed the German-formatted reward without clipping.");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    private static void Capture(FrameworkElement element, string path)
    {
        element.UpdateLayout();
        int width = Math.Max(1, (int)Math.Ceiling(element.ActualWidth));
        int height = Math.Max(1, (int)Math.Ceiling(element.ActualHeight));
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(path);
        encoder.Save(stream);
    }

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            foreach (T child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
