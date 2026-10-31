using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core.MidnightMerge;
using BlackjackCasino.Desktop;

internal static class MidnightMergeSmoke
{
    internal static async Task Run(MainWindow window, string output)
    {
        window.Modes.Open("midnight-merge");
        await Settle(window);
        Require(Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible) == 1, "Merge opened a second visible window.");
        MidnightMergeView view = Find<MidnightMergeView>(window).Single();
        Require(view.IsVisible, "Merge was not embedded in the main window.");
        Require(view.Snapshot.Items.Count == 0, "Fresh Merge board was not empty.");
        Capture(window, Path.Combine(output, "merge-initial.png"));
        Click(view, "MergeProducer"); Click(view, "MergeProducer");
        await Settle(window);
        Require(view.Snapshot.Items.Count == 2, "Producer did not spawn two chips.");
        Require(view.Snapshot.Items.All(x => x.DefinitionId == "chip-1"), "Producer spawned the wrong tier.");
        Click(view, "MergeCell_0"); Click(view, "MergeCell_1");
        await Settle(window);
        Require(view.Snapshot.Items.Count == 1 && view.Snapshot.Items.Single().DefinitionId == "chip-2", "UI merge did not produce tier II.");
        Require(Find<Button>(view).Any(x => x.Name == "MergeOrder_first_stack" && x.IsVisible), "Merge order UI missing.");
        Capture(window, Path.Combine(output, "merge-tier-up.png"));
        Click(view, "MergeOrder_first_stack");
        await Settle(window);
        Require(view.Snapshot.RenovationPoints == 10 && view.Snapshot.Items.Count == 0, "Order did not consume its chip and award 10 points.");
        Click(view, "MergeProducer"); Click(view, "MergeProducer");
        Click(view, "MergeCell_0"); Click(view, "MergeCell_1");
        Click(view, "MergeOrder_first_stack");
        Require(view.Snapshot.RenovationPoints == 20 && view.Snapshot.UnlockedMilestones.Contains("lobby/cleanup"), "Restoration milestone did not unlock.");
        Click(view, "MergeProducer");
        Click(view, "MergeCell_0"); Click(view, "MergeCell_41");
        Require(view.Snapshot.Items.Single().Position == 41, "Click fallback did not move the chip.");
        var before = view.Snapshot;
        string serialized = JsonSerializer.Serialize(before);
        Click(view, "MergeBack");
        await Settle(window);
        Require(!Find<MidnightMergeView>(window).Any(), "Merge remained hosted after returning to the lobby.");
        window.Modes.Open("midnight-merge");
        await Settle(window);
        view = Find<MidnightMergeView>(window).Single();
        Require(JsonSerializer.Serialize(view.Snapshot) == serialized, "Leaving and reopening changed the saved state or IDs.");
        // Exercise the same atomic disk writer and app-preferences schema used by the host.
        string directory = Path.Combine(output, "merge-save-roundtrip");
        PlayerStateStore.SaveOrThrow(new AppPreferences { MidnightMerge = before }, directory);
        var loaded = PlayerStateStore.Load(directory);
        Require(JsonSerializer.Serialize(new MergeBoard(loaded.MidnightMerge).Snapshot) == serialized, "Disk serialize/reload lost merge state.");
        Capture(window, Path.Combine(output, "merge-resumed.png"));
        Click(view, "MergeLanguage");
        await Settle(window);
        Require(Localization.Language == UiLanguagePreference.German, "Merge language action did not switch to German.");
        Require(Find<TextBlock>(view).Any(x => x.IsVisible && x.Text == "Lobby-Restaurierung"), "German restoration label missing.");
        Require(Find<TextBlock>(view).Any(x => x.IsVisible && x.Text.Contains("Renovierungspunkte", StringComparison.Ordinal)), "German points text missing.");
        Require(Find<TextBlock>(view).Any(x => x.IsVisible && x.Text == "LIEFERAUFTRÄGE"), "German orders label missing.");
        Capture(window, Path.Combine(output, "merge-german.png"));
        window.Width = 1080; window.Height = 720;
        await Settle(window);
        var lastCell = Find<Button>(view).Single(x => x.Name == "MergeCell_41");
        Rect bounds = lastCell.TransformToAncestor(window).TransformBounds(new Rect(lastCell.RenderSize));
        Require(bounds.Bottom < window.ActualHeight && bounds.Right < window.ActualWidth && bounds.Top > 0, "Merge board clips at minimum window size.");
        Capture(window, Path.Combine(output, "merge-german-1080x720.png"));
        Click(view, "MergeLanguage");
        await Settle(window);
        Require(Localization.Language == UiLanguagePreference.English, "Merge did not restore English.");
        Require(JsonSerializer.Serialize(view.Snapshot) == serialized, "Language/size changes mutated Merge state.");
        Click(view, "MergeBack");
        window.Width = 1280; window.Height = 900;
        await Settle(window);
        File.WriteAllText(Path.Combine(output, "merge-result.txt"), "PASS: embedded host; producer; click move/merge; tier II; order consumption/rewards; restoration milestone; atomic disk roundtrip; exact resume with stable IDs; German labels; minimum window size; English restored.");

        // Render real animations on a separate test host without altering the accepted save.
        var animated = new Window { Width = 1200, Height = 900, Title = "Merge animation acceptance" };
        var animatedView = new MidnightMergeView(new MergeBoard(), () => false, () => true, animated.Close);
        animated.Content = animatedView; animated.Show();
        try
        {
            await Settle(animated);
            Click(animatedView, "MergeProducer"); Click(animatedView, "MergeProducer");
            await Task.Delay(350);
            Click(animatedView, "MergeCell_0"); Click(animatedView, "MergeCell_1");
            await Task.Delay(90); animated.UpdateLayout();
            Capture(animated, Path.Combine(output, "merge-animation.png"));
            await Task.Delay(400);
            Require(animatedView.Snapshot.Items.Single().DefinitionId == "chip-2", "Animation interfered with merge state.");
        }
        finally { animatedView.Dispose(); animated.Close(); }
    }
    private static async Task Settle(FrameworkElement element) { await Task.Delay(100); element.UpdateLayout(); }
    private static void Click(DependencyObject root, string name) => Find<Button>(root).Single(x => x.Name == name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static void Require(bool test, string message) { if (!test) throw new InvalidOperationException(message); }
    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T value) yield return value;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (T child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
    private static void Capture(FrameworkElement element, string path)
    {
        element.UpdateLayout();
        var bitmap = new RenderTargetBitmap(Math.Max(1, (int)element.ActualWidth), Math.Max(1, (int)element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(path); encoder.Save(file);
    }
}
