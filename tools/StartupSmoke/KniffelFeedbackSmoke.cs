using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core.Kniffel;
using BlackjackCasino.Desktop;

internal static class KniffelFeedbackSmoke
{
    internal static async Task Run(MainWindow window, string output)
    {
        // Host a deterministic presentation fixture inside the existing window;
        // it uses detached sheets and never writes the user's profile or bankroll.
        object original = window.Content;
        KniffelView? view = null;
        try
        {
            KniffelEntry[] twelve = KniffelScoring.Categories.Where(c => c != KniffelCategory.Kniffel).Select(c => new KniffelEntry(c, 0)).ToArray();
            KniffelState Ready() => new() { Entries = twelve, RollsUsed = 1, Dice = [6, 6, 6, 6, 6] };
            KniffelState Waiting() => Ready() with { RollsUsed = 0, Dice = new int[5] };
            KniffelState player = Ready();
            var initial = new KniffelTableState { Stake = 100, Player = player, Opponents = [Waiting(), Waiting(), Waiting()] };
            int sounds = 0, saves = 0;
            view = new KniffelView(player, _ => saves++, () => { }, () => true, persistInitialState: false, fiveOfAKindSound: () => sounds++);
            window.Content = view;
            view.UpdateTable(initial, 49_900, false);
            window.UpdateLayout();
            await Task.Delay(80);
            Click(view, "Score_Kniffel");
            Click(view, "CommitButton");
            Require(sounds == 1 && saves == 1, "The player's saved five of a kind did not trigger exactly one cue.");
            await Task.Delay(220);
            Require(Player(view) == "You scored", "The celebration did not identify the player.");
            Require(Find<Border>(view).Single(b => b.Name == "FiveOfAKindBanner").Effect is not null, "The celebration has no glow.");
            Capture(window, Path.Combine(output, "dice-five-player.png"));

            var afterPlayer = initial with { Player = view.Game.Snapshot };
            KniffelState[] opponents = initial.Opponents.Select(state =>
            {
                var game = KniffelGame.Restore(state, () => 6); game.Roll(); game.Commit(KniffelCategory.Kniffel); return game.Snapshot;
            }).ToArray();
            KniffelTableState result = afterPlayer with { Opponents = opponents, Settled = true };
            result = result with { Placement = KniffelTable.Rank(result), Payout = KniffelTable.Prize(result) };
            KniffelTable.Validate(result);
            view.CelebrateOpponents(afterPlayer, result);
            view.CelebrateOpponents(afterPlayer, result); // repeated refresh must not replay effects
            view.SetCompletionNotice("+300 XP · +150 Season XP earned!");
            view.UpdateTable(result, 49_900 + result.Payout, false);
            Require(!Find<KniffelMatchResultView>(view).Any(), "The result screen covered queued celebrations.");
            foreach (string name in KniffelTable.Names)
            {
                await Until(() => Player(view) == name + " scored");
                if (name == "Mara")
                {
                    await Task.Delay(220);
                    Capture(window, Path.Combine(output, "dice-five-opponent.png"));
                }
            }
            await Until(() => Find<KniffelMatchResultView>(view).Any(v => v.IsVisible));
            Require(sounds == 4 && saves == 1, "Opponent feedback repeated a sound or changed the saved game.");
            Require(Find<TextBlock>(view).Single(t => t.Name == "MatchNetText").Text == "+$1,927.50", "Tied prize receipt is wrong.");
            Require(Find<TextBlock>(view).Any(t => t.Text.Contains("#1 (tied)", StringComparison.Ordinal)), "The result did not explain a shared placement.");
            Capture(window, Path.Combine(output, "dice-match-tie.png"));
            window.Width = 1080; window.Height = 720;
            await Task.Delay(80);
            Capture(window, Path.Combine(output, "dice-match-1080x720.png"));
            window.Width = 1280; window.Height = 900;
            Click(view, "CloseMatchResultButton");
            Click(view, "MatchResultButton");
            Require(sounds == 4 && saves == 1, "Opening the receipt triggered gameplay or sound again.");
            Click(view, "CloseMatchResultButton");
            KniffelTableState loss = result with { Stake = 10_000, Opponents = opponents.Select(s => s with
                { Entries = s.Entries.Select(e => e.Category == KniffelCategory.Sixes ? e with { Points = 30 } : e).ToArray() }).ToArray(), Placement = 4, Payout = 250 };
            KniffelTable.Validate(loss);
            view.UpdateTable(loss, 40_250, false);
            Click(view, "MatchResultButton");
            Require(Find<TextBlock>(view).Single(t => t.Name == "MatchNetText").Text == "−$9,750.00", "A positive prize was incorrectly presented as a net win.");
            Require(Find<TextBlock>(view).Any(t => t.Text == "NET LOSS"), "Loss receipt is missing its clear loss label.");
            Capture(window, Path.Combine(output, "dice-match-loss.png"));
            Click(view, "CloseMatchResultButton");
            view.Dispose();

            // Bonus five in a different category, with reduced motion and a paused dialog.
            var bonusBefore = Ready() with { Entries = [new(KniffelCategory.Kniffel, 50)], KniffelsScored = 1 };
            var bonusGame = KniffelGame.Restore(bonusBefore);
            bonusGame.Commit(KniffelCategory.Chance);
            view = new KniffelView(bonusBefore, _ => { }, () => { }, () => false, persistInitialState: false, fiveOfAKindSound: () => sounds++);
            window.Content = view; window.UpdateLayout();
            await Task.Delay(60);
            view.ShowGameSetup(new Button { Content = "Paused fixture" });
            view.CelebrateScore("You", bonusBefore, bonusGame.Snapshot);
            Require(sounds == 4, "Feedback played behind an open dialog.");
            view.HideGameSetup();
            Require(sounds == 5 && Find<TextBlock>(view).Any(t => t.Text.Contains("+50 bonus", StringComparison.Ordinal)), "Bonus five in another box was missed.");
            Require(Find<Border>(view).Single(b => b.Name == "FiveOfAKindBanner").Opacity == 1, "Reduced motion hid the static feedback.");
            view.Dispose();
            await Task.Delay(80);
            Require(!Find<Border>(view).Any(b => b.Name == "FiveOfAKindBanner"), "Disposal left feedback running.");

            view = new KniffelView(Ready(), _ => throw new IOException("fixture save failure"), () => { }, () => false,
                persistInitialState: false, fiveOfAKindSound: () => sounds++);
            window.Content = view; window.UpdateLayout();
            await Task.Delay(60);
            Click(view, "Score_Kniffel"); Click(view, "CommitButton");
            Require(view.Game.FilledCount == 12 && sounds == 5, "A failed save celebrated an uncommitted score.");
            File.WriteAllText(Path.Combine(output, "dice-feedback-result.txt"), "PASS: saved player and three AI celebrations, glow and particles, ordered cues with deduplication, deferred tied result, exact net and bonus breakdown, 1080x720 receipt, read-only reopen, extra-five bonus in another box, reduced motion, dialog deferral, disposal, failed-save rollback.");
        }
        finally { view?.Dispose(); window.Content = original; window.UpdateLayout(); }
    }
    private static string? Player(DependencyObject root) => Find<TextBlock>(root).FirstOrDefault(t => t.Name == "FiveOfAKindPlayer")?.Text;
    private static async Task Until(Func<bool> condition)
    {
        var clock = Stopwatch.StartNew();
        while (!condition()) { if (clock.Elapsed.TotalSeconds > 15) throw new InvalidOperationException("Dice feedback did not advance."); await Task.Delay(30); }
    }
    private static void Click(DependencyObject root, string name)
    {
        Button button = Find<Button>(root).Single(b => b.Name == name);
        Require(button.IsEnabled && button.IsVisible, name + " is not available."); button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }
    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T found) yield return found;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (T child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
    private static void Capture(FrameworkElement root, string path)
    {
        root.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth), (int)Math.Ceiling(root.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream file = File.Create(path); encoder.Save(file);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
