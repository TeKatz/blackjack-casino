using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Desktop;
using BlackjackCasino.Core;

internal static class LocalizationSmoke
{
    internal static async Task Run(MainWindow window, string output)
    {
        Localization.InstallAutomaticTranslation();

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
            "Casino lobby headline did not switch to German without a restart.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "DIE SAMTTISCHE"),
            "Blackjack lobby subtitle did not switch to German.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "PLATZ NEHMEN   →"),
            "Casino lobby call-to-action did not switch to German.");

        CheckGermanShellWindows(window);
        CheckPhase2Translations();

        SaveScreenshot(window, Path.Combine(output, "localization-german-lobby.png"));

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

        // Settlement can include dealer presentation/animation work. Do not assume that a fixed 100 ms
        // delay is enough before testing another header surface; wait for the table to become deal-ready.
        await WaitForBlackjackSettlement(window);

        TextBlock status = Find<TextBlock>(window).Single(text => text.Name == "StatusText");
        Require(!status.Text.Contains("You win", StringComparison.Ordinal) && !status.Text.Contains("You lose", StringComparison.Ordinal),
            $"Blackjack settlement remained English: {status.Text}");
        Require(!Find<TextBlock>(window).Any(text => text.IsVisible && (text.Text.Contains(" · Loss ", StringComparison.Ordinal) || text.Text.Contains(" · Win ", StringComparison.Ordinal))),
            "Blackjack hand outcome remained English after settlement.");
        SaveScreenshot(window, Path.Combine(output, "localization-german-blackjack.png"));

        Button vault = Find<Button>(window).Single(button => button.Name == "CasinoVaultButton" && button.IsVisible);
        Grid overlay = await OpenVault(window, vault);
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

        NavigationSmoke.OpenCovered(window, "chill-lounge");
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
        Require(Find<Button>(window).Any(button => IsVisibleGamesButton(button, "SPIELE")),
            "Sudoku Games header navigation did not localize to German.");

        Click(Find<Button>(window).Single(button => button.Name == "SudokuNew_Expert"));
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
        Click(Find<Button>(window).Single(button => IsVisibleGamesButton(button, "SPIELE")));
        await Task.Delay(70);
        window.UpdateLayout();

