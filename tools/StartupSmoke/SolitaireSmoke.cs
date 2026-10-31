using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core.Solitaire;
using BlackjackCasino.Desktop;

internal static class SolitaireSmoke
{
    internal static async Task Run(MainWindow window, string output)
    {
        window.Modes.Open("chill-lounge");
        await Task.Delay(120);
        window.UpdateLayout();

        Require(!Find<Button>(window).Any(button => button.Name == "LoungeGamesButton" && button.IsVisible),
            "Chill Lounge still exposes the redundant global Lounge Games header button.");

        Button tile = Find<Button>(window).Single(button => button.Name == "LoungeGame_solitaire" && button.IsVisible);
        Click(tile);
        await Task.Delay(80);
        window.UpdateLayout();
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "MIDNIGHT SOLITAIRE"),
            "Midnight Solitaire eyebrow is missing after opening the Lounge tile.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "Let the cards settle."),
            "Midnight Solitaire hub did not open from the Chill Lounge menu.");
        Capture(window, Path.Combine(output, "solitaire-hub.png"));

        Button newDeal = Find<Button>(window)
            .First(button => button.IsVisible && Equals(button.Content, "NEW DEAL"));
        Click(newDeal);
        await Task.Delay(100);
        window.UpdateLayout();

        SolitaireView view = Find<SolitaireView>(window).Single();
        Require(view.Snapshot.Variant == SolitaireVariant.Klondike,
            "The first Midnight Solitaire New Deal action no longer opens Klondike.");

        Border[] cards = Find<Border>(view)
            .Where(IsFullSizePlayingCard)
            .ToArray();
        Require(cards.Length >= 20, $"Klondike rendered too few playing cards; found {cards.Length}.");

        // Tableau columns are rendered after stock/waste/foundations. The final authored card is
        // therefore the exposed top card in the last tableau column and is always selectable.
        MouseClick(cards[^1]);
        await Task.Delay(40);
        view.UpdateLayout();

        Color gold = (Color)ColorConverter.ConvertFromString("#F2C96D");
        Require(Find<Border>(view).Any(border =>
                border.IsVisible &&
                border.BorderThickness.Left >= 2.9 &&
                border.BorderBrush is SolidColorBrush brush && brush.Color == gold &&
                border.Effect is DropShadowEffect effect && effect.BlurRadius >= 18),
            "Selecting a Solitaire card did not render the skin-independent gold selection halo.");
        Require(Find<TextBlock>(view).Any(text => text.IsVisible && text.Text.StartsWith("SELECTED", StringComparison.Ordinal)),
            "Selecting a Solitaire card did not expose the explicit selected-card status text.");

        Capture(window, Path.Combine(output, "solitaire-selected.png"));

        Border selectedAgain = Find<Border>(view).Where(IsFullSizePlayingCard).Last();
        MouseClick(selectedAgain);
        await Task.Delay(30);
        view.UpdateLayout();
        Require(!Find<TextBlock>(view).Any(text => text.IsVisible && text.Text.StartsWith("SELECTED", StringComparison.Ordinal)),
            "Clicking the selected Solitaire card again did not clear the selection.");

        Click(Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "← SOLITAIRE")));
        await Task.Delay(50);
        window.UpdateLayout();
        Click(Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "← LOUNGE GAMES")));
        await Task.Delay(50);
        window.UpdateLayout();
        Click(Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "RETURN TO CLUB")));
        await Task.Delay(90);
        window.UpdateLayout();

        Require(Find<Button>(window).Any(button => button.Name == "Mode_chill_lounge" && button.IsVisible),
            "Casino lobby was not restored after the Solitaire smoke.");

        File.WriteAllText(Path.Combine(output, "solitaire-result.txt"),
            "PASS: local-only Lounge navigation; Klondike starts from Midnight Solitaire; skin-independent gold selection halo and explicit selection text render and clear correctly.");
    }

    private static bool IsFullSizePlayingCard(Border border) =>
        border.IsVisible && Math.Abs(border.Width - 76d) < 0.1 && Math.Abs(border.Height - 108d) < 0.1 && border.Child is Grid;

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static void MouseClick(Border card)
    {
        var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent,
            Source = card,
        };
        card.RaiseEvent(args);
    }

    private static void Capture(FrameworkElement element, string path)
    {
        element.UpdateLayout();
        int width = Math.Max(1, (int)Math.Ceiling(element.ActualWidth));
        int height = Math.Max(1, (int)Math.Ceiling(element.ActualHeight));
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(path);
        encoder.Save(stream);
    }

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            foreach (T child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
