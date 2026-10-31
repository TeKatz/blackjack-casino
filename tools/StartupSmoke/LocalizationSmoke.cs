using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Desktop;

internal static class LocalizationSmoke
{
    internal static async Task Run(MainWindow window, string output)
    {
        ComboBox language = Find<ComboBox>(window).FirstOrDefault(combo => combo.Name == "LanguageComboBox")
            ?? throw new InvalidOperationException("Presentation settings do not expose the Language selector.");

        Require(language.Items.Count == 2, $"Language selector should expose English and Deutsch; found {language.Items.Count} items.");
        Require(language.SelectedIndex == 0 && SelectedLanguageName(language) == "English",
            "Existing/default preferences should visibly start in English.");

        language.SelectedIndex = 1;
        await Task.Delay(120);
        window.UpdateLayout();

        Require(CultureInfo.CurrentUICulture.Name == "de-DE",
            $"German selection did not switch CurrentUICulture to de-DE (was {CultureInfo.CurrentUICulture.Name}).");
        Require(language.SelectedIndex == 1 && SelectedLanguageName(language) == "Deutsch",
            $"Language selector changed the UI to German but still displays '{SelectedLanguageName(language)}'.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "DARSTELLUNG"),
            "Presentation header did not switch to German without a restart.");
        Require(Find<CheckBox>(window).Any(check => check.Name == "SoundCheckBox" && Equals(check.Content, "Casino-Soundeffekte")),
            "Sound setting did not switch to its German resource value.");
        Require(Find<Button>(window).Any(button => button.Name == "HitButton" && Equals(button.Content, "KARTE  [H]")),
            "Blackjack action text did not switch to German.");
        Require(Find<TextBlock>(window).Any(text => text.Text == "Sprache"),
            "Language selector label did not switch to German.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "Eine andere Art von Nacht."),
            "Casino lobby headline did not switch to German.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "DIE SAMTTISCHE"),
            "Blackjack lobby subtitle did not switch to German.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "PLATZ NEHMEN   →"),
            "Casino lobby call-to-action did not switch to German.");

        SaveScreenshot(window, Path.Combine(output, "localization-german-lobby.png"));

        // Actual Blackjack play surface: the user's first test exposed English hand/result text here.
        window.Modes.Open("blackjack");
        await Task.Delay(80);
        window.UpdateLayout();
        Button deal = Find<Button>(window).Single(button => button.Name == "DealButton");
        Require(deal.IsEnabled, "Blackjack localization smoke could not start its test hand.");
        Click(deal);
        await Task.Delay(100);
        window.UpdateLayout();
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text.StartsWith("DEINE HAND", StringComparison.Ordinal)),
            "Blackjack player-hand label remained English during an actual hand.");
        Button? stand = Find<Button>(window).FirstOrDefault(button => button.Name == "StandButton" && button.IsEnabled);
        if (stand is not null)
        {
            Click(stand);
            await Task.Delay(100);
            window.UpdateLayout();
        }
        TextBlock status = Find<TextBlock>(window).Single(text => text.Name == "StatusText");
        Require(!status.Text.Contains("You win", StringComparison.Ordinal) && !status.Text.Contains("You lose", StringComparison.Ordinal),
            $"Blackjack settlement remained English: {status.Text}");
        Require(!Find<TextBlock>(window).Any(text => text.IsVisible && (text.Text.Contains(" · Loss ", StringComparison.Ordinal) || text.Text.Contains(" · Win ", StringComparison.Ordinal))),
            "Blackjack hand outcome remained English after settlement.");
        SaveScreenshot(window, Path.Combine(output, "localization-german-blackjack.png"));

        // Casino Vault must localize when opened after the language switch, not only its header tile.
        Button vault = Find<Button>(window).Single(button => button.Name == "CasinoVaultButton" && button.IsVisible);
        Click(vault);
        await Task.Delay(100);
        window.UpdateLayout();
        Grid overlay = Find<Grid>(window).Single(grid => grid.Name == "CasinoVaultOverlay" && grid.IsVisible);
        Require(Find<TextBlock>(overlay).Any(text => text.IsVisible && text.Text == "BEITRAG"),
            "Casino Vault contribution row remained English.");
        Require(Find<TextBlock>(overlay).Any(text => text.IsVisible && text.Text == "VOLLER VAULT"),
            "Casino Vault full-vault row remained English.");
        Require(Find<TextBlock>(overlay).Any(text => text.IsVisible && text.Text == "KEIN HAKEN"),
            "Casino Vault no-catch row remained English.");
        Require(Find<Button>(overlay).Any(button => button.Name == "CasinoVaultOpenButton" && button.Content?.ToString()?.StartsWith("AUFBRECHEN", StringComparison.Ordinal) == true),
            "Casino Vault primary action remained English.");
        SaveScreenshot(window, Path.Combine(output, "localization-german-vault.png"));
        Click(Find<Button>(overlay).Single(button => button.Name == "CasinoVaultCloseButton"));
        await Task.Delay(60);

        // Chill Lounge game selection and Sudoku hub/game are dynamically hosted surfaces.
        window.Modes.Open("chill-lounge");
        await Task.Delay(140);
        window.UpdateLayout();
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "Finde dein eigenes Tempo."),
            "Chill Lounge game menu remained English.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "Ein wenig Raum zum Denken."),
            "Sudoku Lounge tile remained English.");
        SaveScreenshot(window, Path.Combine(output, "localization-german-lounge.png"));

        Click(Find<Button>(window).Single(button => button.Name == "LoungeGame_sudoku" && button.IsVisible));
        await Task.Delay(80);
        window.UpdateLayout();
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "Ein Raster. Keine Uhr, die du schlagen musst."),
            "Sudoku hub headline remained English.");
        Require(Find<Button>(window).Any(button => button.Name == "SudokuTimerToggle" && Equals(button.Content, "TIMER · SICHTBAR")),
            "Sudoku timer control remained English.");
        Require(Find<Button>(window).Any(button => button.Name == "SudokuNew_Medium" && Equals(button.Content, "NEUES RÄTSEL")),
            "Sudoku New Puzzle action remained English.");

        Click(Find<Button>(window).Single(button => button.Name == "SudokuNew_Medium"));
        await Task.Delay(100);
        window.UpdateLayout();
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "ZAHL"),
            "Sudoku game number-pad label remained English.");
        Require(Find<Button>(window).Any(button => button.IsVisible && Equals(button.Content, "EIN FELD AUFDECKEN")),
            "Sudoku hint action remained English.");
        Require(Find<System.Windows.Controls.Primitives.ToggleButton>(window).Any(button => button.IsVisible && Equals(button.Content, "NOTIZEN  [LEERTASTE]")),
            "Sudoku pencil action remained English.");
        SaveScreenshot(window, Path.Combine(output, "localization-german-sudoku.png"));

        Click(Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "← LOUNGE")));
        await Task.Delay(70);
        window.UpdateLayout();
        Click(Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "← LOUNGE-SPIELE")));
        await Task.Delay(70);
        window.UpdateLayout();

        // Midnight Solitaire hub and an actual Klondike deal.
        Click(Find<Button>(window).Single(button => button.Name == "LoungeGame_solitaire" && button.IsVisible));
        await Task.Delay(90);
        window.UpdateLayout();
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "Lass die Karten zur Ruhe kommen."),
            "Midnight Solitaire hub headline remained English.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "ERFOLGE"),
            "Midnight Solitaire achievement section remained English.");
        Button newDeal = Find<Button>(window).First(button => button.IsVisible && Equals(button.Content, "NEUES SPIEL"));
        Click(newDeal);
        await Task.Delay(100);
        window.UpdateLayout();
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "ZIEHEN"),
            "Klondike draw-zone label remained English.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "ABLAGE"),
            "Klondike waste-zone label remained English.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "FUNDAMENTE"),
            "Klondike foundation-zone label remained English.");
        Border[] cards = Find<Border>(window).Where(IsPlayingCard).ToArray();
        Require(cards.Length > 0, "Localized Klondike did not render playing cards.");
        MouseClick(cards[^1]);
        await Task.Delay(50);
        window.UpdateLayout();
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text.StartsWith("AUSGEWÄHLT", StringComparison.Ordinal)),
            "Solitaire selected-card feedback remained English.");
        SaveScreenshot(window, Path.Combine(output, "localization-german-solitaire.png"));

        Click(Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "← SOLITAIRE")));
        await Task.Delay(50);
        Click(Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "← LOUNGE-SPIELE")));
        await Task.Delay(50);
        Click(Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "ZURÜCK ZUM CLUB")));
        await Task.Delay(100);
        window.UpdateLayout();

        language.SelectedIndex = 0;
        await Task.Delay(120);
        window.UpdateLayout();

        Require(CultureInfo.CurrentUICulture.Name == "en-US",
            $"English selection did not restore CurrentUICulture to en-US (was {CultureInfo.CurrentUICulture.Name}).");
        Require(language.SelectedIndex == 0 && SelectedLanguageName(language) == "English",
            $"Language selector did not visibly return to English; it shows '{SelectedLanguageName(language)}'.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "PRESENTATION"),
            "Presentation header did not switch back to English without a restart.");
        Require(Find<CheckBox>(window).Any(check => check.Name == "SoundCheckBox" && Equals(check.Content, "Casino sound effects")),
            "Sound setting did not restore its English resource value.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "A different kind of night."),
            "Casino lobby headline did not switch back to English.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "VAULT FULL"),
            "Dynamic Vault header did not switch back to English.");
    }

    private static string SelectedLanguageName(ComboBox combo)
    {
        object? selected = combo.SelectedItem;
        PropertyInfo? display = selected?.GetType().GetProperty("DisplayName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return display?.GetValue(selected)?.ToString() ?? combo.Text;
    }

    private static bool IsPlayingCard(Border border) =>
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

    private static void SaveScreenshot(FrameworkElement element, string path)
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