        Click(Find<Button>(window).Single(button => button.Name == "LoungeGame_solitaire" && button.IsVisible));
        await Task.Delay(90);
        window.UpdateLayout();
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "Lass die Karten zur Ruhe kommen."),
            "Midnight Solitaire hub headline remained English.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "ERFOLGE"),
            "Midnight Solitaire achievement section remained English.");
        Require(Find<Button>(window).Any(button => IsVisibleGamesButton(button, "SPIELE")),
            "Midnight Solitaire Games header navigation did not localize to German.");
        Require(!Find<Button>(window).Any(button => button.IsVisible && button.Content is string text && text.Contains("LOUNGE-SPIELE", StringComparison.OrdinalIgnoreCase)),
            "Midnight Solitaire still exposes the old Lounge-Spiele navigation.");
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
        Click(Find<Button>(window).Single(button => IsVisibleGamesButton(button, "SPIELE")));
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
            "Casino lobby headline did not switch back to English without a restart.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "VAULT FULL"),
            "Dynamic Vault header did not switch back to English.");
    }

    private static void CheckPhase2Translations()
    {
        (string Source, string German)[] samples =
        [
            ("YOUR DICE TABLE", "DEIN WÜRFELTISCH"),
            ("THREE REELS · ONE PAYLINE · PLAY MONEY", "DREI WALZEN · EINE GEWINNLINIE · SPIELGELD"),
            ("THE HOUSE REMEMBERS", "DAS HAUS ERINNERT SICH"),
            ("ACCESS LEVEL I", "ZUGANGSSTUFE I"),
            ("HIGH VOLATILITY · 3×3 · FIVE LINES", "HOHE VOLATILITÄT · 3×3 · FÜNF LINIEN"),
            ("WORDS TO DISCOVER", "WÖRTER ZU FINDEN"),
            ("PAIRS FOUND", "PAARE GEFUNDEN"),
            ("Slot Gallery", "Spielautomatengalerie"),
            ("Warm lights, brass cabinets and the first heartbeat of your casino.",
                "Warmes Licht, Messinggehäuse und der erste Herzschlag deines Casinos."),
            ("Turn 3 of 13", "Runde 3 von 13"),
            ("Roll 2 / 3", "Wurf 2 / 3"),
            ("Record 13 points", "13 Punkte eintragen"),
            ("Upper: 35 points · Bonus: 0 / 35 (at 63)",
                "Oben: 35 Punkte · Bonus: 0 / 35 (bei 63)"),
            ("NINE LIVES · 4 SPINS LEFT", "NINE LIVES · 4 FREISPIELE ÜBRIG"),
            ("Found FOREST. Saved.", "FOREST gefunden. Gespeichert."),
            ("Hint: FOREST begins at the glowing letter.",
                "Hinweis: FOREST beginnt beim leuchtenden Buchstaben."),
            ("7 / 12 words found · 03:12 · 2 hints",
                "7 / 12 Wörter gefunden · 03:12 · 2 Hinweise"),
            ("Active managers: 1/3", "Aktive Manager: 1/3"),
        ];
        foreach ((string source, string german) in samples)
        {
            string actual = Localization.TranslateSource(source);
            Require(string.Equals(actual, german, StringComparison.Ordinal),
                $"Game translation mismatch for '{source}': expected '{german}', got '{actual}'.");
        }
    }

    private static void CheckGermanShellWindows(MainWindow owner)
    {
        var profile = new PlayerProfileState { DisplayName = "Testspieler", Level = 75, Prestige = 3, Coins = 1500 };
        Type welcomeType = typeof(MainWindow).Assembly.GetType(
            "BlackjackCasino.Desktop.StartupWelcomeWindow", throwOnError: true)!;
        ConstructorInfo constructor = welcomeType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null, new[] { typeof(PlayerProfileState) }, modifiers: null)
            ?? throw new InvalidOperationException("Missing startup welcome constructor.");
        var welcome = (Window)constructor.Invoke([profile]);
        welcome.Owner = owner;
        try
        {
            welcome.Show();
            welcome.UpdateLayout();
            Require(Find<TextBlock>(welcome).Any(x => x.Text == "Willkommen zurück, Testspieler."),
                "German welcome failed to localize its personalized greeting.");
            Require(Find<TextBlock>(welcome).Any(x => x.Text == "Änderungen dieser Version"),
                "German welcome still displays an English changelog heading.");
            Require(Find<Button>(welcome).Any(x => Equals(x.Content, "CLUB BETRETEN  →")),
                "German welcome action remained English.");
        }
        finally { welcome.Close(); }

        using var controller = new PlayerProgressionController(profile);
        var playerWindow = new PlayerProfileWindow(controller) { Owner = owner };
        try
        {
            playerWindow.Show();
            playerWindow.UpdateLayout();
            Require(Find<TextBlock>(playerWindow).Any(x => x.Text == "Deine Casino-Identität"),
                "Player profile identity heading remained English.");
            Require(Find<TextBlock>(playerWindow).Any(x => x.Text.Contains("Stufe 75", StringComparison.Ordinal)),
                "Player profile level summary remained English.");
            Require(Find<Button>(playerWindow).Any(x => Equals(x.Content, "Fortschritt & Shop →")),
                "Player profile progression action remained English.");
        }
        finally { playerWindow.Close(); }

        var rulesWindow = new TableRulesDialog(TablePresets.Get(TablePresetId.Casual).Rules) { Owner = owner };
        try
        {
            rulesWindow.Show();
            rulesWindow.UpdateLayout();
            Require(Find<TextBlock>(rulesWindow).Any(x => x.Text == "Erweiterte Tischregeln"),
                "Advanced table rules heading remained English.");
            Require(Find<Button>(rulesWindow).Any(x => Equals(x.Content, "Eigene Regeln verwenden")),
                "Advanced table setup action remained English.");
            Require(Find<ComboBox>(rulesWindow).Any(x =>
                x.Items.OfType<string>().Any(v => v.Contains("weichen 17", StringComparison.Ordinal))),
                "Advanced table setup combo-box values remained English.");
        }
        finally { rulesWindow.Close(); }
    }

    private static bool IsVisibleGamesButton(Button button, string label)
    {
        if (!button.IsVisible || button.Content is not string text) return false;
        return text.Contains(label, StringComparison.OrdinalIgnoreCase) &&
               !text.Contains("LOUNGE", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WaitForBlackjackSettlement(MainWindow window)
    {
        for (int attempt = 0; attempt < 120; attempt++)
        {
            window.UpdateLayout();
            Button? deal = Find<Button>(window).FirstOrDefault(button => button.Name == "DealButton");
            if (deal is { IsEnabled: true }) return;

            Button? decline = Find<Button>(window).FirstOrDefault(button => button.Name == "DeclineInsuranceButton" && button.IsVisible && button.IsEnabled);
            if (decline is not null) Click(decline);
            Button? stand = Find<Button>(window).FirstOrDefault(button => button.Name == "StandButton" && button.IsEnabled);
            if (stand is not null) Click(stand);

            await Task.Delay(75);
        }

        TextBlock? status = Find<TextBlock>(window).FirstOrDefault(text => text.Name == "StatusText");
        throw new InvalidOperationException("Localized Blackjack hand did not return to a deal-ready state. Status: " + status?.Text);
    }

    private static async Task<Grid> OpenVault(MainWindow window, Button vault)
    {
        await WaitForBlackjackSettlement(window);
        Click(vault);

        for (int attempt = 0; attempt < 40; attempt++)
        {
            await Task.Delay(50);
            window.UpdateLayout();
            Grid? overlay = Find<Grid>(window).FirstOrDefault(grid => grid.Name == "CasinoVaultOverlay" && grid.IsVisible);
            if (overlay is not null) return overlay;
        }

        TextBlock? status = Find<TextBlock>(window).FirstOrDefault(text => text.Name == "StatusText");
        throw new InvalidOperationException("Casino Vault overlay did not open after Blackjack reached its deal-ready state. Status: " + status?.Text);
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
