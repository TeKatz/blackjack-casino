using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using BlackjackCasino.Core;
using BlackjackCasino.Desktop;
using Microsoft.Web.WebView2.Core;

/// <summary>Exercise the packaged browser, not an invisible WPF backup table.</summary>
internal static class ModernNineLivesWebSmoke
{
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

    internal static async Task Run(MainWindow window, PlayerProfileState profile, string output)
    {
        long spinsBefore = profile.NineLives.Spins;
        window.Modes.Open("nine-lives");
        ModernNineLivesView? cabinet = null;
        for (int i = 0; i < 150; i++)
        {
            await Task.Delay(75);
            cabinet = Find<ModernNineLivesView>(window).FirstOrDefault();
            if (cabinet is { Ready: true } or { Failed: true }) break;
        }
        Require(cabinet is { Ready: true, Failed: false, IsVisible: true },
            "Nine Lives did not open the packaged React/WebView2 cabinet.");
        Require(!Find<FrameworkElement>(window).Any(v => v.IsVisible && v.GetType().Name == "NineLivesWindow"),
            "The retired Nine Lives WPF view is still the game route.");
        Require(Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible) == 1,
            "Nine Lives opened another visible native window.");
        var core = cabinet!.Browser.CoreWebView2!;
        Require(await core.ExecuteScriptAsync(
            "Boolean(document.querySelector('[data-testid=\"nine-lives-app\"]')"
            + "&&document.querySelectorAll('.nine-cell').length===9"
            + "&&document.querySelectorAll('.nine-payrow').length===8"
            + "&&document.querySelectorAll('.nine-bet-grid button').length===13"
            + "&&document.querySelector('[data-testid=\"nine-lives-spin\"]'))") == "true",
            "React Nine Lives board, paytable or controls are incomplete.");

        bool artworkReady = false;
        for (int i = 0; i < 70; i++)
        {
            artworkReady = await core.ExecuteScriptAsync(
                "(()=>{const images=[...document.querySelectorAll('.nine-cell img,.nine-payrow img')];"
                + "return images.length===17&&images.every(image=>image.complete"
                + "&&image.naturalWidth>=1024&&image.src.includes('/nine-lives-art/'))})()") == "true";
            if (artworkReady) break;
            await Task.Delay(90);
        }
        Require(artworkReady, "Nine Lives did not load all eight premium 1024px packaged JPGs.");

