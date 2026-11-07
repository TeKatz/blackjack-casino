using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BlackjackCasino.Core;
using BlackjackCasino.Desktop;

internal static partial class Program
{
    private static async Task CheckMenuPerformance(MainWindow window, PlayerProfileState profile)
    {
        // Once React has become authoritative, opening a menu must not be
        // judged by whether a retired WPF ProgressionWindow was materialized.
        ModernLobbyView? react = Find<ModernLobbyView>(window)
            .FirstOrDefault(view => view.Ready && view.IsVisible);
        if (react is not null)
        {
            var timings = new List<object>();
            var core = react.Browser.CoreWebView2
                ?? throw new InvalidOperationException("Ready React lobby is missing its browser.");
            foreach (string section in new[] { "Challenges", "Cosmetic Shop" })
            {
                var watch = Stopwatch.StartNew();
                if (section == "Challenges") react.ShowProgression("challenges");
                else react.ShowShop();
                string active = section == "Challenges" ? "records-active" : "shop-active";
                bool displayed = false;
                for (int attempt = 0; attempt < 70; attempt++)
                {
                    string visible = await core.ExecuteScriptAsync(
                        "Boolean(document.querySelector('.lobby-body." + active + "'))");
                    if (visible == "true") { displayed = true; break; }
                    await Task.Delay(60);
                }
                Require(displayed, "React " + section + " did not materialize in the live lobby.");
                await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
                watch.Stop();
                int shopRows = 0;
                if (section == "Cosmetic Shop")
                {
                    for (int attempt = 0; attempt < 70; attempt++)
                    {
                        shopRows = int.Parse(await core.ExecuteScriptAsync(
                            "document.querySelectorAll('.shop-product').length"),
                            System.Globalization.CultureInfo.InvariantCulture);
                        if (shopRows > 0) break;
                        await Task.Delay(50);
                    }
                    Require(shopRows > 0 && shopRows <= 32,
                        $"React Shop materialized {shopRows} items outside its viewport budget.");
                }
                timings.Add(new { section, elapsedMilliseconds = watch.ElapsedMilliseconds,
                    materializedShopRows = shopRows });
                await core.ExecuteScriptAsync(
                    "[...document.querySelectorAll('.lobby-nav-item')]"
                    + ".find(b=>/Lobby|Startseite/.test(b.textContent||''))?.click()");
                for (int attempt = 0; attempt < 60 && (react.ShopOpen || react.RecordsPage is not null); attempt++)
                    await Task.Delay(50);
                Require(!react.ShopOpen && react.RecordsPage is null,
                    "React menu did not return to the primary casino lobby.");
            }
            File.WriteAllText(Path.Combine(output, "menu-opening-timings.json"),
                System.Text.Json.JsonSerializer.Serialize(timings));
            var pages = (IEnumerable)typeof(MainWindow).GetField(
                "metaPages", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
            Require(!pages.Cast<object>().Any(),
                "React menu navigation unnecessarily instantiated the retired WPF progression shell.");
        }
        else
        {
            var timings = new List<object>();
            foreach (string section in new[] { "Challenges", "Cosmetic Shop" })
            {
                var watch = Stopwatch.StartNew();
                typeof(MainWindow).GetMethod("OpenProgressionPage", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, [section]);
                window.UpdateLayout();
                await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
                watch.Stop();
                var pages = (IEnumerable)typeof(MainWindow).GetField("metaPages", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
                object page = pages.Cast<object>().First();
                var shell = (ProgressionWindow)page.GetType().GetProperty("Shell")!.GetValue(page)!;
                Panel Panel(string name) => (Panel)(shell.FindName(name) ?? typeof(ProgressionWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(shell))!;
                Require(Panel("AlbumCards").Children.Count == 0 && Panel("AlbumThemes").Children.Count == 0, "Opening " + section + " eagerly built the retired album tab.");
                Require(Panel("SeasonRewardsPanel").Children.Count == 0 && Panel("GameStatsPanel").Children.Count == 0 && Panel("HistoryPanel").Children.Count == 0,
                    "Opening " + section + " constructed hidden progression sections.");
    
                Panel shopPanel = Panel("ShopPanel");
                int materializedShopRows = shopPanel.Children.OfType<FrameworkElement>().Count(element => element.Uid != "shop-placeholder");
                if (section == "Challenges")
                {
                    Require(shopPanel.Children.Count == 0, "Challenges constructed the hidden shop.");
                }
                else
                {
                    Require(Panel("ChallengesPanel").Children.Count == 0, "Shop constructed hidden challenges.");
                    Require(shopPanel.Children.Count == CosmeticCatalog.All.Count,
                        "Shop no longer exposes the full filtered catalogue to its scroll surface.");
                    // The initial 18-row materialization can legitimately gain a few rows when the
                    // ScrollViewer performs its first real layout and asks for the viewport preload
                    // cushion. Guard the optimization by bounding that cushion rather than requiring
                    // an exact count that varies slightly with runner DPI/window metrics.
                    int expectedMaximum = Math.Min(32, CosmeticCatalog.All.Count);
                    Require(materializedShopRows > 0 && materializedShopRows <= expectedMaximum,
                        $"Shop eagerly materialized {materializedShopRows} cosmetic previews; expected at most {expectedMaximum} on first open.");
                    if (CosmeticCatalog.All.Count > expectedMaximum)
                        Require(materializedShopRows < CosmeticCatalog.All.Count, "Shop eagerly materialized the entire cosmetic catalogue.");
                }
    
                timings.Add(new { section, elapsedMilliseconds = watch.ElapsedMilliseconds, materializedShopRows });
                Find<Button>(window).Single(b => b.Name == "ProgressionBackButton" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Delay(25);
            }
            File.WriteAllText(Path.Combine(output, "menu-opening-timings.json"), System.Text.Json.JsonSerializer.Serialize(timings));
    
        }

        CasinoLobbyView lobby = Find<CasinoLobbyView>(window).Single();
        Button stats = Find<Button>(lobby).Single(b => b.Name == "LobbyProgression_Overview");
        Require(Equals(stats.Tag, "Statistics") && Find<TextBlock>(stats).Any(t => t.Text == "Statistics"), "Lobby Statistics still has the retired career overview copy.");
        LobbyProgressSnapshot expected = LobbyProgressSnapshot.From(profile);
        Require(Math.Abs(Find<ProgressBar>(lobby).Single(p => p.Name == "LobbySeasonProgress").Value - expected.SeasonFraction) < .001, "Lobby season progress differs from current tier XP.");
        Require(Math.Abs(Find<ProgressBar>(lobby).Single(p => p.Name == "LobbyChallengeProgress").Value - expected.ChallengeFraction) < .001, "Lobby challenge progress is not the closest active challenge.");
        // The native Blackjack header is now collapsed outside the Blackjack route.
        // Visual-tree enumeration no longer materializes its legacy Album image.
        // The current React header/album navigation is exercised by CollectionSmoke,
        // while the WPF fallback still verifies its own collection progress below.
        Border badge = Find<Border>(lobby).Single(b => b.Name == "LobbyAlbumPackBadge");
        Require(badge.MinWidth >= 44 && badge.Height >= 32, "Lobby pack badge is still undersized.");
        var preferencesField = typeof(MainWindow).GetField("preferences", BindingFlags.Instance | BindingFlags.NonPublic)!;
        AppPreferences prefs = (AppPreferences)preferencesField.GetValue(window)!;
        preferencesField.SetValue(window, prefs.WithPresentation("lobby", prefs.Presentation("lobby") with { AnimationsEnabled = true }));
        lobby.SetAlbumPackCount(1); PackBadgeMotion.Sync(badge);
        Require(((ScaleTransform)badge.RenderTransform).HasAnimatedProperties, "Available pack badge has no breathing animation.");
        badge.Visibility = Visibility.Collapsed;
        Require(!((ScaleTransform)badge.RenderTransform).HasAnimatedProperties, "Hidden pack badge still animates.");
        preferencesField.SetValue(window, prefs);
        lobby.SetAlbumPackCount(profile.Collection.Packs.Values.Sum());
        PackBadgeMotion.Sync(badge);
        Require(!((ScaleTransform)badge.RenderTransform).HasAnimatedProperties, "Reduced motion did not stop pack breathing.");
    }
}
