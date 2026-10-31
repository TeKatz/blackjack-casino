using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core.Kniffel;
using BlackjackCasino.Core;
using BlackjackCasino.Desktop;

internal static class KniffelSmoke
{
    internal static async Task Run(MainWindow window, string output, PlayerProfileState profile)
    {
        Require(window.Modes.Modes.Any(mode => mode.Id == "kniffel"), "Kniffel is missing from the lobby.");
        profile.OwnedCosmeticIds.Add("diceset-aurora");
        profile.EquippedDiceSetId = "diceset-aurora";
        window.Modes.Open("kniffel");
        await Task.Delay(100);
        KniffelView view = Find<KniffelView>(window).Single();
        Require(Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible) == 1, "Kniffel opened another native window.");
        Require(Find<KniffelDie>(view).Count() == 5, "Five vector dice were not rendered.");
        Require(Find<KniffelDie>(view).All(die => die.VisualVariant == "dice-aurora"), "Equipped premium dice were not rendered.");
        Require(Find<Button>(view).Count(button => button.Name.StartsWith("Score_", StringComparison.Ordinal)) == 13, "Score sheet does not have 13 boxes.");
        Require(Find<TextBlock>(view).Any(text => text.Text == "Midnight Dice"), "English game title is missing.");
        Require(Find<TextBlock>(view).Single(text => text.Name == "TurnText").Text == "Turn 1 of 13", "Table text was not translated.");
        Require(!Button(view, "CommitButton").IsEnabled, "Scoring was enabled before a roll.");
        Capture(window, Path.Combine(output, "kniffel-initial.png"));
        window.Width = 1080;
        window.Height = 720;
        await Task.Delay(80);
        Capture(window, Path.Combine(output, "kniffel-1080x720.png"));
        window.Width = 1280;
        window.Height = 900;

        Click(view, "RollButton");
        Require(view.Game.RollsUsed == 1, "First roll was not committed.");
        Click(view, "KniffelDie0");
        int held = view.Game.Dice[0];
        Click(view, "RollButton");
        Require(view.Game.RollsUsed == 2 && view.Game.Dice[0] == held, "Reroll changed a held die.");
        Click(view, "Score_Chance");
        Require(view.Game.FilledCount == 0, "Selecting a box committed it without confirmation.");
        Require(Button(view, "Score_Chance").Effect is not null, "A scoring candidate has no glow effect.");
        Capture(window, Path.Combine(output, "kniffel-score-selection.png"));
        Click(view, "CommitButton");
        Require(view.Game.FilledCount == 1 && view.Game.RollsUsed == 0, "Scoring did not advance the turn.");

        // Returning to the lobby must preserve exact dice, holds and remaining rolls.
        Click(view, "RollButton");
        Click(view, "KniffelDie1");
        int[] dice = view.Game.Dice.ToArray();
        Click(view, "KniffelBackButton");
        Require(Find<Button>(window).Any(button => button.Name == "Mode_kniffel" && button.IsVisible && button.IsEnabled), "Lobby was not restored.");
        window.Modes.Open("kniffel");
        await Task.Delay(80);
        view = Find<KniffelView>(window).Single();
        Require(view.Game.RollsUsed == 1 && view.Game.Held[1] && dice.SequenceEqual(view.Game.Dice), "Resume changed the turn checkpoint.");

        long xpBefore = profile.LifetimeExperience;
        foreach (KniffelCategory category in KniffelScoring.Categories.Where(category => category != KniffelCategory.Chance))
        {
            if (!view.Game.CanScore) Click(view, "RollButton");
            int preview = view.Game.Preview(category);
            Click(view, "Score_" + category);
            Click(view, preview > 0 ? "CommitButton" : "ScratchButton");
        }
        Require(view.Game.IsComplete && !Button(view, "RollButton").IsEnabled, "The 13-round game did not finish.");
        long earned = KniffelProgression.PlayerExperience(view.Game.Total);
        Require(profile.LifetimeExperience == xpBefore + earned, "Completed game did not grant the expected XP.");
        Require(Find<TextBlock>(view).Single(text => text.Name == "NoticeText").Text.Contains("XP", StringComparison.Ordinal), "Completion did not show the XP award.");
        await CloseResult(view);
        Capture(window, Path.Combine(output, "kniffel-complete.png"));
        Click(view, "KniffelBackButton");
        window.Modes.Open("kniffel");
        await Task.Delay(80);
        view = Find<KniffelView>(window).Single();
        Require(view.Game.IsComplete && profile.LifetimeExperience == xpBefore + earned, "Reopening a finished game granted duplicate XP.");
        await CloseResult(view);
        Click(view, "NewGameButton");
        Click(view, "StartCasualButton");
        view = Find<KniffelView>(window).Single();
        Require(view.Game.FilledCount == 0, "A new game did not clear the score sheet.");

        // Enable actual roll animation, close while it is running, then resume the result.
        CheckBox animationSetting = Find<CheckBox>(window).Single(box => box.Name == "AnimationsCheckBox");
        animationSetting.IsChecked = true;
        Click(view, "RollButton");
        Require(!Button(view, "RollButton").IsEnabled && !Button(view, "Score_Chance").IsEnabled,
            "Animation did not lock rolling and scoring.");
        dice = view.Game.Dice.ToArray();
        Click(view, "KniffelBackButton");
        window.Modes.Open("kniffel");
        await Task.Delay(80);
        view = Find<KniffelView>(window).Single();
        Require(dice.SequenceEqual(view.Game.Dice) && view.Game.RollsUsed == 1, "Leaving mid-animation granted a reroll.");
        Click(view, "RollButton");
        await Task.Delay(1000);
        Require(view.Game.RollsUsed == 2 && Button(view, "RollButton").IsEnabled, "Animated roll did not settle and unlock.");
        Capture(window, Path.Combine(output, "kniffel-animated-settled.png"));
        Click(view, "NewGameButton");
        Require(Find<Border>(view).Single(border => border.Name == "RestartOverlay").IsVisible, "Restart did not request confirmation.");
        Click(view, "CloseDiceSetupButton");
        Require(view.Game.RollsUsed == 2, "Cancelling restart discarded the turn.");
        Click(view, "KniffelBackButton");
        animationSetting.IsChecked = false;
        File.WriteAllText(Path.Combine(output, "kniffel-result.txt"),
            "PASS: embedded lobby entry; five vector dice; 13 scoring rows; hold/reroll; score selection and confirmation; glow; full game; checkpoints; animation locking, settlement and mid-animation return; restart cancellation; 1080x720 and 1280x900 captures.");
    }

    private static Button Button(DependencyObject root, string name) => Find<Button>(root).Single(button => button.Name == name);
    private static async Task CloseResult(KniffelView view)
    {
        for (int i = 0; i < 400 && !Find<Button>(view).Any(b => b.Name == "CloseMatchResultButton" && b.IsVisible); i++) await Task.Delay(30);
        Click(view, "CloseMatchResultButton");
    }
    private static void Click(DependencyObject root, string name)
    {
        Button button = Button(root, name);
        Require(button.IsEnabled && button.IsVisible, name + " is unavailable.");
        button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
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
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream file = File.Create(path);
        encoder.Save(file);
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
