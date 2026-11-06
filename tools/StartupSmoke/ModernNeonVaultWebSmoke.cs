using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using BlackjackCasino.Core;
using BlackjackCasino.Desktop;
using Microsoft.Web.WebView2.Core;

/// <summary>Validates the real offline React cabinet, shared wallet, and browser failure recovery.</summary>
internal static class ModernNeonVaultWebSmoke
{
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

    internal static async Task Run(MainWindow window, PlayerProfileState profile, string output)
    {
        long spinsBefore = profile.NeonVault.Spins;
        window.Modes.Open("neon-vault");
        ModernNeonVaultView view = await WaitForView(window);
        Require(view.Ready && !view.Failed && view.IsVisible,
            "Neon Vault did not mount the packaged React/WebView2 cabinet.");
        Require(Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible) == 1,
            "Neon Vault created a second native game window.");
        var core = view.Browser.CoreWebView2!;
        Require(await core.ExecuteScriptAsync(
            "Boolean(document.querySelector('[data-testid=\"neon-vault-app\"]')"
            + "&&document.querySelectorAll('.vault-cell').length===20"
            + "&&document.querySelectorAll('.vault-cell svg').length===20"
            + "&&document.querySelectorAll('.vault-payrow').length===8"
            + "&&document.querySelectorAll('.vault-bets button').length===16"
            + "&&document.querySelectorAll('.vault-jackpot').length===4"
            + "&&document.querySelector('[data-testid=\"neon-vault-spin\"]'))") == "true",
            "Neon Vault did not render five reels, four rows, all symbols and jackpots.");
        Require(await core.ExecuteScriptAsync(
            "Boolean(document.querySelector('.vault-energy')"
            + "&&document.querySelector('[data-testid=\"neon-vault-bonus\"]')"
            + "&&document.querySelector('[data-testid=\"neon-vault-max\"]'))") == "true",
            "Neon Vault energy/free-spin panel or high-roller controls are missing.");
        await core.ExecuteScriptAsync("document.querySelector('[data-testid=\"neon-vault-paytable\"]')?.click()");
        Require(await core.ExecuteScriptAsync(
            "document.querySelectorAll('.vault-paytable-details > div').length===6") == "true",
            "The full 3-/4-/5-reel paytable was not opened.");
        await core.ExecuteScriptAsync("document.querySelector('[data-testid=\"neon-vault-settings\"]')?.click()");
        Require(await core.ExecuteScriptAsync(
            "Boolean(document.querySelector('[data-testid=\"neon-vault-settings-panel\"]'))") == "true",
            "Neon Vault React presentation settings are missing.");
        using (var image = File.Create(Path.Combine(output, "neon-vault-react-cabinet.png")))
            await core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, image);

        // The CI spin uses a deterministic native RNG. It is still settled by
        // NeonVaultSession, never by JavaScript, but cannot randomly trigger
        // a ten-second bonus feature that would make the smoke nondeterministic.
        object controller = typeof(MainWindow).GetField("blackjack", Flags)!.GetValue(window)!;
        CasinoSession shared = (CasinoSession)controller.GetType().GetProperty("Session", Flags)!
            .GetValue(controller)!;
        var machineField = typeof(MainWindow).GetField("modernNeonVaultMachine", Flags)!;
        machineField.SetValue(window, new NeonVaultSession(shared.Bankroll,
            max => max == 10_000 ? 5_000 : 0));
        await core.ExecuteScriptAsync(
            "document.querySelector('.vault-bets button[data-bet=\"1\"]')?.click()");
        for (int n = 0; n < 45; n++)
        {
            await Task.Delay(80);
            string raw = await core.ExecuteScriptAsync(
                "document.querySelector('.vault-meters div:nth-child(2) strong')?.textContent??''");
            if (JsonSerializer.Deserialize<string>(raw)?.Contains("$1.00") == true) break;
        }
        Require(await core.ExecuteScriptAsync(
            "Boolean(document.querySelector('[data-testid=\"neon-vault-spin\"]:not(:disabled)'))") == "true",
            "Neon Vault cannot spin after choosing a legal affordable wager.");
        await core.ExecuteScriptAsync(
            "document.querySelector('[data-testid=\"neon-vault-spin\"]')?.click()");
        string spinId = "";
        for (int n = 0; n < 110; n++)
        {
            await Task.Delay(80);
            string raw = await core.ExecuteScriptAsync(
                "document.querySelector('[data-testid=\"neon-vault-app\"]')?.dataset.spinId??''");
            spinId = JsonSerializer.Deserialize<string>(raw) ?? "";
            if (Guid.TryParse(spinId, out _) && profile.NeonVault.Spins == spinsBefore + 1) break;
        }
        Require(Guid.TryParse(spinId, out _), "Neon Vault did not present its C# spin result.");
        Require(profile.NeonVault.Spins == spinsBefore + 1,
            "Neon Vault did not commit player progression exactly once.");
        var machine = (NeonVaultSession)machineField.GetValue(window)!;
        Require(shared.Bankroll == machine.Bankroll,
            "Neon Vault changed the game wallet without matching the shared C# bankroll.");
        // Wait until the spin presentation is finished and React allows return.
        for (int n = 0; n < 90; n++)
        {
            await Task.Delay(80);
            if (await core.ExecuteScriptAsync(
                "document.querySelector('[data-testid=\"neon-vault-app\"]')?.dataset.animating") == "\"false\"") break;
        }
        await core.ExecuteScriptAsync("document.querySelector('.vault-back')?.click()");
        await WaitForExit(window);
        Require(Find<CasinoLobbyView>(window).Any(x => x.IsVisible),
            "Neon Vault Back did not restore the lobby.");

        // Browser failure must preserve the C# machine and receipt ledger.
        window.Modes.Open("neon-vault");
        ModernNeonVaultView reopened = await WaitForView(window);
        Require(reopened.Ready && !reopened.Failed, "Neon Vault React cannot reopen.");
        object retained = machineField.GetValue(window)!;
        typeof(ModernNeonVaultView).GetMethod("Fail", Flags)!.Invoke(reopened, null);
        await WaitForExit(window);
        BrowserRecoveryView? recovery = null;
        for (int n = 0; n < 90; n++)
        {
            await Task.Delay(75);
            recovery = Find<BrowserRecoveryView>(window).FirstOrDefault(x => x.IsVisible);
            if (recovery is not null) break;
        }
        Require(recovery is not null && recovery.RetryButton.IsEnabled
            && !recovery.LegacyButton.IsEnabled,
            "Neon Vault WebView failure did not show modern-only recovery.");
        recovery!.RetryButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        ModernNeonVaultView recovered = await WaitForView(window);
        Require(recovered.Ready && !recovered.Failed, "Neon Vault WebView failed to recover.");
        Require(ReferenceEquals(retained, machineField.GetValue(window)),
            "Neon Vault recovery recreated the machine, discarding native bonus state.");
        Require(profile.NeonVault.Spins == spinsBefore + 1,
            "Neon Vault recovery replayed a settled spin.");
        await recovered.Browser.CoreWebView2!.ExecuteScriptAsync(
            "document.querySelector('.vault-back')?.click()");
        await WaitForExit(window);
        Require(Find<CasinoLobbyView>(window).Any(x => x.IsVisible),
            "Neon Vault recovery did not end at the lobby.");
    }

    private static async Task<ModernNeonVaultView> WaitForView(MainWindow window)
    {
        ModernNeonVaultView? view = null;
        for (int n = 0; n < 165; n++)
        {
            await Task.Delay(75);
            view = Find<ModernNeonVaultView>(window).FirstOrDefault();
            if (view is { Ready: true } or { Failed: true }) break;
        }
        return view ?? throw new InvalidOperationException("The Neon Vault WebView never mounted.");
    }
    private static async Task WaitForExit(MainWindow window)
    {
        for (int n = 0; n < 90 && Find<ModernNeonVaultView>(window).Any(x => x.IsVisible); n++)
            await Task.Delay(75);
        Require(!Find<ModernNeonVaultView>(window).Any(x => x.IsVisible),
            "Neon Vault WebView remained hosted after the close command.");
    }
    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T value) yield return value;
        for (int n = 0; n < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); n++)
            foreach (T child in Find<T>(System.Windows.Media.VisualTreeHelper.GetChild(root, n)))
                yield return child;
    }
    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
    }
}
