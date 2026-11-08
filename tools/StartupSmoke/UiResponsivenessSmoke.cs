using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Empire;
using BlackjackCasino.Desktop;

internal static class UiResponsivenessSmoke
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

    internal static async Task Run(MainWindow window, string output)
    {
        var prefsField = typeof(MainWindow).GetField("preferences", Flags)!;
        var empireField = typeof(MainWindow).GetField("empire", Flags)!;
        var controller = (PlayerProgressionController)typeof(MainWindow).GetField("progressionController", Flags)!.GetValue(window)!;
        AppPreferences oldPrefs = (AppPreferences)prefsField.GetValue(window)!;
        object oldEmpire = empireField.GetValue(window)!;
        PlayerProfileState profile = controller.Profile;
        string oldCards = profile.EquippedCardSetId, oldFrame = profile.EquippedFrameId;
        int oldPrestige = profile.Prestige;
        string[] oldOwned = profile.OwnedCosmeticIds.ToArray();
        var host = new Grid { IsHitTestVisible = false, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom };
        var root = (Grid)window.Content;
        Grid.SetRowSpan(host, 2); Grid.SetColumnSpan(host, 2);
        try
        {
            empireField.SetValue(window, new EmpireService(EmpireState.Normalize(new EmpireState
            {
                Started = true, LastAccruedAt = DateTimeOffset.UtcNow, Prestige = 10, Credits = 41_360_000m,
                Levels = EmpireCatalog.Departments.ToDictionary(room => room.Id, room => room.Floor == 1 ? 20 : 0),
            })));
            prefsField.SetValue(window, oldPrefs.WithPresentation("lobby", oldPrefs.Presentation("lobby") with { AnimationsEnabled = true }));
            profile.Prestige = 3; profile.EquippedFrameId = "frame-prestige-10";
            profile.OwnedCosmeticIds.Add("frame-prestige-10"); profile.OwnedCosmeticIds.Add("cardset-emerald-house");
            Call(window, "RefreshProfileButton"); Call(window, "RefreshEmpireHeader");
            foreach (var size in new[] { (1080d, 720d), (1440d, 900d), (1920d, 1080d) })
            {
                window.Width = size.Item1; window.Height = size.Item2;
                window.UpdateLayout(); await Task.Delay(100); window.UpdateLayout();
                var bank = (FrameworkElement)window.FindName("BankrollPanel");
                var shortcuts = Find<FrameworkElement>(window).Single(e => e.Name == "CasinoHeaderShortcuts");
                var credits = (FrameworkElement)window.FindName("EmpireBalancePanel");
                FrameworkElement nativeHeader = (FrameworkElement)window.FindName("CasinoHeader");
                if (nativeHeader.IsVisible)
                {
                    // Keep the original WPF fallback geometry contract.
                    Rect a = Bounds(shortcuts, window), b = Bounds(bank, window), c = Bounds(credits, window);
                    Require(a.Right + 4 <= b.Left && b.Right + 4 <= c.Left,
                        "Header money overlaps shortcuts or Empire credits.");
                    Rect header = Bounds(nativeHeader, window);
                    Require(c.Right <= window.ActualWidth && b.Top >= header.Top && b.Bottom <= header.Bottom + 1,
                        "Header money left the header.");
                }
                else
                {
                    // The React lobby deliberately replaces the native casino header.
                    // Inspect the *visible WebView2 DOM*, rather than measuring an
                    // invisible WPF header and declaring it clipped.
                    ModernLobbyView modern = Find<ModernLobbyView>(window)
                        .Single(view => view.Ready && view.IsVisible);
                    string headerGeometry = await modern.Browser.CoreWebView2!.ExecuteScriptAsync(
                        "(()=>{const top=document.querySelector('.lobby-topbar');"
                        + "const wallet=document.querySelector('.lobby-finances');"
                        + "const brand=document.querySelector('.lobby-brand');"
                        + "if(!top||!wallet||!brand)return false;"
                        + "const a=top.getBoundingClientRect(),b=wallet.getBoundingClientRect(),"
                        + "c=brand.getBoundingClientRect();"
                        + "return a.height>=45&&b.width>60&&c.width>100&&"
                        + "b.left>=a.left&&b.right<=a.right+2&&"
                        + "b.top>=a.top&&b.bottom<=a.bottom+2;})()");
                    Require(headerGeometry.Equals("true", StringComparison.OrdinalIgnoreCase),
                        "React lobby header money is clipped or outside the visible topbar at "
                        + size.Item1 + "x" + size.Item2 + ".");
                }
                Border hub = Find<Border>(window).Single(e => e.Name == "LobbyOverviewHub");
                foreach (string title in new[] { "Season Pass", "Cosmetic Shop", "Collector's Album", "Challenges", "Statistics" })
                {
                    var text = Find<TextBlock>(hub).Single(t => t.Text == title);
                    Require(text.FontSize >= 18, title + " is still shrinking inside a fixed-width Viewbox.");
                    Rect titleBounds = Bounds(text, window);
                    Button tile = Ancestor<Button>(text)!;
                    Rect tileBounds = Bounds(tile, window);
                    Require(titleBounds.Right <= window.ActualWidth + 1 && titleBounds.Top >= tileBounds.Top && titleBounds.Bottom <= tileBounds.Bottom,
                        title + " clips the lobby hub.");
                }
                foreach (TextBlock status in Find<TextBlock>(hub).Where(t => t.Name is "LobbySeasonStatus" or "LobbyChallengeStatus"))
                {
                    Rect area = Bounds(status, window), tile = Bounds(Ancestor<Button>(status)!, window);
                    Require(area.Top >= tile.Top && area.Bottom <= tile.Bottom, "Lobby progress numbers are clipped.");
                }
                Capture(window, Path.Combine(output, $"responsive-lobby-{(int)size.Item1}.png"));
            }

            // A loaded hand card and an equipment change must still be styled immediately.
            Border card = PlayingCardPresentation.Create(new Card(Rank.Ace, Suit.Spades), false, false, null);
            host.Children.Add(card); root.Children.Add(host);
            await Task.Delay(50); window.UpdateLayout();
            File.WriteAllText(Path.Combine(output, "card-styling-state.json"), JsonSerializer.Serialize(new
            {
                card.Tag, expected = profile.EquippedCardSetId, card.IsLoaded,
                owner = Window.GetWindow(card)?.GetType().Name, fullScans = window.CardSetFullScanCount,
            }));
            Require(Equals(card.Tag, profile.EquippedCardSetId), "Newly loaded playing cards no longer receive the equipped design.");
            profile.EquippedCardSetId = "cardset-emerald-house";
            Call(window, "RefreshProfileButton");
            Require(Equals(card.Tag, profile.EquippedCardSetId), "Changing the equipped design did not restyle the existing hand.");
            root.Children.Remove(host);

            int scans = window.CardSetFullScanCount;
            object optimized = await Sample(window, legacyScan: false);
            Require(window.CardSetFullScanCount == scans, "Live header ticks still rescan the whole window for cards.");
            object legacy = await Sample(window, legacyScan: true);
            File.WriteAllText(Path.Combine(output, "ui-responsiveness.json"), JsonSerializer.Serialize(new
            {
                renderTier = RenderCapability.Tier >> 16,
                optimized, legacyTreeWalkReplay = legacy,
                fullCardScansWhileAnimated = window.CardSetFullScanCount - scans,
            }, new JsonSerializerOptions { WriteIndented = true }));

            // The covered header must stop its animation clocks while menus remain interactive.
            Call(window, "OpenProgressionPage", "Challenges"); await Task.Delay(80); window.UpdateLayout();
            var counter = (EmpireBalanceCounter)window.FindName("EmpireHeaderBalance");
            Require(!DependencyPropertyHelper.GetValueSource(counter, EmpireBalanceCounter.DisplayedAmountProperty).IsAnimated,
                "The hidden Empire header continues to animate underneath a menu.");
            ModernLobbyView? modernRecords = Find<ModernLobbyView>(window)
                .FirstOrDefault(view => view.Ready && view.IsVisible);
            if (modernRecords is not null)
            {
                for (int i = 0; i < 90 && modernRecords.RecordsPage != "challenges"; i++)
                    await Task.Delay(60);
                Require(modernRecords.RecordsPage == "challenges",
                    "The responsiveness test did not reach React Challenges.");
                await modernRecords.Browser.CoreWebView2!.ExecuteScriptAsync(
                    "document.querySelector('.record-page .shop-back')?.click()");
                for (int i = 0; i < 70 && modernRecords.RecordsPage is not null; i++)
                    await Task.Delay(60);
                Require(modernRecords.RecordsPage is null,
                    "React Challenges did not release the casino header.");
            }
            else
            {
                Find<Button>(window).Single(b => b.Name == "ProgressionBackButton" && b.IsVisible)
                    .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            File.WriteAllText(Path.Combine(output, "ui-responsiveness-result.txt"), "PASS: right-aligned bankroll rail; readable unscaled hub text; loaded/equipped card styling; no per-layout card scans; stopped covered header clocks; rendering and input latency samples recorded.");
        }
        finally
        {
            root.Children.Remove(host);
            profile.EquippedCardSetId = oldCards; profile.EquippedFrameId = oldFrame; profile.Prestige = oldPrestige;
            profile.OwnedCosmeticIds.Clear(); foreach (string id in oldOwned) profile.OwnedCosmeticIds.Add(id);
            prefsField.SetValue(window, oldPrefs); empireField.SetValue(window, oldEmpire);
            Call(window, "RefreshProfileButton"); Call(window, "RefreshEmpireHeader");
        }
    }

    private static async Task<object> Sample(MainWindow window, bool legacyScan)
    {
        int layouts = 0, frames = 0, visited = 0;
        var input = new List<double>();
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        var elapsed = Stopwatch.StartNew();
        EventHandler layout = (_, _) =>
        {
            layouts++;
            if (legacyScan)
                foreach (Border border in Find<Border>(window))
                {
                    // Replay the removed geometric scan, including its iterator allocation.
                    visited++;
                    _ = (Math.Abs(border.Width - 76) < .1 && Math.Abs(border.Height - 108) < .1)
                        || (Math.Abs(border.Width - 48) < .1 && Math.Abs(border.Height - 68) < .1);
                }
        };
        EventHandler rendering = (_, _) => frames++;
        window.LayoutUpdated += layout; CompositionTarget.Rendering += rendering;
        try
        {
            await Task.Run(async () =>
            {
                var period = Stopwatch.StartNew();
                while (period.Elapsed < TimeSpan.FromSeconds(2))
                {
                    await Task.Delay(16);
                    var latency = Stopwatch.StartNew();
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await window.Dispatcher.InvokeAsync(() => input.Add(latency.Elapsed.TotalMilliseconds), DispatcherPriority.Input, timeout.Token);
                }
            });
        }
        finally { window.LayoutUpdated -= layout; CompositionTarget.Rendering -= rendering; }
        input.Sort();
        return new { elapsedMs = elapsed.Elapsed.TotalMilliseconds, frames, layouts, visited,
            allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocated,
            inputP95Ms = input.Count == 0 ? 0 : input[(int)((input.Count - 1) * .95)],
            inputMaxMs = input.Count == 0 ? 0 : input[^1] };
    }

    private static T? Ancestor<T>(DependencyObject element) where T : DependencyObject
    {
        for (DependencyObject? parent = VisualTreeHelper.GetParent(element); parent is not null; parent = VisualTreeHelper.GetParent(parent))
            if (parent is T match) return match;
        return null;
    }
    private static void Call(MainWindow window, string method, params object[] args) => typeof(MainWindow).GetMethod(method, Flags)!.Invoke(window, args);
    private static Rect Bounds(FrameworkElement element, Visual parent) => element.TransformToAncestor(parent).TransformBounds(new Rect(element.RenderSize));
    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T item) yield return item;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (T child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
    private static void Capture(FrameworkElement element, string path)
    {
        var bitmap = new RenderTargetBitmap((int)element.ActualWidth, (int)element.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream file = File.Create(path); encoder.Save(file);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
