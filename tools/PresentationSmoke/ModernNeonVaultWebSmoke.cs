using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using BlackjackCasino.Desktop;
using Microsoft.Web.WebView2.Core;

internal static partial class Program
{
    private static async Task CheckModernNeonVaultRoute(MainWindow window)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var settingsField = typeof(MainWindow).GetField("preferences", flags)!;
        AppPreferences scoped = (AppPreferences)settingsField.GetValue(window)!;
        Require(!scoped.Presentation("neon-vault").SoundEnabled,
            "Neon Vault unexpectedly inherited Midnight Reels sound settings.");

        NavigationSmoke.OpenCovered(window, "neon-vault");
        ModernNeonVaultView? cabinet = null;
        for (int n = 0; n < 155; n++)
        {
            await Task.Delay(75);
            cabinet = Find<ModernNeonVaultView>(window).FirstOrDefault();
            if (cabinet is { Ready: true } or { Failed: true }) break;
        }
        Require(cabinet is { Ready: true, Failed: false, IsVisible: true },
            "Neon Vault did not present its React WebView2 view.");
        Require(Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible) == 1,
            "Neon Vault opened an extra native window.");
        Require(!((FrameworkElement)window.FindName("CasinoHeader")).IsVisible,
            "Native casino header was not hidden for React Neon Vault.");
        var browser = cabinet!.Browser.CoreWebView2!;
        Require(await browser.ExecuteScriptAsync(
            "Boolean(document.querySelector('[data-testid=\"neon-vault-app\"]')"
            + "&&document.querySelectorAll('.vault-cell').length===20"
            + "&&document.querySelector('.vault-energy[aria-valuemax=\"12\"]')"
            + "&&document.querySelectorAll('.vault-jackpot').length===4"
            + "&&document.querySelector('[data-testid=\"neon-vault-max\"]')"
            + "&&document.querySelector('.vault-reactor-rail')"
            + "&&document.querySelector('[data-neon-engine=\"pixi-cabinet-v2\"]')"
            + "&&document.querySelectorAll('.vault-reel-column').length===5"
            + "&&document.querySelectorAll('.vault-cell .vault-symbol').length===20)") == "true",
            "React Neon Vault is missing the five-reel symbols, energy meter or MAX control.");
        await browser.ExecuteScriptAsync(
            "document.querySelector('[data-testid=\"neon-vault-settings\"]')?.click()");
        Require(await browser.ExecuteScriptAsync(
            "document.querySelector('[data-testid=\"neon-vault-settings-panel\"] input')?.checked===false") == "true",
            "React Neon Vault inherited the wrong sound preference.");
        foreach (var size in new[] { (1080d, 720d), (1440d, 900d), (1920d, 1080d), (2560d, 1440d) })
        {
            window.Width = size.Item1;
            window.Height = size.Item2;
            window.UpdateLayout();
            await Task.Delay(140);
            string rectangle = await browser.ExecuteScriptAsync(
                "(()=>{let r=document.querySelector('[data-testid=\"neon-vault-spin\"]')?.getBoundingClientRect();"
                + "return r?{top:r.top,bottom:r.bottom,left:r.left,right:r.right,width:r.width,"
                + "viewportHeight:window.innerHeight,viewportWidth:window.innerWidth}:null})()");
            using JsonDocument doc = JsonDocument.Parse(rectangle);
            JsonElement box = doc.RootElement;
            Require(box.ValueKind == JsonValueKind.Object, "Neon Vault Spin missing at " + size);
            double top = box.GetProperty("top").GetDouble();
            double bottom = box.GetProperty("bottom").GetDouble();
            double left = box.GetProperty("left").GetDouble();
            double right = box.GetProperty("right").GetDouble();
            double width = box.GetProperty("width").GetDouble();
            double viewHeight = box.GetProperty("viewportHeight").GetDouble();
            double viewWidth = box.GetProperty("viewportWidth").GetDouble();
            Require(top >= 56 && bottom <= viewHeight - 5
                && left >= 0 && right <= viewWidth && width > 120,
                $"React Neon Vault Spin is clipped at {size}: " + rectangle);
            await using var image = File.Create(Path.Combine(output,
                "05-neon-vault-" + (int)size.Item1 + ".png"));
            await browser.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, image);
        }
        await browser.ExecuteScriptAsync("document.querySelector('.vault-back')?.click()");
        for (int n = 0; n < 90 && Find<ModernNeonVaultView>(window).Any(x => x.IsVisible); n++)
            await Task.Delay(75);
        Require(!Find<ModernNeonVaultView>(window).Any(x => x.IsVisible)
            && Find<CasinoLobbyView>(window).Any(x => x.IsVisible),
            "React Neon Vault did not return to the casino lobby.");
        Require(((FrameworkElement)window.FindName("CasinoHeader")).IsVisible
            == (!ModernLobbyView.IsAvailable || Find<ModernLobbyView>(window).Any(v => v.Failed)),
            "Lobby header visibility did not match the active React/WPF presentation after Neon Vault.");
    }
}
