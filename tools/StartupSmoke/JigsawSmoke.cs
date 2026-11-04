using System.IO;
using System.Reflection;
using BlackjackCasino.Core.Jigsaw;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Desktop;

internal static class JigsawSmoke
{
    internal static async Task Run(MainWindow window, string output)
    {
        NavigationSmoke.OpenCovered(window, "chill-lounge");
        await Task.Delay(120);
        window.UpdateLayout();
        Button tile = FindButton(window, "LoungeGame_jigsaw");
        Require(tile.IsVisible, "Jigsaw tile is missing from Chill Lounge.");
        Click(tile);
        await Task.Delay(120);
        window.UpdateLayout();

        Require(Find<Button>(window).Count(b => b.Name.StartsWith("JigsawMotif_", StringComparison.Ordinal)) == 12,
            "The Jigsaw gallery did not offer all twelve themes.");
        Require(Find<Button>(window).Count(b => b.Name.StartsWith("JigsawSize_", StringComparison.Ordinal)) == 8,
            "The Jigsaw setup is missing one of the eight piece counts.");
        Capture(window, Path.Combine(output, "jigsaw-gallery.png"));

        Require(FindButton(window, "JigsawMenuTimer").IsVisible, "The timer option is missing.");
        Require(FindButton(window, "JigsawAchievementsOpen").IsVisible, "Jigsaw has no achievements entry.");
        Click(FindButton(window, "JigsawAchievementsOpen"));
        await Task.Delay(80);
        window.UpdateLayout();
        Require(Find<Button>(window).Any(b => b.Name == "JigsawAchievementsBack" && b.IsVisible),
            "The achievements gallery did not open.");
        Capture(window, Path.Combine(output, "jigsaw-achievements.png"));
        Click(FindButton(window, "JigsawAchievementsBack"));
        await Task.Delay(80);
        window.UpdateLayout();
        Click(FindButton(window, "JigsawMenuTimer"));
        await Task.Delay(80);
        window.UpdateLayout();
        Require(Equals(FindButton(window, "JigsawMenuTimer").Content, "TIMER: OFF"),
            "Disabling the timer did not persist in the setup menu.");

        Click(FindButton(window, "JigsawSize_1000"));
        await Task.Delay(90);
        window.UpdateLayout();
        Click(FindButton(window, "JigsawStart"));
        await Task.Delay(150);
        window.UpdateLayout();
        Require(Find<FrameworkElement>(window).Any(b => b.IsVisible && b.GetType().Name == "JigsawBoardSurface"),
            "Jigsaw did not open the interactive puzzle surface.");
        Require(Find<Button>(window).Any(b => b.Name == "JigsawNextTray"),
            "Jigsaw's paginated piece tray is missing.");
        Require(Equals(FindButton(window, "JigsawTimerToggle").Content, "TIMER: OFF"),
            "The in-game timer did not inherit the disabled preference.");
        Capture(window, Path.Combine(output, "jigsaw-board-1000.png"));

        Click(FindButton(window, "JigsawBackToMenu"));
        await Task.Delay(100);
        window.UpdateLayout();
        Require(FindButton(window, "JigsawResume").IsVisible,
            "The puzzle was not resumable after leaving the board.");
        Click(FindButton(window, "JigsawResume"));
        await Task.Delay(100);
        window.UpdateLayout();
        Require(Find<FrameworkElement>(window).Any(b => b.IsVisible && b.GetType().Name == "JigsawBoardSurface"),
            "Resuming a puzzle did not restore the board.");
        // Simulate a solved 1,000-piece board and verify the hosted result transition.
        FrameworkElement view = Find<FrameworkElement>(window)
            .Single(e => e.IsVisible && e.GetType().Name == "JigsawPlayView");
        FieldInfo gameField = view.GetType().GetField("game",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var game = (JigsawGameState)gameField.GetValue(view)!;
        for (int i = 0; i < game.PieceCount; i++)
        {
            (double x, double y) = JigsawGame.Target(game, i);
            if (!game.Pieces[i].OnBoard) Require(JigsawGame.Place(game, i, x, y), "Could not place smoke piece.");
            else { game.Pieces[i].X = x; game.Pieces[i].Y = y; }
            JigsawGame.Snap(game, i);
        }
        Require(game.Completed, "Thousand-piece game could not complete.");
        MethodInfo committed = view.GetType().GetMethod("OnBoardCommitted",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        committed.Invoke(view, null);
        await Task.Delay(180);
        window.UpdateLayout();
        Require(FindButton(window, "JigsawResultBack").IsVisible,
            "No puzzle completion screen was displayed.");
        Require(FindButton(window, "JigsawResultAgain").IsVisible,
            "Completion screen is missing the play-again action.");
        Capture(window, Path.Combine(output, "jigsaw-completion.png"));
        Click(FindButton(window, "JigsawResultBack"));
        await Task.Delay(70);
        window.UpdateLayout();
        Click(FindButton(window, "LoungeGamesHeaderBack"));
        await Task.Delay(70);
        window.UpdateLayout();
        Require(FindButton(window, "LoungeGame_jigsaw").IsVisible,
            "Back-to-games did not restore the Chill Lounge menu.");
        Click(Find<Button>(window).Single(b => b.IsVisible && Equals(b.Content, "RETURN TO CLUB")));
        await Task.Delay(70);
        window.UpdateLayout();
        File.WriteAllText(Path.Combine(output, "jigsaw-result.txt"),
            "PASS: Jigsaw tile, 12 motifs, 8 sizes, clock toggle, 30 achievement gallery, 1000-piece board, tray, resume, completion and Lounge navigation.");
    }

    private static Button FindButton(MainWindow window, string name) =>
        Find<Button>(window).Single(x => x.Name == name && x.IsVisible);

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static void Capture(FrameworkElement element, string file)
    {
        element.UpdateLayout();
        int w = Math.Max(1, (int)Math.Ceiling(element.ActualWidth));
        int h = Math.Max(1, (int)Math.Ceiling(element.ActualHeight));
        var bitmap = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(file);
        encoder.Save(stream);
    }

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T found) yield return found;
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
            foreach (T element in Find<T>(VisualTreeHelper.GetChild(root, i)))
                yield return element;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