        await core.ExecuteScriptAsync("document.querySelector('[data-testid=\"nine-lives-settings\"]')?.click()");
        Require(await core.ExecuteScriptAsync(
            "Boolean(document.querySelector('[data-testid=\"nine-lives-settings-panel\"]'))") == "true",
            "Nine Lives React presentation settings flyout is missing.");
        await core.ExecuteScriptAsync("document.querySelector('[data-testid=\"nine-lives-paytable\"]')?.click()");
        Require(await core.ExecuteScriptAsync(
            "document.querySelectorAll('[data-testid=\"nine-lives-paytable-details\"] div').length===5") == "true",
            "Nine Lives React five-line explanation is unavailable.");
        using (var shot = File.Create(Path.Combine(output, "nine-lives-modern-cabinet.png")))
            await core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, shot);

        await core.ExecuteScriptAsync("document.querySelector('[data-testid=\"nine-lives-spin\"]')?.click()");
        string spinId = "";
        for (int i = 0; i < 100; i++)
        {
            await Task.Delay(80);
            string raw = await core.ExecuteScriptAsync(
                "document.querySelector('[data-testid=\"nine-lives-app\"]')?.dataset.spinId??''");
            spinId = System.Text.Json.JsonSerializer.Deserialize<string>(raw) ?? "";
            if (Guid.TryParse(spinId, out _) && profile.NineLives.Spins == spinsBefore + 1) break;
        }
        Require(Guid.TryParse(spinId, out _), "Nine Lives spin result never reached React.");
        Require(profile.NineLives.Spins == spinsBefore + 1,
            "Nine Lives did not award its C# progression exactly once.");
        object controller = typeof(MainWindow).GetField("blackjack", Flags)!.GetValue(window)!;
        var session = (CasinoSession)controller.GetType().GetProperty("Session", Flags)!.GetValue(controller)!;
        var machine = (NineLivesSession)typeof(MainWindow).GetField("modernNineLivesMachine", Flags)!
            .GetValue(window)!;
        Require(session.Bankroll == machine.Bankroll,
            "Nine Lives did not synchronize the shared authoritative wallet.");
        await core.ExecuteScriptAsync("document.querySelector('.nine-back')?.click()");
        for (int i = 0; i < 80 && Find<ModernNineLivesView>(window).Any(v => v.IsVisible); i++)
            await Task.Delay(75);
        Require(!Find<ModernNineLivesView>(window).Any(v => v.IsVisible)
            && Find<CasinoLobbyView>(window).Any(v => v.IsVisible),
            "Nine Lives did not return to the casino lobby.");
        // Test WebView loss with an unchanged machine, not a fallback casino.
        window.Modes.Open("nine-lives");
        ModernNineLivesView? reopened = null;
        for (int i = 0; i < 150; i++)
        {
            await Task.Delay(75);
            reopened = Find<ModernNineLivesView>(window).FirstOrDefault();
            if (reopened is { Ready: true } or { Failed: true }) break;
        }
        Require(reopened is { Ready: true, Failed: false, IsVisible: true },
            "React Nine Lives cannot be reopened.");
        var retained = typeof(MainWindow).GetField("modernNineLivesMachine", Flags)!.GetValue(window);
        typeof(ModernNineLivesView).GetMethod("Fail", Flags)!.Invoke(reopened, null);
        for (int i = 0; i < 80 && Find<ModernNineLivesView>(window).Any(v => v.IsVisible); i++)
            await Task.Delay(75);
        Require(!Find<ModernNineLivesView>(window).Any(v => v.IsVisible),
            "Nine Lives failure did not dispose its browser host.");
        BrowserRecoveryView? recovery = null;
        for (int i = 0; i < 80; i++)
        {
            await Task.Delay(75);
            recovery = Find<BrowserRecoveryView>(window).FirstOrDefault(v => v.IsVisible);
            if (recovery is not null) break;
        }
        Require(recovery is not null && recovery.RetryButton.IsEnabled
            && !recovery.LegacyButton.IsEnabled,
            "Nine Lives browser crash did not show modern-only recovery.");
        recovery.RetryButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        ModernNineLivesView? recovered = null;
        for (int i = 0; i < 150; i++)
        {
            await Task.Delay(75);
            recovered = Find<ModernNineLivesView>(window).FirstOrDefault();
            if (recovered is { Ready: true } or { Failed: true }) break;
        }
        Require(recovered is { Ready: true, Failed: false, IsVisible: true },
            "Nine Lives could not recreate React after a browser failure.");
        Require(ReferenceEquals(retained,
            typeof(MainWindow).GetField("modernNineLivesMachine", Flags)!.GetValue(window)),
            "Browser failure discarded the authoritative Nine Lives machine.");
        await recovered!.Browser.CoreWebView2!.ExecuteScriptAsync("document.querySelector('.nine-back')?.click()");
        for (int i = 0; i < 80 && Find<ModernNineLivesView>(window).Any(v => v.IsVisible); i++)
            await Task.Delay(75);
        Require(Find<CasinoLobbyView>(window).Any(v => v.IsVisible),
            "Nine Lives recovery did not restore the lobby.");
    }

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T value) yield return value;
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (T child in Find<T>(System.Windows.Media.VisualTreeHelper.GetChild(root, i)))
                yield return child;
    }
    private static void Require(bool valid, string error)
    {
        if (!valid) throw new InvalidOperationException(error);
    }
}
