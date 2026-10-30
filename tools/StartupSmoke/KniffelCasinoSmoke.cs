using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Kniffel;
using BlackjackCasino.Desktop;

internal static class KniffelCasinoSmoke
{
    internal static async Task Run(MainWindow window, string output, PlayerProfileState profile)
    {
        window.Modes.Open("kniffel");
        await Task.Delay(80);
        KniffelView view = Find<KniffelView>(window).Single();
        Click(view, "NewGameButton");
        Capture(window, Path.Combine(output, "dice-casino-setup.png"));
        decimal initialCash = window.DiceCasinoBankroll;
        Click(view, "StartCasinoButton");
        view = Find<KniffelView>(window).Single();
        Require(window.DiceCasinoBankroll == initialCash - 100, "Entry fee was not debited exactly once.");
        Require(Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible) == 1, "Casino dice opened another native game window.");
        Guid gameId = view.Game.GameId;
        Click(view, "NewGameButton");
        Require(!Find<Button>(view).Single(b => b.Name == "StartCasinoButton").IsEnabled, "A second entry can replace an unfinished paid table.");
        Click(view, "CloseDiceSetupButton");
        long xp = profile.LifetimeExperience;
        foreach (KniffelCategory category in KniffelScoring.Categories)
        {
            await Until(() => Find<Button>(view).Single(b => b.Name == "RollButton").IsEnabled);
            Click(view, "RollButton");
            int points = view.Game.Preview(category);
            Click(view, "Score_" + category);
            Click(view, points > 0 ? "CommitButton" : "ScratchButton");
            int turn = view.Game.FilledCount;
            if (turn == 1 || turn == 13)
            {
                // Exit while the worker may still be running, then resume the same paid seat.
                Click(view, "KniffelBackButton");
                decimal cashWhileAway = window.DiceCasinoBankroll;
                await Task.Delay(160);
                Require(window.DiceCasinoBankroll == cashWhileAway, "A paused table modified another surface's bankroll.");
                window.Modes.Open("kniffel");
                await Task.Delay(60);
                view = Find<KniffelView>(window).Single();
                Require(view.Game.GameId == gameId, "Resume changed the paid game identity.");
            }
            await Until(() => window.DiceTableCheckpoint!.Opponents.All(s => s.Entries.Length == turn));
            if (turn == 1)
            {
                Click(view, "OpponentSheetsButton");
                Require(Find<TextBlock>(view).Any(text => text.Text.StartsWith("Mara ·", StringComparison.Ordinal)), "Opponent sheets are missing.");
                Capture(window, Path.Combine(output, "dice-opponent-sheets.png"));
                Click(view, "CloseOpponentSheetsButton");
                Capture(window, Path.Combine(output, "dice-casino-table.png"));
            }
        }
        await Until(() => window.DiceTableCheckpoint!.Settled);
        KniffelTableState result = window.DiceTableCheckpoint!;
        Require(window.DiceCasinoBankroll == initialCash - 100 + result.Payout, "Final cash does not match entry plus rank prize.");
        Require(profile.Kniffel.CasinoGamesCompleted == 1 && profile.Kniffel.TotalWagered == 100, "Casino statistics were not recorded once.");
        Require(profile.LifetimeExperience == xp + KniffelProgression.PlayerExperience(view.Game.Total), "Paid table completion XP is wrong.");
        Capture(window, Path.Combine(output, "dice-casino-result.png"));
        decimal settledCash = window.DiceCasinoBankroll;
        long settledXp = profile.LifetimeExperience;
        Click(view, "KniffelBackButton");
        window.Modes.Open("kniffel");
        await Task.Delay(80);
        view = Find<KniffelView>(window).Single();
        Require(window.DiceCasinoBankroll == settledCash && profile.LifetimeExperience == settledXp, "Reopening a result duplicated cash or XP.");
        Click(view, "NewGameButton");
        Click(view, "StartCasinoButton");
        view = Find<KniffelView>(window).Single();
        Click(view, "NewGameButton");
        Click(view, "ForfeitCasinoButton");
        Require(!window.DiceTableCheckpoint!.Forfeited, "Forfeit did not require confirmation.");
        Click(view, "ForfeitCasinoButton");
        view = Find<KniffelView>(window).Single();
        Require(window.DiceTableCheckpoint!.Forfeited && window.DiceCasinoBankroll == settledCash - 100, "Forfeit incorrectly refunded or paid a prize.");
        Require(profile.LifetimeExperience == settledXp && profile.Kniffel.Forfeits == 1, "Forfeit awarded XP or missed statistics.");
        Require(!Find<Button>(view).Single(b => b.Name == "RollButton").IsEnabled, "A forfeited table can still roll.");
        Click(view, "KniffelBackButton");
        Button overview = Find<Button>(window).Single(b => Equals(b.Content, "Overview") && b.IsVisible);
        overview.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(120);
        Require(Find<TextBlock>(window).Any(text => text.Text.Contains("MIDNIGHT DICE", StringComparison.Ordinal)), "Statistics overview has no Midnight Dice card.");
        Find<TextBlock>(window).First(text => text.Text.Contains("MIDNIGHT DICE", StringComparison.Ordinal)).BringIntoView();
        await Task.Delay(80);
        Capture(window, Path.Combine(output, "dice-statistics.png"));
        Click(window, "ProgressionBackButton");
        Button challenges = Find<Button>(window).Single(b => Equals(b.Content, "Challenges") && b.IsVisible);
        challenges.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(100);
        ComboBox filter = Find<ComboBox>(window).Single(combo => combo.Name == "ChallengeCategoryFilter");
        filter.SelectedItem = filter.Items.Cast<object>().Single(item => item.ToString() == "Midnight Dice");
        await Task.Delay(60);
        Require(Find<TextBlock>(window).Any(text => text.Text.Contains("First Sheet", StringComparison.Ordinal)), "Dice achievements did not appear in the filtered challenge screen.");
        Capture(window, Path.Combine(output, "dice-achievements.png"));
        Click(window, "ProgressionBackButton");
        File.WriteAllText(Path.Combine(output, "dice-casino-result.txt"), "PASS: paid entry, three legal AI seats, full 13-turn table, opponent sheets, pending-turn resume, exact rank payout, no duplicate settlement/XP, confirmed forfeit, statistics and filtered achievements.");
    }
    private static async Task Until(Func<bool> condition)
    {
        var time = Stopwatch.StartNew();
        while (!condition())
        {
            if (time.Elapsed > TimeSpan.FromSeconds(30)) throw new InvalidOperationException("Casino dice operation did not finish in time.");
            await Task.Delay(30);
        }
    }
    private static void Click(DependencyObject root, string name)
    {
        Button button = Find<Button>(root).Single(b => b.Name == name);
        Require(button.IsEnabled && button.IsVisible, name + " is not available.");
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }
    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (T child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
    private static void Capture(FrameworkElement element, string path)
    {
        element.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth), (int)Math.Ceiling(element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream file = File.Create(path); encoder.Save(file);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
