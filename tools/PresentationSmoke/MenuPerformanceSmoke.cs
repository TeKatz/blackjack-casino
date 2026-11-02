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

            int renderedShopRows = Panel("ShopPanel").Children.Count;
            if (section == "Challenges")
            {
                Require(renderedShopRows == 0, "Challenges constructed the entire hidden shop.");
            }
            else
            {
                Require(Panel("ChallengesPanel").Children.Count == 0, "Shop constructed hidden challenges.");
                int expectedMaximum = Math.Min(18, CosmeticCatalog.All.Count);
                Require(renderedShopRows > 0 && renderedShopRows <= expectedMaximum,
                    $"Shop eagerly rendered {renderedShopRows} cosmetic rows; expected at most {expectedMaximum} on first open.");
                if (CosmeticCatalog.All.Count > 18)
                    Require(renderedShopRows < CosmeticCatalog.All.Count, "Shop eagerly rendered the entire cosmetic catalogue.");
            }

            timings.Add(new { section, elapsedMilliseconds = watch.ElapsedMilliseconds, renderedShopRows });
            Find<Button>(window).Single(b => b.Name == "ProgressionBackButton" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(25);
        }
        File.WriteAllText(Path.Combine(output, "menu-opening-timings.json"), System.Text.Json.JsonSerializer.Serialize(timings));

        CasinoLobbyView lobby = Find<CasinoLobbyView>(window).Single();
        Button stats = Find<Button>(lobby).Single(b => b.Name == "LobbyProgression_Overview");
        Require(Equals(stats.Tag, "Statistics") && Find<TextBlock>(stats).Any(t => t.Text == "Statistics"), "Lobby Statistics still has the retired career overview copy.");
        LobbyProgressSnapshot expected = LobbyProgressSnapshot.From(profile);
        Require(Math.Abs(Find<ProgressBar>(lobby).Single(p => p.Name == "LobbySeasonProgress").Value - expected.SeasonFraction) < .001, "Lobby season progress differs from current tier XP.");
        Require(Math.Abs(Find<ProgressBar>(lobby).Single(p => p.Name == "LobbyChallengeProgress").Value - expected.ChallengeFraction) < .001, "Lobby challenge progress is not the closest active challenge.");
        Image lobbyAlbum = Find<Image>(lobby).Single(i => Equals(i.Tag, "album"));
        Image headerAlbum = Find<Image>(Find<Button>(window).Single(b => b.Name == "HeaderCollectionAlbumButton")).Single();
        Require(ReferenceEquals(lobbyAlbum.Source, headerAlbum.Source) && headerAlbum.Source.IsFrozen, "Header and lobby do not reuse one frozen generated emblem.");
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
